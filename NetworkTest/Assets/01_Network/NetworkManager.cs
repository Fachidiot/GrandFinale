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

    private Callback<LobbyCreated_t> m_LobbyCreated;
    private Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequested;
    private Callback<LobbyEnter_t> m_LobbyEnter;
    private Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;
    private Callback<P2PSessionRequest_t> m_P2PSessionRequest;

    public CSteamID CurrentLobbyID { get { return m_CurrentLobbyID; } }

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
        if (!IsConnected || NetworkPlayerManager.Instance == null || ServerRoomManager.Instance == null) return;

        if (!NetworkPlayerManager.Instance.Players.TryGetValue(PlayerId, out GameObject myPlayerGo)) return;
        PlayerState myState = GetPlayerStateFromGameObject(myPlayerGo, selfSteamId);

        if (Mode == NetworkMode.Host)
        {
            string myByteIdStr = ServerRoomManager.Instance.GetPlayerId(selfSteamId);
            if (byte.TryParse(myByteIdStr, out byte myByteId))
            {
                receivedPlayerStates[myByteId] = myState;
            }

            var authoritativeState = new NetworkGameState();
            foreach (var playerState in receivedPlayerStates.Values)
            {
                authoritativeState.players.Add(playerState);
            }
            
            byte[] gameStateBytes = authoritativeState.ToByteArray();
            byte[] message = new byte[gameStateBytes.Length + 1];
            message[0] = (byte)MessageType.GameState;
            Buffer.BlockCopy(gameStateBytes, 0, message, 1, gameStateBytes.Length);

            BroadcastP2PMessage(message, EP2PSend.k_EP2PSendUnreliable);
        }
        else if (Mode == NetworkMode.Client)
        {
            byte[] stateBytes = myState.ToByteArray();
            byte[] message = new byte[stateBytes.Length + 1];
            message[0] = (byte)MessageType.PlayerState;
            Buffer.BlockCopy(stateBytes, 0, message, 1, stateBytes.Length);

            SendP2PMessage(lobbyHostID, message, EP2PSend.k_EP2PSendUnreliable);
        }
    }

    private PlayerState GetPlayerStateFromGameObject(GameObject playerGo, CSteamID steamId)
    {
        var animSync = playerGo.GetComponentInChildren<NetworkAnimatorSync>();
        var weaponCtrl = playerGo.GetComponentInChildren<WeaponController>();
        var camTransformSync = playerGo.GetComponentsInChildren<NetworkTransformSync>().FirstOrDefault(s => s.viewId == 1);
        
        string byteIdStr = ServerRoomManager.Instance.GetPlayerId(steamId);

        return new PlayerState
        {
            playerId = byte.TryParse(byteIdStr, out byte id) ? id : (byte)255,
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
        Debug.Log("[NetworkManager] Disconnecting...");

        IsConnected = false;
        
        if (m_CurrentLobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(m_CurrentLobbyID);
            m_CurrentLobbyID = CSteamID.Nil;
        }

        NetworkPlayerManager.Instance?.ClearPlayers();
        ServerRoomManager.Instance?.ClearRoom();
        lobbyMembers.Clear();
        receivedPlayerStates.Clear();
        
        Mode = NetworkMode.None;
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
        Debug.Log($"[NetworkManager] Lobby created! ID: {m_CurrentLobbyID}");
        
        lobbyHostID = selfSteamId;
        IsConnected = true;
        OnConnected?.Invoke();
        
        if (ServerRoomManager.Instance == null)
        {
            gameObject.AddComponent<ServerRoomManager>();
        }
        
        SceneManager.LoadScene("RoomScene");
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
        
        Debug.Log($"[NetworkManager] Entered lobby {m_CurrentLobbyID}. Host is {lobbyHostID}");
        
        UpdateLobbyMembers();

        IsConnected = true;
        OnConnected?.Invoke();

        if (Mode == NetworkMode.Host && ServerRoomManager.Instance == null)
        {
             gameObject.AddComponent<ServerRoomManager>();
        }

        SceneManager.LoadScene("RoomScene");
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t pCallback)
    {
        CSteamID userChanged = new CSteamID(pCallback.m_ulSteamIDUserChanged);

        if ((EChatMemberStateChange)pCallback.m_rgfChatMemberStateChange == EChatMemberStateChange.k_EChatMemberStateChangeEntered)
        {
            Debug.Log($"Player {userChanged} entered the lobby.");
        }
        else
        {
            Debug.Log($"Player {userChanged} left the lobby.");
            
            if (Mode == NetworkMode.Host)
            {
                // Host removes the player who left
                ServerRoomManager.Instance?.RemovePlayer(userChanged);
            }
            else if (Mode == NetworkMode.Client)
            {
                // Client checks if the host was the one who left
                if (userChanged == lobbyHostID)
                {
                    Debug.LogError("Host has left the lobby. Disconnecting.");
                    Disconnect();
                }
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
        MessageType messageType = (MessageType)data[0];
        byte[] content = new byte[data.Length - 1];
        Buffer.BlockCopy(data, 1, content, 0, content.Length);

        if (Mode == NetworkMode.Host)
        {
            if (messageType == MessageType.PlayerState)
            {
                PlayerState state = PlayerState.FromBytes(content);
                receivedPlayerStates[state.playerId] = state;
            }
            else if (messageType == MessageType.JsonMessage)
            {
                string jsonMsg = Encoding.UTF8.GetString(content);
                OnJsonMessageReceived?.Invoke(sender, jsonMsg);
            }
        }
        else // Client
        {
            if (messageType == MessageType.GameState)
            {
                var gameState = NetworkGameState.FromBytes(content);
                NetworkPlayerManager.Instance?.UpdateFromGameState(gameState);
            }
            else if (messageType == MessageType.JsonMessage)
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
        message[0] = (byte)MessageType.JsonMessage;
        Buffer.BlockCopy(jsonBytes, 0, message, 1, jsonBytes.Length);
        SendP2PMessage(target, message, EP2PSend.k_EP2PSendReliable);
    }
    
    public void BroadcastJsonMessage(JObject json)
    {
        string jsonString = json.ToString(Newtonsoft.Json.Formatting.None);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        byte[] message = new byte[jsonBytes.Length + 1];
        message[0] = (byte)MessageType.JsonMessage;
        Buffer.BlockCopy(jsonBytes, 0, message, 1, jsonBytes.Length);
        
        // Broadcast to remote peers
        BroadcastP2PMessage(message, EP2PSend.k_EP2PSendReliable);

        // Process locally for the host
        if (Mode == NetworkMode.Host)
        {
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

public enum MessageType : byte
{
    GameState = 0,
    PlayerState = 1,
    JsonMessage = 2
}
