using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine.SceneManagement;
using System.Text;

public enum NetworkMode
{
    None,
    Client,
    Host
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public NetworkMode Mode { get; private set; } = NetworkMode.None;
    public byte MyPlayerId { get; private set; } = 255; // 0=Host, >0=Client, 255=NotSet
    public bool IsConnected { get; private set; }

    private readonly ConcurrentQueue<(CSteamID, byte[])> p2pPacketQueue = new ConcurrentQueue<(CSteamID, byte[])>();
    private readonly ConcurrentDictionary<byte, PlayerState> receivedPlayerStates = new ConcurrentDictionary<byte, PlayerState>();

    public string PlayerId { get; private set; }
    public CSteamID selfSteamId { get; private set; }

    public static event Action OnConnected;
    public static event Action<string> OnConnectionFailed;
    public static event Action OnDisconnected;
    public static event Action<CSteamID, string> OnJsonMessageReceived;

    private CSteamID m_CurrentLobbyID;
    private List<CSteamID> lobbyMembers = new List<CSteamID>();
    private CSteamID lobbyHostID;

    public CSteamID LobbyHostID { get { return lobbyHostID; } }

    private Callback<LobbyCreated_t> m_LobbyCreated;
    private Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequested;
    private Callback<LobbyEnter_t> m_LobbyEnter;
    private Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;
    private Callback<P2PSessionRequest_t> m_P2PSessionRequest;

    public CSteamID CurrentLobbyID { get { return m_CurrentLobbyID; } }

