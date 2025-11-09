using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine.SceneManagement;

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

    public NetworkMode Mode { get; private set; } = NetworkMode.None;
    public bool IsConnected { get; private set; }

    // --- Queues for thread-safe message handling in Update() ---
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
    public static event Action<ClientConnection, string> OnClientMessageReceived; // Passes the client connection

    public string PlayerId { get; private set; }

    [Header("Connection Settings")]
    public string hostIpAddress = "127.0.0.1";

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
        if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
        {
            m_LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            m_GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            m_LobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
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

        // Client-side processing of UDP messages
        if (Mode == NetworkMode.Client)
        {
            while (udpDataQueue.TryDequeue(out byte[] data))
            {
                var gameState = NetworkGameState.FromBytes(data);
                NetworkPlayerManager.Instance?.UpdateFromGameState(gameState);
            }
        }
    }

    private void FixedUpdate()
    {
        // Host-side: broadcast its own state
        if (Mode != NetworkMode.Host) return;
        if (NetworkPlayerManager.Instance == null) return;
        if (!NetworkPlayerManager.Instance.Players.TryGetValue("0", out GameObject hostGo)) return;

        var gameState = new NetworkGameState();
        var animSync = hostGo.GetComponentInChildren<NetworkAnimatorSync>();

        var playerState = new PlayerState
        {
            playerId = 0,
            position = hostGo.transform.position,
            rotation = hostGo.transform.rotation,
            // Other fields can be added later
        };
        gameState.players.Add(playerState);

        byte[] gameStateBytes = gameState.ToByteArray();
        SendUDPMessage(gameStateBytes);
    }

    private void InitializeMessageHandlers()
    {
        messageHandlers = new Dictionary<string, Action<ClientConnection, JObject>>
        {
            { "player_action", HandlePlayerAction },
            { "assign_id", HandleAssignId }
        };
    }

    private void HandleAssignId(ClientConnection client, JObject data)
    {
        if (Mode != NetworkMode.Client) return;
        string assignedId = data["player_id"]?.ToString();
        if (!string.IsNullOrEmpty(assignedId))
        {
            PlayerId = assignedId;
        }
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
                OnMessageReceived?.Invoke(jsonMsg);
            }
            OnClientMessageReceived?.Invoke(client, jsonMsg);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling server message: {e.Message}\nMessage: {jsonMsg}");
        }
    }

    private void HandlePlayerAction(ClientConnection client, JObject data)
    {
        NetworkPlayerManager.Instance?.RoutePlayerEvent(data);
        if (Mode == NetworkMode.Host)
        {
            string message = data.ToString(Newtonsoft.Json.Formatting.None);
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

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    #region Connection Management

    public PlayerInfo HostPlayerInfo { get; private set; }

    public void StartHost(int port = 8080)
    {
        if (Mode != NetworkMode.None) return;
        Mode = NetworkMode.Host;
        PlayerId = "0";
        HostPlayerInfo = new PlayerInfo
        {
            player_id = PlayerId,
            nickname = CustomSteamManager.Instance.PlayerName,
            is_ready = false
        };
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            hostTcpListenTask = Task.Run(() => ListenForConnections());
            udpClient = new UdpClient(port + 1);
            udpListeningTask = Task.Run(() => ListenForUdpMessages());
            IsConnected = true;
            OnConnected?.Invoke();
            if (CustomSteamManager.Instance != null && CustomSteamManager.Instance.IsSteamInitialized)
            {
                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
            }
            if (ServerRoomManager.Instance == null)
            {
                gameObject.AddComponent<ServerRoomManager>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] Failed to start host: {e.Message}");
            Disconnect();
        }
    }

    public async Task<bool> ConnectAsClient(string ip = "127.0.0.1", int port = 8080)
    {
        if (Mode != NetworkMode.None) return false;
        Mode = NetworkMode.Client;
        try
        {
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ip, port);
            var stream = tcpClient.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);
            tcpListeningTask = Task.Run(() => ListenForTcpMessages());
            udpClient = new UdpClient(0);
            serverUdpEndPoint = new IPEndPoint(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address, port + 1);
            udpListeningTask = Task.Run(() => ListenForUdpMessages());
            byte[] handshake = new byte[1];
            udpClient.Send(handshake, handshake.Length, serverUdpEndPoint);
            IsConnected = true;
            OnConnected?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("--- CONNECTION FAILED ---");
            Debug.LogException(e);
            OnConnectionFailed?.Invoke(e.Message);
            Disconnect();
            return false;
        }
    }

    public void Disconnect()
    {
        if (Mode == NetworkMode.None) return;
        Debug.Log("[NetworkManager] Disconnecting...");
        IsConnected = false;
        NetworkPlayerManager.Instance?.ClearPlayers();
        ServerRoomManager.Instance?.ClearRoom();
        tcpListener?.Stop();
        tcpClient?.Close();
        udpClient?.Close();
        if (m_CurrentLobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(m_CurrentLobbyID);
            m_CurrentLobbyID = CSteamID.Nil;
        }
        foreach (var client in connectedClients) client.TcpClient.Close();
        connectedClients.Clear();
        Mode = NetworkMode.None;
        Debug.Log("[NetworkManager] Disconnected.");
        OnDisconnected?.Invoke();
    }

    #endregion

    #region Steam Lobby Callbacks and Methods

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

    private void OnLobbyCreated(LobbyCreated_t pCallback)
    {
        if (pCallback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[NetworkManager] Lobby creation failed: {pCallback.m_eResult}");
            return;
        }
        m_CurrentLobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(m_CurrentLobbyID, "host_steam_id", SteamUser.GetSteamID().ToString());
        try
        {
            string hostIP = GetLocalIPAddress();
            SteamMatchmaking.SetLobbyData(m_CurrentLobbyID, "host_ip", hostIP);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get and set host IP address: {e.Message}");
        }
        OnLobbyIDUpdated?.Invoke(m_CurrentLobbyID);
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t pCallback)
    {
        JoinSteamLobby(pCallback.m_steamIDLobby);
    }

    private async void OnLobbyEnter(LobbyEnter_t pCallback)
    {
        CSteamID lobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
        if ((EChatRoomEnterResponse)pCallback.m_EChatRoomEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Debug.LogError($"[NetworkManager] Failed to enter lobby: {(EChatRoomEnterResponse)pCallback.m_EChatRoomEnterResponse}");
            return;
        }
        m_CurrentLobbyID = lobbyID;
        if (Mode != NetworkMode.Host)
        {
            string hostSteamIDStr = SteamMatchmaking.GetLobbyData(m_CurrentLobbyID, "host_steam_id");
            string hostIp = SteamMatchmaking.GetLobbyData(m_CurrentLobbyID, "host_ip");
            if (!string.IsNullOrEmpty(hostSteamIDStr))
            {
                if (string.IsNullOrEmpty(hostIp))
                {
                    Debug.LogError("Host IP not found in lobby data. Cannot connect.");
                    return;
                }
                bool connected = await ConnectAsClient(hostIp, 8080);
                if (!connected)
                {
                    Debug.LogError("[NetworkManager] Failed to connect to host after entering lobby.");
                    return;
                }
            }
            else
            {
                Debug.LogError("Host Steam ID not found in lobby data.");
                return;
            }
        }
        SceneManager.LoadScene("RoomScene");
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
            catch (Exception) { break; }
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
            catch (Exception) { break; }
        }
        Debug.Log($"Client {client.PlayerId} disconnected.");
        ServerRoomManager.Instance?.RemovePlayer(client.PlayerId);
        connectedClients.Remove(client);
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
                if (Mode == NetworkMode.Host)
                {
                    var client = connectedClients.FirstOrDefault(c => c.TcpClient.Client.RemoteEndPoint is IPEndPoint tcpEp && tcpEp.Address.Equals(result.RemoteEndPoint.Address));
                    if (client != null && client.UdpEndPoint == null)
                    {
                        client.UdpEndPoint = result.RemoteEndPoint;
                        Debug.Log($"[NetworkManager] Learned UDP endpoint for client {client.PlayerId}: {result.RemoteEndPoint}");
                        continue;
                    }
                }
                udpDataQueue.Enqueue(result.Buffer);
            }
            catch (Exception e)
            {
                Debug.LogError($"UDP Listen Error: {e.Message}");
                break;
            }
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
            tcpMessageQueue.Enqueue((null, message));
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

    public void SendTCPMessageToClient(ClientConnection client, string message)
    {
        if (Mode != NetworkMode.Host || client == null || client.Writer == null) return;
        try
        {
            client.Writer.WriteLine(message);
            client.Writer.Flush();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send message to client {client.PlayerId}: {e.Message}");
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