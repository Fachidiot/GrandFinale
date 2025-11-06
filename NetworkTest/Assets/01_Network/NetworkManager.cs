using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Steamworks;

public enum NetworkMode
{
    None,
    Client,
    Host
}

// A simple class to hold information about connected clients for the host
public class ClientConnection
{
    public TcpClient TcpClient { get; set; }
    public StreamWriter Writer { get; set; }
    public StreamReader Reader { get; set; }
    public IPEndPoint UdpEndPoint { get; set; } // We need to learn this endpoint
    public string PlayerId { get; set; } // Can be a string or a byte
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public string LastRoomUpdateInfo { get; set; }

    public NetworkMode Mode { get; private set; } = NetworkMode.None;
    public bool IsConnected { get; private set; }

    // --- Queues for thread-safe message handling in Update()
    private readonly ConcurrentQueue<(ClientConnection, string)> tcpMessageQueue = new ConcurrentQueue<(ClientConnection, string)>();
    private readonly ConcurrentQueue<byte[]> udpDataQueue = new ConcurrentQueue<byte[]>();

    // --- Client-Specific Fields ---
    private TcpClient tcpClient;
    private StreamWriter writer;
    private StreamReader reader;
    private Task tcpListeningTask;
    private IPEndPoint serverUdpEndPoint;

    // --- Host-Specific Fields ---
    private TcpListener tcpListener;
    private List<ClientConnection> connectedClients = new List<ClientConnection>();
    private Task hostTcpListenTask;

    // --- Common Fields ---
    private UdpClient udpClient;
    private Task udpListeningTask;

    // --- Events ---
    public static event Action OnConnected;
    public static event Action<string> OnConnectionFailed;
    public static event Action OnDisconnected;
    public static event Action<string> OnMessageReceived; // For TCP messages primarily

    public string PlayerId { get; private set; }

    // --- Steam Lobby Fields ---
    private CSteamID m_CurrentLobbyID;
    private Callback<LobbyCreated_t> m_LobbyCreated;
    private Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequested;
    private Callback<LobbyEnter_t> m_LobbyEnter;

    public CSteamID CurrentLobbyID { get { return m_CurrentLobbyID; } }

    private Dictionary<string, Action<ClientConnection, JObject>> messageHandlers;

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

