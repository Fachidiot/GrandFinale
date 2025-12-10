using UnityEngine;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine.SceneManagement;
using System.Text;

public enum NetworkMode
{
    None,
    Client,
    Host,
    SinglePlayer
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public const byte INVALID_PLAYER_ID = 255;

    public NetworkMode Mode { get; private set; } = NetworkMode.None;

    public void SetMode(NetworkMode mode)
    {
        Mode = mode;
        if (mode == NetworkMode.SinglePlayer)
        {
            // Ensure ServerRoomManager exists for single-player sessions
            if (ServerRoomManager.Instance == null)
            {
                gameObject.AddComponent<ServerRoomManager>();
            }
            // No network connection is established, but the room manager is needed for local state.
        }
    }
    public byte MyPlayerId { get; private set; } = INVALID_PLAYER_ID;
    public bool IsConnected { get; private set; }
    public string PlayerId { get; private set; } // SteamID as a string
    public CSteamID selfSteamId { get; private set; }
    public CSteamID CurrentLobbyID => m_CurrentLobbyID;
    public CSteamID LobbyHostID => lobbyHostID;

    // --- Events ---
    public static event Action OnConnected;
    public static event Action<string> OnConnectionFailed;
    public static event Action OnDisconnected;
    public static event Action<CSteamID, string> OnJsonMessageReceived;

    // --- Networking Internals ---
    // Queue for packets received on the networking thread, to be processed on the main thread in Update.
    private readonly ConcurrentQueue<(CSteamID, byte[])> p2pPacketQueue = new ConcurrentQueue<(CSteamID, byte[])>();
    // Host-only cache of the latest state received from each client.
    private readonly ConcurrentDictionary<byte, PlayerState> receivedPlayerStates = new ConcurrentDictionary<byte, PlayerState>();

    // --- Steam Lobby Internals ---
    private CSteamID m_CurrentLobbyID;
    private CSteamID lobbyHostID;
    private List<CSteamID> lobbyMembers = new List<CSteamID>();

    // --- Cached Manager References ---
    private SpaceShipManager _spaceShipManager;
    private SpaceShipManager SpaceShipManager
    {
        get
        {
            if (_spaceShipManager == null) _spaceShipManager = FindObjectOfType<SpaceShipManager>();
            return _spaceShipManager;
        }
    }