    public void SetMyPlayerId(byte id)
    {
        MyPlayerId = id;
        // Debug.Log($"[NetworkManager] My Player ID is set to: {MyPlayerId}");
    }

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
            return;
        }
    }

    private void Start()
    {
        if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
        {
            selfSteamId = SteamUser.GetSteamID();
            PlayerId = selfSteamId.ToString();

            m_LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            m_GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            m_LobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            m_P2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
        }
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        Disconnect();
    }

    private void Update()
    {
        ListenForP2PPackets();

        while (p2pPacketQueue.TryDequeue(out var item))
        {
            (CSteamID sender, byte[] data) = item;
            HandleP2PPacket(sender, data);
        }
    }

    private void FixedUpdate()
    {
        if (string.IsNullOrEmpty(PlayerId)) return;
        if (!IsConnected || MyPlayerId == 255 || NetworkPlayerManager.Instance == null) return;

        if (!NetworkPlayerManager.Instance.Players.TryGetValue(PlayerId, out GameObject myPlayerGo)) return;

        // Host Logic
        if (MyPlayerId == 0)
        {
            if (ServerRoomManager.Instance == null) return;

            var authoritativeState = new NetworkGameState();

            // 1. Gather Player States
            foreach (var playerEntry in NetworkPlayerManager.Instance.Players)
            {
                string steamIdStr = playerEntry.Key;
                CSteamID steamId = new CSteamID(ulong.Parse(steamIdStr));
                string byteIdStr = ServerRoomManager.Instance.GetPlayerId(steamId);

                if (!byte.TryParse(byteIdStr, out byte playerId)) continue;

                PlayerState playerState;
                if (playerId == 0) // Host's own state
                {
                    playerState = GetPlayerStateFromGameObject(playerEntry.Value, selfSteamId);
                }
                else // Client's state
                {
                    if (receivedPlayerStates.TryGetValue(playerId, out playerState))
                    {
                        // We have the client's state, use it
                    }
                    else
                    {
                        // Client state not received yet, maybe skip or use last known
                        continue;
                    }
                }
                authoritativeState.players.Add(playerState);
            }

            // 2. Gather Monster States
            if (SpawnManager.Instance != null)
            {
                foreach (var monsterGo in SpawnManager.Instance.SpawnedMonsters)
                {
                    if (monsterGo == null) continue; // Monster might have been destroyed

                    var networkMonster = monsterGo.GetComponent<NetworkMonster>();
                    if (networkMonster == null) continue;

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
                        monsterType = networkMonster.MonsterType, // Get monster type from NetworkMonster
                        position = monsterGo.transform.position,
                        rotation = monsterGo.transform.rotation,
                        animationData = animDataBytes
                    };
                    authoritativeState.monsters.Add(monsterState);
                }
            }

            // 3. Broadcast the combined state
            byte[] gameStateBytes = authoritativeState.ToByteArray();
            byte[] message = new byte[gameStateBytes.Length + 1];
            message[0] = (byte)NetworkMessageType.GameState;
            Buffer.BlockCopy(gameStateBytes, 0, message, 1, gameStateBytes.Length);

            BroadcastP2PMessage(message, EP2PSend.k_EP2PSendUnreliable);

            // 4. Update host's local game state directly
            if (NetworkPlayerManager.Instance != null) NetworkPlayerManager.Instance.UpdateFromGameState(authoritativeState);
        }
        // Client Logic
        else
        {
            PlayerState playerState = GetPlayerStateFromGameObject(myPlayerGo, selfSteamId);

            if (playerState.playerId == 255) return;

            byte[] stateBytes = playerState.ToByteArray();
            byte[] messageBytes = new byte[stateBytes.Length + 1];
            messageBytes[0] = (byte)NetworkMessageType.PlayerState;
            Buffer.BlockCopy(stateBytes, 0, messageBytes, 1, stateBytes.Length);

            SendP2PMessage(lobbyHostID, messageBytes, EP2PSend.k_EP2PSendUnreliable);
        }
    }

    private PlayerState GetPlayerStateFromGameObject(GameObject playerGo, CSteamID steamId)
    {
        var networkPlayer = playerGo.GetComponent<NetworkPlayer>();
        if (networkPlayer == null)
        {
            // Return a default or empty state if the component isn't ready yet
            return new PlayerState { playerId = 255 };
        }

        var animSync = networkPlayer.AnimatorSync;
        var weaponCtrl = networkPlayer.WeaponController;
        var camTransformSync = networkPlayer.CameraTransformSync;

        return new PlayerState
        {
            playerId = MyPlayerId,
            position = playerGo.transform.position,
            rotation = playerGo.transform.rotation,
            cameraRotation = camTransformSync != null ? camTransformSync.transform.rotation : Quaternion.identity,
            animationMask = animSync != null ? animSync.GetAnimationMask() : (byte)0,
            moveX = animSync != null ? animSync.GetHorizontal() : 0,
            moveY = animSync != null ? animSync.GetVertical() : 0,
            weaponId = weaponCtrl != null ? weaponCtrl.activeID : 0
        };
    }

    public void Disconnect()
    {
        if (!IsConnected) return;

        IsConnected = false;
        MyPlayerId = 255;
        Mode = NetworkMode.None;

        if (m_CurrentLobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(m_CurrentLobbyID);
            m_CurrentLobbyID = CSteamID.Nil;
        }

        if (NetworkPlayerManager.Instance != null) NetworkPlayerManager.Instance.ClearAllNetworkEntities();
        if (ServerRoomManager.Instance != null) ServerRoomManager.Instance.ClearRoom();

        lobbyMembers.Clear();
        receivedPlayerStates.Clear();

        Debug.Log("[NetworkManager] Disconnected.");
        OnDisconnected?.Invoke();
    }

    #region Steam Lobby Callbacks and Methods

    public void CreateSteamLobby()
    {
        if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
        }
    }

    private void OnLobbyCreated(LobbyCreated_t pCallback)
    {
        if (pCallback.m_eResult != EResult.k_EResultOK)
        {
            OnConnectionFailed?.Invoke($"Lobby creation failed: {pCallback.m_eResult}");
            return;
        }

        Mode = NetworkMode.Host;
        m_CurrentLobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
        lobbyHostID = selfSteamId;
        IsConnected = true;

        if (ServerRoomManager.Instance == null)
        {
            gameObject.AddComponent<ServerRoomManager>();
        }

        SceneManager.LoadScene(GameManager.Instance.GameSettings.spaceroomScene);
    }

    public void JoinSteamLobby(CSteamID lobbyID)
    {
        if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
        {
            SteamMatchmaking.JoinLobby(lobbyID);
        }
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

        if (selfSteamId != lobbyHostID)
        {
            Mode = NetworkMode.Client;
        }
        else
        {
            // If we are the lobby owner, ensure mode is Host.
            // This can happen if OnLobbyCreated hasn't set it yet in some race conditions.
            Mode = NetworkMode.Host;
        }

        // Ensure ServerRoomManager exists for both Host and Client
        if (ServerRoomManager.Instance == null)
        {
            gameObject.AddComponent<ServerRoomManager>();
        }

        UpdateLobbyMembers();

        IsConnected = true;
        OnConnected?.Invoke();

        SceneManager.LoadScene(GameManager.Instance.GameSettings.spaceroomScene);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t pCallback)
    {
        CSteamID userChanged = new CSteamID(pCallback.m_ulSteamIDUserChanged);
        EChatMemberStateChange stateChange = (EChatMemberStateChange)pCallback.m_rgfChatMemberStateChange;

        // If a player has left, disconnected, or been kicked
        if ((stateChange & (EChatMemberStateChange.k_EChatMemberStateChangeLeft | EChatMemberStateChange.k_EChatMemberStateChangeDisconnected | EChatMemberStateChange.k_EChatMemberStateChangeKicked | EChatMemberStateChange.k_EChatMemberStateChangeBanned)) != 0)
        {
            Debug.Log($"Player {userChanged} left the lobby (Reason: {stateChange}).");

            // If we are the host, we need to clean up the disconnected player
            if (Mode == NetworkMode.Host)
            {
                if (ServerRoomManager.Instance != null)
                {
                    ServerRoomManager.Instance.RemovePlayer(userChanged);
                }
                if (NetworkPlayerManager.Instance != null)
                {
                    NetworkPlayerManager.Instance.RemovePlayer(userChanged.ToString());
                }
            }
            // If we are a client and the user who left was the host, we must disconnect
            else if (Mode == NetworkMode.Client && userChanged == lobbyHostID)
            {
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

    private void OnP2PSessionRequest(P2PSessionRequest_t pCallback)
    {
        SteamNetworking.AcceptP2PSessionWithUser(pCallback.m_steamIDRemote);
    }

    private void ListenForP2PPackets()
    {
        uint packetSize;
        while (SteamNetworking.IsP2PPacketAvailable(out packetSize))
        {
            byte[] buffer = new byte[packetSize];
            CSteamID remoteId;
            if (SteamNetworking.ReadP2PPacket(buffer, packetSize, out uint bytesRead, out remoteId))
            {
                p2pPacketQueue.Enqueue((remoteId, buffer));
            }
        }
    }

    private void HandleP2PPacket(CSteamID sender, byte[] data)
    {
        if (data.Length == 0) return;
        NetworkMessageType messageType = (NetworkMessageType)data[0];
        byte[] content = new byte[data.Length - 1];
        Buffer.BlockCopy(data, 1, content, 0, content.Length);

        // Host receives state from clients
        if (MyPlayerId == 0)
        {
            if (messageType == NetworkMessageType.PlayerState)
            {
                PlayerState state = PlayerState.FromBytes(content);

                receivedPlayerStates[state.playerId] = state;
            }
            else if (messageType == NetworkMessageType.JsonMessage)
            {
                string jsonMsg = Encoding.UTF8.GetString(content);
                OnJsonMessageReceived?.Invoke(sender, jsonMsg);
            }
        }
        // Client receives game state from host
        else
        {
            if (messageType == NetworkMessageType.GameState)
            {
                var gameState = NetworkGameState.FromBytes(content);
                if (NetworkPlayerManager.Instance != null) NetworkPlayerManager.Instance.UpdateFromGameState(gameState);
            }
            else if (messageType == NetworkMessageType.JsonMessage)
            {
                string jsonMsg = Encoding.UTF8.GetString(content);
                OnJsonMessageReceived?.Invoke(sender, jsonMsg);
            }
        }
    }

    public void SendJsonMessage(CSteamID target, JObject json)
    {
        string jsonString = json.ToString(Newtonsoft.Json.Formatting.None);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        byte[] message = new byte[jsonBytes.Length + 1];
        message[0] = (byte)NetworkMessageType.JsonMessage;
        Buffer.BlockCopy(jsonBytes, 0, message, 1, jsonBytes.Length);
        SendP2PMessage(target, message, EP2PSend.k_EP2PSendReliable);
    }

    public void BroadcastJsonMessage(JObject json)
    {
        string jsonString = json.ToString(Newtonsoft.Json.Formatting.None);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        byte[] message = new byte[jsonBytes.Length + 1];
        message[0] = (byte)NetworkMessageType.JsonMessage;
        Buffer.BlockCopy(jsonBytes, 0, message, 1, jsonBytes.Length);

        BroadcastP2PMessage(message, EP2PSend.k_EP2PSendReliable);

        // Host also processes its own JSON messages
        if (Mode == NetworkMode.Host)
        {
            // Debug.Log("NetworkManager: BroadcastJsonMessage() called. Invoking locally for host.");
            OnJsonMessageReceived?.Invoke(selfSteamId, jsonString);
        }
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

public enum NetworkMessageType : byte
{
    GameState = 0,
    PlayerState = 1,
    JsonMessage = 2
}