        InitializeMessageHandlers();
    }

    private void Start()
    {
        // Initialize Steam Lobby Callbacks
        if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
        {
            Debug.Log("NetworkManager: Initializing Steam Lobby Callbacks...");
            m_LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            m_GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            m_LobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            Debug.Log("NetworkManager: Steam Lobby Callbacks Initialized.");
        }
        else
        {
            Debug.LogWarning("NetworkManager: CustomSteamManager not initialized in Start. Steam Lobby callbacks will not be active.");
        }
    }

    private void Update()
    {
        while (tcpMessageQueue.TryDequeue(out var item))
        {
            (ClientConnection client, string jsonMsg) = item;
            if (jsonMsg == "__DISCONNECTED__")
            {
                Disconnect();
                continue;
            }
            HandleServerMessage(client, jsonMsg);
        }

        while (udpDataQueue.TryDequeue(out byte[] data))
        {
            // Process UDP data (e.g., game state updates)
            // This needs a proper deserialization mechanism based on your data structure
        }
    }

    private void InitializeMessageHandlers()
    {
        messageHandlers = new Dictionary<string, Action<ClientConnection, JObject>>
        {
            { "player_action", HandlePlayerAction }
        };
    }

    private void HandleServerMessage(ClientConnection client, string jsonMsg)
    {
        try
        {
            JObject response = JObject.Parse(jsonMsg);
            string type = response["type"]?.ToString();

            if (messageHandlers.TryGetValue(type, out var handler))
            {
                handler(client, response);
            }
            else
            {
                // Pass to the old system if no handler is found
                OnMessageReceived?.Invoke(jsonMsg);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling server message: {e.Message}\nMessage: {jsonMsg}");
        }
    }

    private void HandlePlayerAction(ClientConnection client, JObject data)
    {
        // Route the event for local processing
        NetworkPlayerManager.Instance?.RoutePlayerEvent(data);

        // If this is the host, broadcast to other clients
        if (Mode == NetworkMode.Host)
        {
            string message = data.ToString();
            foreach (var otherClient in connectedClients)
            {
                if (otherClient != client)
                {
                    otherClient.Writer.WriteLine(message);
                    otherClient.Writer.Flush();
                }
            }
        }
    }

    #region Connection Management

    public void StartHost(int port = 8080)
    {
        if (Mode != NetworkMode.None) return;
        Mode = NetworkMode.Host;
        PlayerId = "0"; // Host is player 0
        Debug.Log($"NetworkManager: Host Player Name: {CustomSteamManager.Instance.PlayerName}");
        HostPlayerInfo = new PlayerInfo
        {
            player_id = PlayerId,
            nickname = CustomSteamManager.Instance.PlayerName,
            is_ready = false
        };
        Debug.Log("NetworkManager: Starting as Host...");

        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            hostTcpListenTask = Task.Run(() => ListenForConnections());

            udpClient = new UdpClient(port);
            udpListeningTask = Task.Run(() => ListenForUdpMessages());

            IsConnected = true;
            OnConnected?.Invoke();

            // Host also creates a Steam Lobby
            if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
            {
                Debug.Log("NetworkManager: Calling SteamMatchmaking.CreateLobby...");
                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4); // Max 4 players
            }
            else
            {
                Debug.LogWarning("NetworkManager: CustomSteamManager not initialized. Cannot create Steam Lobby.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("NetworkManager: Failed to start host: " + e.Message);
            Disconnect();
        }
    }

    public void ConnectAsClient(string ip = "127.0.0.1", int port = 8080)
    {
        if (Mode != NetworkMode.None) return;
        Mode = NetworkMode.Client;
        Debug.Log("NetworkManager: Connecting as Client...");

        try
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(ip, port);
            var stream = tcpClient.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);
            tcpListeningTask = Task.Run(() => ListenForTcpMessages());

            var localUdpPort = ((IPEndPoint)tcpClient.Client.LocalEndPoint).Port;
            udpClient = new UdpClient(localUdpPort);
            serverUdpEndPoint = new IPEndPoint(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address, port);
            udpListeningTask = Task.Run(() => ListenForUdpMessages());

            IsConnected = true;
            OnConnected?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("NetworkManager: Failed to connect as client: " + e.ToString());
            OnConnectionFailed?.Invoke(e.Message);
            Disconnect();
        }
    }

    public void Disconnect()
    {
        if (Mode == NetworkMode.None) return;
        Debug.Log("NetworkManager: Disconnecting...");

        IsConnected = false;
        NetworkPlayerManager.Instance?.ClearPlayers();

        tcpListener?.Stop();
        tcpClient?.Close();
        udpClient?.Close();

        // Leave Steam Lobby if connected
        if (m_CurrentLobbyID.IsValid())
        {
            Debug.Log($"NetworkManager: Leaving Steam Lobby: {m_CurrentLobbyID}");
            SteamMatchmaking.LeaveLobby(m_CurrentLobbyID);
            m_CurrentLobbyID = CSteamID.Nil;
        }

        // Await tasks to finish to prevent race conditions on exit
        // In a real game, you'd use cancellation tokens

        foreach (var client in connectedClients) client.TcpClient.Close();
        connectedClients.Clear();

        Mode = NetworkMode.None;
        Debug.Log("NetworkManager: Disconnected.");
        OnDisconnected?.Invoke();
    }

    public PlayerInfo HostPlayerInfo { get; private set; }

    #endregion

    #region Steam Lobby Callbacks and Methods

    public void CreateSteamLobby()
    {
        if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
        {
            Debug.Log("NetworkManager: Calling SteamMatchmaking.CreateLobby from CreateSteamLobby()...");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
        }
        else
        {
            Debug.LogWarning("NetworkManager: Steam not initialized. Cannot create lobby.");
        }
    }

    public void JoinSteamLobby(CSteamID lobbyID)
    {
        if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
        {
            Debug.Log($"NetworkManager: Calling SteamMatchmaking.JoinLobby for ID: {lobbyID}");
            SteamMatchmaking.JoinLobby(lobbyID);
        }
        else
        {
            Debug.LogWarning("NetworkManager: Steam not initialized. Cannot join lobby.");
        }
    }

    private void OnLobbyCreated(LobbyCreated_t pCallback)
    {
        Debug.Log($"NetworkManager: OnLobbyCreated callback received. Result: {pCallback.m_eResult}");
        if (pCallback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"NetworkManager: Lobby creation failed: {pCallback.m_eResult}");
            OnConnectionFailed?.Invoke($"Lobby creation failed: {pCallback.m_eResult}");
            return;
        }

        m_CurrentLobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
        Debug.Log($"NetworkManager: Lobby created! ID: {m_CurrentLobbyID}");

        // Set lobby data (e.g., host's Steam ID, IP/Port if not using Steam P2P)
        SteamMatchmaking.SetLobbyData(m_CurrentLobbyID, "host_steam_id", SteamUser.GetSteamID().ToString());

        OnLobbyIDUpdated?.Invoke(m_CurrentLobbyID);
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t pCallback)
    {
        Debug.Log($"NetworkManager: Game Lobby Join Requested. Lobby ID: {pCallback.m_steamIDLobby}");
        JoinSteamLobby(pCallback.m_steamIDLobby);
    }

    private void OnLobbyEnter(LobbyEnter_t pCallback)
    {
        CSteamID lobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
        Debug.Log($"NetworkManager: OnLobbyEnter callback received. Lobby ID: {lobbyID}. Result: {(EChatRoomEnterResponse)pCallback.m_EChatRoomEnterResponse}");
        if ((EChatRoomEnterResponse)pCallback.m_EChatRoomEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Debug.LogError($"NetworkManager: Failed to enter lobby: {(EChatRoomEnterResponse)pCallback.m_EChatRoomEnterResponse}");
            OnConnectionFailed?.Invoke($"Failed to enter lobby: {(EChatRoomEnterResponse)pCallback.m_EChatRoomEnterResponse}");
            return;
        }

        m_CurrentLobbyID = lobbyID;
        Debug.Log($"NetworkManager: Entered Lobby! ID: {m_CurrentLobbyID}");

        // If client, connect to the host's game server
        if (Mode == NetworkMode.Client)
        {
            string hostSteamIDStr = SteamMatchmaking.GetLobbyData(m_CurrentLobbyID, "host_steam_id");
            if (!string.IsNullOrEmpty(hostSteamIDStr))
            {
                // In a real game, you'd use Steam P2P or get the host's IP/Port from lobby data
                // For now, we'll assume direct connect to localhost for testing
                ConnectAsClient("127.0.0.1", 8080);
            }
            else
            {
                Debug.LogError("NetworkManager: Host Steam ID not found in lobby data.");
                OnConnectionFailed?.Invoke("Host Steam ID not found in lobby data.");
            }
        }
    }

    public static event Action<CSteamID> OnLobbyIDUpdated;

    #endregion

    #region Message Listening

    private async Task ListenForConnections()
    {
        while (Mode == NetworkMode.Host && tcpListener != null)
        {
            try
            {
                TcpClient newTcpClient = await tcpListener.AcceptTcpClientAsync();
                var stream = newTcpClient.GetStream();
                var connection = new ClientConnection
                {
                    TcpClient = newTcpClient,
                    Writer = new StreamWriter(stream),
                    Reader = new StreamReader(stream)
                };
                connectedClients.Add(connection);
                Debug.Log($"New client connected: {newTcpClient.Client.RemoteEndPoint}");
                Task.Run(() => ListenForClientTcpMessages(connection));
            }
            catch (Exception) { break; /* Listener stopped */ }
        }
    }

    private async Task ListenForClientTcpMessages(ClientConnection client)
    {
        while (client.TcpClient.Connected)
        {
            try
            {
                string message = await client.Reader.ReadLineAsync();
                if (message == null) break;
                tcpMessageQueue.Enqueue((client, message));
            }
            catch (Exception)
            {
                break;
            }
        }
        // Handle client disconnection
        connectedClients.Remove(client);
        Debug.Log($"Client {client.PlayerId} disconnected.");
    }

    private async Task ListenForTcpMessages()
    {
        while (Mode == NetworkMode.Client && tcpClient.Connected)
        {
            try
            {
                string message = await reader.ReadLineAsync();
                if (message == null) break;
                tcpMessageQueue.Enqueue((null, message));
            }
            catch (Exception) { break; }
        }
        if (Mode == NetworkMode.Client) tcpMessageQueue.Enqueue((null, "__DISCONNECTED__"));
    }

    private async Task ListenForUdpMessages()
    {
        while (udpClient != null)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                // In Host mode, we need to identify which client sent the data.
                // The result.RemoteEndPoint tells us who sent it.
                // We can then process their input.
                // For now, we assume all UDP data is game state for the client.
                udpDataQueue.Enqueue(result.Buffer);
            }
            catch (Exception) { break; }
        }
    }

    #endregion

    #region Message Sending

    public void SendTCPMessage(string message)
    {
        if (Mode == NetworkMode.Client)
        {
            if (writer != null)
            {
                writer.WriteLine(message);
                writer.Flush();
            }
        }
        else if (Mode == NetworkMode.Host)
        {
            foreach (var client in connectedClients)
            {
                if (client.Writer != null)
                {
                    client.Writer.WriteLine(message);
                    client.Writer.Flush();
                }
            }
        }
    }

    public void SendUDPMessage(byte[] data)
    {
        if (Mode == NetworkMode.Client)
        {
            udpClient.Send(data, data.Length, serverUdpEndPoint);
        }
        else if (Mode == NetworkMode.Host)
        {
            foreach (var client in connectedClients)
            {
                if (client.UdpEndPoint != null)
                {
                    udpClient.Send(data, data.Length, client.UdpEndPoint);
                }
            }
        }
    }

    #endregion
}