    // --- Steam Callbacks ---
    private Callback<LobbyCreated_t> m_LobbyCreated;
    private Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequested;
    private Callback<LobbyEnter_t> m_LobbyEnter;
    private Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;
    private Callback<P2PSessionRequest_t> m_P2PSessionRequest;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
        {
            selfSteamId = SteamUser.GetSteamID();
            PlayerId = selfSteamId.ToString();

            // Register Steam callback handlers
            m_LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            m_GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            m_LobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            m_P2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        Disconnect();
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    /// <summary>
    /// Main thread loop for processing received packets.
    /// </summary>
    private void Update()
    {
        ListenForP2PPackets();

        while (p2pPacketQueue.TryDequeue(out var item))
        {
            (CSteamID sender, byte[] data) = item;
            HandleP2PPacket(sender, data);
        }
    }

    /// <summary>
    /// Physics-timed loop for sending network state updates.
    /// </summary>
    private void FixedUpdate()
    {
        // Guard clauses to prevent sending updates when not in a valid state
        if (!IsConnected || MyPlayerId == INVALID_PLAYER_ID || PlayerManager.Instance == null) return;

        // This check is problematic if the player hasn't been spawned yet.
        // Let's refine it to only check for the player GO if we are a client.
        if (Mode == NetworkMode.Client && !PlayerManager.Instance.Players.ContainsKey(PlayerId)) return;


        if (Mode == NetworkMode.Host)
        {
            SendHostUpdates();
        }
        else // Client
        {
            if (PlayerManager.Instance.Players.TryGetValue(PlayerId, out GameObject myPlayerGo))
            {
                SendClientUpdates(myPlayerGo);
            }
        }
    }

    #endregion

    #region State Sending

    /// <summary>
    /// Executed by the host. Gathers all game state and broadcasts it to clients.
    /// </summary>
    private void SendHostUpdates()
    {
        if (ServerRoomManager.Instance == null) return;

        // 1. Create and populate the game state
        var authoritativeState = new NetworkGameState
        {
            players = GatherPlayerStates()
        };

        if (SpaceShipManager != null)
        {
            authoritativeState.isShipLanded = SpaceShipManager.IsLanded;
            authoritativeState.isShipDoorOpen = SpaceShipManager.IsDoorOpen;
        }


        // 2. Send Player and Game States
        byte[] playerStateBytes = authoritativeState.ToByteArray();
        byte[] playerMessage = new byte[playerStateBytes.Length + 1];
        playerMessage[0] = (byte)NetworkMessageType.GameState;
        Buffer.BlockCopy(playerStateBytes, 0, playerMessage, 1, playerStateBytes.Length);
        BroadcastP2PMessage(playerMessage, EP2PSend.k_EP2PSendUnreliable);

        // 3. Send Monster States
        var monsterUpdateState = new NetworkMonsterUpdateState { monsters = GatherMonsterStates() };
        byte[] monsterStateBytes = monsterUpdateState.ToByteArray();
        byte[] monsterMessage = new byte[monsterStateBytes.Length + 1];
        monsterMessage[0] = (byte)NetworkMessageType.MonsterUpdate;
        Buffer.BlockCopy(monsterStateBytes, 0, monsterMessage, 1, monsterStateBytes.Length);
        BroadcastP2PMessage(monsterMessage, EP2PSend.k_EP2PSendUnreliable);

        // The host is the authority, but it still needs to update its local representation
        // of other players based on the state it has received and is broadcasting.
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UpdateFromGameState(authoritativeState);
        }
    }

    /// <summary>
    /// Executed by the host. Collects the state of all players in the room.
    /// </summary>
    private List<PlayerState> GatherPlayerStates()
    {
        var playerStates = new List<PlayerState>();
        if (PlayerManager.Instance == null || ServerRoomManager.Instance == null) return playerStates;

        foreach (var playerEntry in PlayerManager.Instance.Players)
        {
            if (playerEntry.Value == null) continue; // Skip if player object has been destroyed

            string steamIdStr = playerEntry.Key;
            CSteamID steamId = new CSteamID(ulong.Parse(steamIdStr));
            string byteIdStr = ServerRoomManager.Instance.GetPlayerId(steamId);

            if (!byte.TryParse(byteIdStr, out byte playerId)) continue;

            PlayerState playerState;
            if (playerId == MyPlayerId) // Host's own state
            {
                playerState = GetPlayerStateFromGameObject(playerEntry.Value);
            }
            else // Client's state from the last packet we received
            {
                if (!receivedPlayerStates.TryGetValue(playerId, out playerState))
                {
                    continue; // Skip if we haven't received an update from this client yet
                }
            }

            // Overwrite customization with authoritative data from PlayerManager
            playerState.isMale = PlayerManager.Instance.GetPlayerGender(playerId);
            playerState.modelInfo = PlayerManager.Instance.GetPlayerModelInfo(playerId);

            playerStates.Add(playerState);
        }
        return playerStates;
    }

