using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

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
    public IPEndPoint UdpEndPoint { get; set; } // We need to learn this endpoint
    public string PlayerId { get; set; } // Can be a string or a byte
    // Add more fields like StreamWriter/Reader if needed for TCP comms
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public NetworkMode Mode { get; private set; } = NetworkMode.None;
    public bool IsConnected { get; private set; }

    // --- Queues for thread-safe message handling in Update()
    private readonly ConcurrentQueue<string> tcpMessageQueue = new ConcurrentQueue<string>();
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

    #region Connection Management

    public void StartHost(int port = 8080)
    {
        if (Mode != NetworkMode.None) return;
        Mode = NetworkMode.Host;
        PlayerId = "0"; // Host is player 0
        Debug.Log("Starting as Host...");

        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            hostTcpListenTask = Task.Run(() => ListenForConnections());

            udpClient = new UdpClient(port);
            udpListeningTask = Task.Run(() => ListenForUdpMessages());

            IsConnected = true;
            OnConnected?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start host: " + e.Message);
            Disconnect();
        }
    }

    public void ConnectAsClient(string ip = "127.0.0.1", int port = 8080)
    {
        if (Mode != NetworkMode.None) return;
        Mode = NetworkMode.Client;
        Debug.Log("Connecting as Client...");

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
            Debug.LogError("Failed to connect as client: " + e.ToString());
            OnConnectionFailed?.Invoke(e.Message);
            Disconnect();
        }
    }

    public void Disconnect()
    {
        if (Mode == NetworkMode.None) return;
        Debug.Log("Disconnecting...");

        IsConnected = false;
        NetworkPlayerManager.Instance?.ClearPlayers();

        tcpListener?.Stop();
        tcpClient?.Close();
        udpClient?.Close();

        // Await tasks to finish to prevent race conditions on exit
        // In a real game, you'd use cancellation tokens

        foreach (var client in connectedClients) client.TcpClient.Close();
        connectedClients.Clear();

        Mode = NetworkMode.None;
        Debug.Log("Disconnected.");
        OnDisconnected?.Invoke();
    }

    private void OnApplicationQuit() => Disconnect();

    #endregion

    #region Message Listening

    private async Task ListenForConnections()
    {
        while (Mode == NetworkMode.Host && tcpListener != null)
        {
            try
            {
                TcpClient newTcpClient = await tcpListener.AcceptTcpClientAsync();
                var connection = new ClientConnection { TcpClient = newTcpClient };
                connectedClients.Add(connection);
                Debug.Log($"New client connected: {newTcpClient.Client.RemoteEndPoint}");
                // TODO: Start a separate task to listen for TCP messages from this specific client
            }
            catch (Exception) { break; /* Listener stopped */ }
        }
    }

    private async Task ListenForTcpMessages()
    {
        while (Mode == NetworkMode.Client && tcpClient.Connected)
        {
            try
            {
                string message = await reader.ReadLineAsync();
                if (message == null) break;
                tcpMessageQueue.Enqueue(message);
            }
            catch (Exception) { break; }
        }
        if (Mode == NetworkMode.Client) tcpMessageQueue.Enqueue("__DISCONNECTED__");
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

    #region Message Processing and Sending

    private void Update()
    {
        // Process TCP messages (Lobby, Chat, etc.) - CLIENT ONLY
        while (tcpMessageQueue.TryDequeue(out string message))
        {
            if (message == "__DISCONNECTED__") { Disconnect(); return; }
            HandleLobbyMessage(message);
            OnMessageReceived?.Invoke(message);
        }

        // Process UDP data (Game State)
        while (udpDataQueue.TryDequeue(out byte[] data))
        {
            if (Mode == NetworkMode.Client)
            {
                HandleGameState(data);
            }
            else if (Mode == NetworkMode.Host)
            {
                // TODO: Process client input data
            }
        }

        // Host-specific logic to broadcast game state periodically
        if (Mode == NetworkMode.Host)
        {
            // TODO: This should be on a fixed interval (e.g., 20 times a second)
            // 1. Collect current state from all players, monsters, etc.
            // 2. Create GameState object
            // 3. Serialize to byte array
            // 4. Broadcast to all clients
        }
    }

    private void HandleGameState(byte[] data)
    {
        try
        {
            NetworkGameState state = NetworkGameState.FromBytes(data);
            NetworkPlayerManager.Instance.UpdateFromGameState(state);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deserializing game state: {e.Message}");
        }
    }

    private void HandleLobbyMessage(string message)
    {
        try
        {
            JObject json = JObject.Parse(message);
            string messageType = json["type"]?.ToString();

            switch (messageType)
            {
                case "assign_id":
                    PlayerId = json["player_id"]?.ToString();
                    break;
                case "update_room_info":
                    JArray players = json["players"] as JArray;
                    NetworkPlayerManager.Instance?.UpdatePlayerList(players);
                    break;
                // Handle other lobby/chat messages...
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to handle lobby message: {message}. Error: {e.Message}");
        }
    }

    public void SendTCPMessage(string jsonMessage)
    {
        if (Mode != NetworkMode.Client || !IsConnected) return;
        try
        {
            writer.WriteLine(jsonMessage);
            writer.Flush();
        }
        catch (Exception e) { Debug.LogError("SendTCPMessage failed: " + e.Message); Disconnect(); }
    }

    // Client sends its input to the host
    public void SendUDPData(byte[] data)
    {
        if (Mode != NetworkMode.Client || !IsConnected) return;
        try
        {
            udpClient.Send(data, data.Length, serverUdpEndPoint);
        }
        catch (Exception e) { Debug.LogError("SendUDPData failed: " + e.Message); }
    }

    // Host broadcasts game state to all clients
    public void BroadcastUDPData(byte[] data)
    {
        if (Mode != NetworkMode.Host) return;
        foreach (var client in connectedClients)
        {
            try
            {
                // TODO: Need to get the client's UDP endpoint, which they should tell us in an initial handshake.
                // udpClient.Send(data, data.Length, client.UdpEndPoint);
            }
            catch (Exception e) { Debug.LogError($"BroadcastUDPData failed for a client: {e.Message}"); }
        }
    }

    #endregion
}
