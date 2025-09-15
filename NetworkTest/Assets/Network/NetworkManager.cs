using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    // TCP-related fields
    private TcpClient tcpClient;
    private StreamWriter writer;
    private StreamReader reader;
    private Task tcpListeningTask;
    private readonly ConcurrentQueue<string> tcpMessageQueue = new ConcurrentQueue<string>();

    // UDP-related fields
    private UdpClient udpClient;
    private IPEndPoint serverUdpEndPoint;
    private Task udpListeningTask;
    private readonly ConcurrentQueue<string> udpMessageQueue = new ConcurrentQueue<string>();

    // Events
    public static event Action OnConnected;
    public static event Action<string> OnConnectionFailed;
    public static event Action OnDisconnected;
    public static event Action<string> OnMessageReceived;

    // Player and Room Info
    public string PlayerId { get; private set; }
    public List<string> PlayerIdsInRoom { get; private set; } = new List<string>();
    public string LastRoomUpdateInfo { get; set; }

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

    public void Connect(string ip = "127.0.0.1", int tcpPort = 8080, int udpPort = 8081)
    {
        if (tcpClient != null && tcpClient.Connected)
        {
            Debug.LogWarning("Already connected.");
            return;
        }

        try
        {
            // TCP Connection
            tcpClient = new TcpClient();
            tcpClient.Connect(ip, tcpPort);
            NetworkStream stream = tcpClient.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);
            tcpListeningTask = Task.Run(() => ListenForTcpMessages());
            Debug.Log("Successfully connected to the server via TCP.");

            // UDP Setup
            udpClient = new UdpClient(tcpClient.Client.LocalEndPoint as IPEndPoint);
            serverUdpEndPoint = new IPEndPoint(IPAddress.Parse(ip), udpPort);
            udpListeningTask = Task.Run(() => ListenForUdpMessages());
            // Debug.Log("UDP listener started.");

            OnConnected?.Invoke();
        }
        catch (SocketException e)
        {
            Debug.LogError("SocketException: " + e.ToString());
            OnConnectionFailed?.Invoke(e.Message);
            Disconnect();
        }
    }

    public void Disconnect()
    {
        if (tcpClient == null) return;

        GameManager.Instance?.ClearPlayers();

        try
        {
            tcpClient.Close();
            udpClient?.Close();
        }
        catch (Exception e)
        {
            Debug.LogError("Error while disconnecting: " + e.Message);
        }
        finally
        {
            tcpClient = null;
            writer = null;
            reader = null;
            udpClient = null;
            Debug.Log("Disconnected from server.");
            OnDisconnected?.Invoke();
        }
    }

    private async Task ListenForTcpMessages()
    {
        while (tcpClient != null && tcpClient.Connected)
        {
            try
            {
                string message = await reader.ReadLineAsync();
                if (message != null)
                {
                    tcpMessageQueue.Enqueue(message);
                }
                else break;
            }
            catch (IOException) { break; }
            catch (Exception e)
            {
                Debug.LogError("Error receiving TCP message: " + e.Message);
                break;
            }
        }
        tcpMessageQueue.Enqueue("__DISCONNECTED__");
    }

    private async Task ListenForUdpMessages()
    {
        while (udpClient != null)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string message = Encoding.UTF8.GetString(result.Buffer);
                udpMessageQueue.Enqueue(message);
            }
            catch (ObjectDisposedException) { break; } // UdpClient was closed.
            catch (Exception e)
            {
                Debug.LogError("Error receiving UDP message: " + e.Message);
            }
        }
    }

    private void Update()
    {
        // Process TCP messages
        while (tcpMessageQueue.TryDequeue(out string message))
        {
            if (message == "__DISCONNECTED__")
            {
                Disconnect();
                continue;
            }
            HandleMessage(message);
            OnMessageReceived?.Invoke(message);
        }

        // Process UDP messages
        while (udpMessageQueue.TryDequeue(out string message))
        {
            HandleMessage(message);
        }
    }

    private void HandleMessage(string message)
    {
        try
        {
            JObject json = JObject.Parse(message);
            string messageType = json["type"]?.ToString();

            switch (messageType)
            {
                case "assign_id":
                    PlayerId = json["player_id"]?.ToString();
                    // Debug.Log($"My ID is: {PlayerId}");
                    break;
                case "update_room_info":
                    JArray players = json["players"] as JArray;
                    GameManager.Instance?.UpdatePlayerList(players);
                    break;
                case "game_start":
                    // TODO: Game start logic
                    break;
                case "leave_room_success":
                    GameManager.Instance?.ClearPlayers();
                    break;
                case "player_event":
                    GameManager.Instance?.RoutePlayerEvent(json);
                    break;
                case "game_state": // Renamed from transform_update
                    JArray playersState = json["updates"] as JArray;
                    GameManager.Instance?.UpdatePlayersState(playersState);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to handle message: {message}. Error: {e.Message}");
        }
    }

    public void SendMessageToServer(string jsonMessage)
    {
        if (writer != null && tcpClient != null && tcpClient.Connected)
        {
            try
            {
                writer.WriteLine(jsonMessage);
                writer.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send TCP message: " + e.Message);
                Disconnect();
            }
        }
        else
        {
            Debug.LogError("Not connected to the server (TCP).");
        }
    }

    public async void SendUdpMessage(string jsonMessage)
    {
        if (udpClient != null && serverUdpEndPoint != null)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
                await udpClient.SendAsync(data, data.Length, serverUdpEndPoint);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send UDP message: " + e.Message);
            }
        }
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }
}