    /// <summary>
    /// Executed by the host. Collects the state of all active monsters.
    /// </summary>
    private List<MonsterState> GatherMonsterStates()
    {
        var monsterStates = new List<MonsterState>();
        if (SpawnManager.Instance == null) return monsterStates;

        foreach (var monsterGo in SpawnManager.Instance.SpawnedMonsters)
        {
            if (monsterGo == null || !monsterGo.activeInHierarchy) continue;

            var networkMonster = monsterGo.GetComponent<NetworkMonster>();
            if (networkMonster == null) continue;

            // Get Health
            var monsterHealth = monsterGo.GetComponent<MonsterHealth>();
            float currentHp = 0;
            float maxHp = 0;
            if (monsterHealth != null)
            {
                currentHp = monsterHealth.CurrentHP;
                maxHp = monsterHealth._maxHP;
            }
            else
            {
                Debug.LogWarning($"[NetworkManager] Monster {networkMonster.MonsterId} is missing MonsterHealth component!");
            }

            var monsterAnimSync = monsterGo.GetComponent<NetworkMonsterAnimatorSync>();
            byte[] animDataBytes = null;
            if (monsterAnimSync != null)
            {
                var animData = monsterAnimSync.GetAnimationData();
                animDataBytes = NetworkMonsterAnimatorSync.Serialize(animData);
            }

            var monsterState = new MonsterState
            {
                monsterId = networkMonster.MonsterId,
                monsterType = networkMonster.MonsterType,
                position = monsterGo.transform.position,
                rotation = monsterGo.transform.rotation,
                currentHP = currentHp,
                maxHP = maxHp,
                animationData = animDataBytes
            };
            monsterStates.Add(monsterState);
        }
        return monsterStates;
    }

    /// <summary>
    /// Executed by the client. Sends its own input and state to the host.
    /// </summary>
    private void SendClientUpdates(GameObject myPlayerGo)
    {
        PlayerState playerState = GetPlayerStateFromGameObject(myPlayerGo);
        if (playerState.playerId == INVALID_PLAYER_ID) return;

        byte[] stateBytes = playerState.ToByteArray();
        byte[] messageBytes = new byte[stateBytes.Length + 1];
        messageBytes[0] = (byte)NetworkMessageType.PlayerState;
        Buffer.BlockCopy(stateBytes, 0, messageBytes, 1, stateBytes.Length);

        SendP2PMessage(lobbyHostID, messageBytes, EP2PSend.k_EP2PSendUnreliable);
    }

    /// <summary>
    /// Gathers the current state of a player GameObject.
    /// </summary>
    private PlayerState GetPlayerStateFromGameObject(GameObject playerGo)
    {
        var networkPlayer = playerGo.GetComponent<NetworkPlayer>();
        if (networkPlayer == null || networkPlayer.CharacterMove == null || networkPlayer.CharacterMove.Inputs == null || PlayerManager.Instance == null)
        {
            return new PlayerState { playerId = INVALID_PLAYER_ID };
        }

        var playerState = new PlayerState
        {
            playerId = MyPlayerId,
            position = playerGo.transform.position,
            rotation = playerGo.transform.rotation,
            cameraRotation = networkPlayer.CameraTransformSync != null ? networkPlayer.CameraTransformSync.transform.rotation : Quaternion.identity,
            animationMask = networkPlayer.AnimatorSync != null ? networkPlayer.AnimatorSync.GetAnimationMask() : (byte)0,
            moveX = networkPlayer.AnimatorSync != null ? networkPlayer.AnimatorSync.GetHorizontal() : 0,
            moveY = networkPlayer.AnimatorSync != null ? networkPlayer.AnimatorSync.GetVertical() : 0,
            weaponId = networkPlayer.WeaponController != null ? networkPlayer.WeaponController.activeID : 0,
            bending = networkPlayer.CharacterMove.Inputs.GetBending()
        };

        // Also include the current customization. This is used by the client to send its info to the host.
        // The host will then use its authoritative version when broadcasting the game state.
        playerState.isMale = PlayerManager.Instance.GetPlayerGender(MyPlayerId);
        playerState.modelInfo = PlayerManager.Instance.GetPlayerModelInfo(MyPlayerId);

        return playerState;
    }

    #endregion

    #region Public Session Management

    public void SetMyPlayerId(byte id)
    {
        MyPlayerId = id;
    }

    public void CreateSteamLobby()
    {
        if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
        }
    }

    public void JoinSteamLobby(CSteamID lobbyID)
    {
        if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
        {
            SteamMatchmaking.JoinLobby(lobbyID);
        }
    }

    public void Disconnect()
    {
        if (!IsConnected) return;

        IsConnected = false;
        MyPlayerId = INVALID_PLAYER_ID;
        Mode = NetworkMode.None;

        if (m_CurrentLobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(m_CurrentLobbyID);
            m_CurrentLobbyID = CSteamID.Nil;
        }

        // Clean up persistent managers
        if (PlayerManager.Instance != null) PlayerManager.Instance.ClearAllNetworkEntities();
        if (ServerRoomManager.Instance != null) ServerRoomManager.Instance.ClearRoom();

        lobbyMembers.Clear();
        receivedPlayerStates.Clear();

        GameManager.Instance.isMainMenu = true;
        Destroy(NetworkManager.Instance.GetComponent<ServerRoomManager>());

        Debug.Log("[NetworkManager] Disconnected.");
        OnDisconnected?.Invoke();
    }

    #endregion

    #region Steam Lobby Callbacks

    private void OnLobbyCreated(LobbyCreated_t pCallback)
    {
        if (pCallback.m_eResult != EResult.k_EResultOK)
        {
            OnConnectionFailed?.Invoke($"Lobby creation failed: {pCallback.m_eResult}");
            return;
        }

        m_CurrentLobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
        lobbyHostID = selfSteamId;
        IsConnected = true;
        Mode = NetworkMode.Host;

        if (ServerRoomManager.Instance == null)
        {
            gameObject.AddComponent<ServerRoomManager>();
        }
        ServerRoomManager.Instance.Initialize(Mode);
        // Add the host player to the room immediately upon creation.
        ServerRoomManager.Instance.AddHostPlayer(selfSteamId, CustomSteamManager.Instance.PlayerName);

        // SceneManager.LoadScene(GameManager.Instance.GameSettings.spaceroomScene);
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t pCallback)
    {
        JoinSteamLobby(pCallback.m_steamIDLobby);
    }

    private void OnLobbyEnter(LobbyEnter_t pCallback)
    {
        if ((EChatRoomEnterResponse)pCallback.m_EChatRoomEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            OnConnectionFailed?.Invoke($"Failed to enter lobby: {(EChatRoomEnterResponse)pCallback.m_EChatRoomEnterResponse}");
            return;
        }

        m_CurrentLobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
        lobbyHostID = SteamMatchmaking.GetLobbyOwner(m_CurrentLobbyID);

        // Ensure ServerRoomManager exists for both Host and Client BEFORE we use it.
        if (ServerRoomManager.Instance == null)
        {
            gameObject.AddComponent<ServerRoomManager>();
        }

        if (selfSteamId != lobbyHostID)
        {
            Mode = NetworkMode.Client;
            ServerRoomManager.Instance.Initialize(Mode);
            // Now that we know the instance exists and is initialized, we can safely call this.
            ServerRoomManager.Instance.SendNickname();

            // Get local player customization info
            ModelInfo localModelInfo = PlayerCustomizer.Instance.GetLocalPlayerInfo();
            bool isLocalMale = PlayerCustomizer.Instance.IsMale;

            // Create JSON message for customization
            JObject customizationMessage = new JObject
            {
                { "type", "player_customization" },
                { "isMale", isLocalMale },
                { "head", localModelInfo.head },
                { "body", localModelInfo.body },
                { "acc1", localModelInfo.acc1 },
                { "acc2", localModelInfo.acc2 }
            };

            // Send customization data to the host
            SendJsonMessage(lobbyHostID, customizationMessage);
        }
        else
        {
            Mode = NetworkMode.Host;
            ServerRoomManager.Instance.Initialize(Mode);
        }

        UpdateLobbyMembers();
        IsConnected = true;
        OnConnected?.Invoke();

        // SceneManager.LoadScene(GameManager.Instance.GameSettings.spaceroomScene);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t pCallback)
    {
        CSteamID userChanged = new CSteamID(pCallback.m_ulSteamIDUserChanged);
        EChatMemberStateChange stateChange = (EChatMemberStateChange)pCallback.m_rgfChatMemberStateChange;

        // If a player has left, disconnected, or been kicked
        if ((stateChange & (EChatMemberStateChange.k_EChatMemberStateChangeLeft | EChatMemberStateChange.k_EChatMemberStateChangeDisconnected | EChatMemberStateChange.k_EChatMemberStateChangeKicked | EChatMemberStateChange.k_EChatMemberStateChangeBanned)) != 0)
        {
            Debug.Log($"Player {userChanged} left the lobby (Reason: {stateChange}).");

            if (Mode == NetworkMode.Host)
            {
                // Host cleans up the disconnected player's data
                if (ServerRoomManager.Instance != null) ServerRoomManager.Instance.RemovePlayer(userChanged);
                if (PlayerManager.Instance != null) PlayerManager.Instance.RemovePlayer(userChanged.ToString());
            }
            else if (Mode == NetworkMode.Client && userChanged == lobbyHostID)
            {
                // Client gets disconnected if the host leaves
                Debug.LogError("Host has left the lobby. Disconnecting.");
                Disconnect();
            }
        }

        UpdateLobbyMembers();
    }

    private void UpdateLobbyMembers()
    {
        lobbyMembers.Clear();
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(m_CurrentLobbyID);
        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(m_CurrentLobbyID, i);
            lobbyMembers.Add(memberId);
        }
    }

    #endregion

    #region P2P Networking

    /// <summary>
    /// Steam callback when a user wants to establish a P2P connection. We automatically accept.
    /// </summary>
    private void OnP2PSessionRequest(P2PSessionRequest_t pCallback)
    {
        SteamNetworking.AcceptP2PSessionWithUser(pCallback.m_steamIDRemote);
    }

    /// <summary>
    /// Polls Steamworks for available P2P packets and adds them to a thread-safe queue.
    /// </summary>
    private void ListenForP2PPackets()
    {
        uint packetSize;
        while (SteamNetworking.IsP2PPacketAvailable(out packetSize))
        {
            byte[] buffer = new byte[packetSize];
            if (SteamNetworking.ReadP2PPacket(buffer, packetSize, out _, out CSteamID remoteId))
            {
                p2pPacketQueue.Enqueue((remoteId, buffer));
            }
        }
    }

    /// <summary>
    /// Main router for incoming data packets. Deserializes the message type and routes the content.
    /// </summary>
    private void HandleP2PPacket(CSteamID sender, byte[] data)
    {
        if (data.Length == 0) return;
        NetworkMessageType messageType = (NetworkMessageType)data[0];
        byte[] content = new byte[data.Length - 1];
        Buffer.BlockCopy(data, 1, content, 0, content.Length);

        if (Mode == NetworkMode.Host)
        {
            HandleHostPacket(messageType, content, sender);
        }
        else // Client
        {
            HandleClientPacket(messageType, content, sender);
        }
    }

    private void HandleHostPacket(NetworkMessageType messageType, byte[] content, CSteamID sender)
    {
        switch (messageType)
        {
            case NetworkMessageType.PlayerState:
                PlayerState state = PlayerState.FromBytes(content);
                receivedPlayerStates[state.playerId] = state;
                break;
            case NetworkMessageType.JsonMessage:
                string jsonMsg = Encoding.UTF8.GetString(content);
                OnJsonMessageReceived?.Invoke(sender, jsonMsg);
                break;
        }
    }

    private void HandleClientPacket(NetworkMessageType messageType, byte[] content, CSteamID sender)
    {
        switch (messageType)
        {
            case NetworkMessageType.GameState:
                var gameState = NetworkGameState.FromBytes(content);
                if (PlayerManager.Instance != null) PlayerManager.Instance.UpdateFromGameState(gameState);
                if (SpaceShipManager != null)
                {
                    SpaceShipManager.UpdateStateFromNetwork(gameState.isShipLanded, gameState.isShipDoorOpen);
                }
                break;
            case NetworkMessageType.MonsterSpawn:
                var monsterState = MonsterState.FromBytes(content);
                if (PlayerManager.Instance != null) PlayerManager.Instance.SpawnMonsterFromState(monsterState);
                break;
            case NetworkMessageType.MonsterUpdate:
                var monsterUpdateState = NetworkMonsterUpdateState.FromBytes(content);
                if (PlayerManager.Instance != null) PlayerManager.Instance.OnMonsterUpdateReceived(monsterUpdateState);
                break;
            case NetworkMessageType.MonsterDespawn:
                ushort monsterId = BitConverter.ToUInt16(content, 0);
                if (PlayerManager.Instance != null) PlayerManager.Instance.DespawnMonster(monsterId);
                break;
            case NetworkMessageType.JsonMessage:
                string jsonMsg = Encoding.UTF8.GetString(content);
                OnJsonMessageReceived?.Invoke(sender, jsonMsg);
                break;
        }
    }

    /// <summary>
    /// Sends a message containing a JSON object. Sent reliably.
    /// </summary>
    public void SendJsonMessage(CSteamID target, JObject json)
    {
        string jsonString = json.ToString(Newtonsoft.Json.Formatting.None);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        byte[] message = new byte[jsonBytes.Length + 1];
        message[0] = (byte)NetworkMessageType.JsonMessage;
        Buffer.BlockCopy(jsonBytes, 0, message, 1, jsonBytes.Length);
        SendP2PMessage(target, message, EP2PSend.k_EP2PSendReliable);
    }

    /// <summary>
    /// Broadcasts a JSON message to all lobby members. Sent reliably.
    /// </summary>
    public void BroadcastJsonMessage(JObject json)
    {
        string jsonString = json.ToString(Newtonsoft.Json.Formatting.None);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        byte[] message = new byte[jsonBytes.Length + 1];
        message[0] = (byte)NetworkMessageType.JsonMessage;
        Buffer.BlockCopy(jsonBytes, 0, message, 1, jsonBytes.Length);

        BroadcastP2PMessage(message, EP2PSend.k_EP2PSendReliable);

        if (Mode == NetworkMode.Host)
        {
            OnJsonMessageReceived?.Invoke(selfSteamId, jsonString);
        }
    }

    /// <summary>
    /// Broadcasts a command to spawn a specific monster. Sent reliably.
    /// </summary>
    public void BroadcastMonsterSpawn(MonsterState monsterState)
    {
        if (Mode != NetworkMode.Host) return;

        byte[] monsterBytes = monsterState.ToByteArray();
        byte[] message = new byte[monsterBytes.Length + 1];
        message[0] = (byte)NetworkMessageType.MonsterSpawn;
        Buffer.BlockCopy(monsterBytes, 0, message, 1, monsterBytes.Length);

        BroadcastP2PMessage(message, EP2PSend.k_EP2PSendReliable);
    }

    /// <summary>
    /// Broadcasts a command to despawn a specific monster. Sent reliably.
    /// </summary>
    public void BroadcastMonsterDespawn(ushort monsterId)
    {
        if (Mode != NetworkMode.Host) return;

        byte[] idBytes = BitConverter.GetBytes(monsterId);
        byte[] message = new byte[idBytes.Length + 1];
        message[0] = (byte)NetworkMessageType.MonsterDespawn;
        Buffer.BlockCopy(idBytes, 0, message, 1, idBytes.Length);

        BroadcastP2PMessage(message, EP2PSend.k_EP2PSendReliable);
    }

    private void SendP2PMessage(CSteamID target, byte[] data, EP2PSend sendType)
    {
        SteamNetworking.SendP2PPacket(target, data, (uint)data.Length, sendType);
    }

    private void BroadcastP2PMessage(byte[] data, EP2PSend sendType)
    {
        foreach (var member in lobbyMembers)
        {
            if (member != selfSteamId)
            {
                SendP2PMessage(member, data, sendType);
            }
        }
    }

    #endregion
}

/// <summary>
/// Defines the different types of network messages that can be sent.
/// The first byte of any packet corresponds to a value in this enum.
/// </summary>
public enum NetworkMessageType : byte
{
    GameState = 0,      // Unreliable update of all player states
    PlayerState = 1,    // Unreliable update of a single client's state sent to the host
    JsonMessage = 2,    // Reliable message for generic, non-realtime events (e.g., chat, planet proposals)
    MonsterSpawn = 3,   // Reliable command to spawn a monster
    MonsterUpdate = 4,  // Unreliable update of all monster states
    MonsterDespawn = 5  // Reliable command to despawn a monster
}
