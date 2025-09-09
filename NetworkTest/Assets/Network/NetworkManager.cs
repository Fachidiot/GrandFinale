using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    private TcpClient client;
    private StreamWriter writer;
    private StreamReader reader;
    private Task listeningTask;

    private readonly ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    public static event Action OnConnected;
    public static event Action<string> OnConnectionFailed;
    public static event Action OnDisconnected;
    public static event Action<string> OnMessageReceived;

    public string PlayerId { get; private set; }
    public List<string> PlayerIdsInRoom { get; private set; } = new List<string>();

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

    public void Connect(string ip = "127.0.0.1", int port = 8080)
    {   // ip주소로 서버에 접속을 시도
        if (client != null && client.Connected)
        {
            Debug.LogWarning("Already connected.");
            return;
        }

        try
        {
            client = new TcpClient();
            client.Connect(ip, port);

            NetworkStream stream = client.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);

            listeningTask = Task.Run(() => ListenForServerMessages());

            Debug.Log("Successfully connected to the server.");
            OnConnected?.Invoke();
        }
        catch (SocketException e)
        {
            Debug.LogError("SocketException: " + e.ToString());
            OnConnectionFailed?.Invoke(e.Message);
            client = null;
            OnDisconnected?.Invoke();
        }
    }

    public void Disconnect()
    {   // 안전하게 서버와의 연결을 종료
        if (client == null || !client.Connected)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearPlayers();
        }

        try
        {
            client.Close();
        }
        catch (Exception e)
        {
            Debug.LogError("Error while disconnecting: " + e.Message);
        }
        finally
        {
            client = null;
            writer = null;
            reader = null;
            Debug.Log("Disconnected from server.");
            OnDisconnected?.Invoke();
        }
    }

    private async Task ListenForServerMessages()
    {   // Task로 서버로부터 메시지를 받는 함수
        while (client != null && client.Connected)
        {
            try
            {
                string message = await reader.ReadLineAsync();
                if (message != null)
                {
                    messageQueue.Enqueue(message);
                }
                else
                {
                    // Stream is closed
                    break;
                }
            }
            catch (IOException)
            {
                // Connection lost
                break;
            }
            catch (Exception e)
            {
                Debug.LogError("Error receiving message: " + e.Message);
                break;
            }
        }

        // while loop문을 나왔다는 뜻은, 서버로부터 연결이 끊겼다는걸 의미한다.
        // 메인 스레드에서 연결 해제 로직이 실행될수 있도록 메시지큐에 추가한다.
        messageQueue.Enqueue("__DISCONNECTED__");
    }

    private void Update()
    {
        while (messageQueue.TryDequeue(out string message))
        {
            if (message == "__DISCONNECTED__")
            {
                Disconnect();
                continue;
            }

            try
            {
                JObject json = JObject.Parse(message);
                string messageType = json["type"]?.ToString();

                switch (messageType)
                {
                    case "assign_id":
                        PlayerId = json["player_id"]?.ToString();
                        Debug.Log($"My ID is: {PlayerId}");
                        continue;
                    case "update_room_info":
                        JArray players = json["players"] as JArray;
                        if (GameManager.Instance != null && players != null)
                        {
                            GameManager.Instance.UpdatePlayerList(players);
                        }
                        break;
                    case "game_start":
                        // Game start logic can go here if needed, but spawning is now handled by room updates
                        continue;
                    case "leave_room_success":
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.ClearPlayers();
                        }
                        break;
                    case "player_moved":
                        string playerId = json["player_id"].ToString();
                        JObject pos = json["position"] as JObject;
                        Vector3 position = new Vector3(pos["x"].Value<float>(), pos["y"].Value<float>(), pos["z"].Value<float>());
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.UpdatePlayerPosition(playerId, position);
                        }
                        continue;
                }
            }
            catch (Exception)
            {
                // Not a json message or has no type, just pass it on
            }

            OnMessageReceived?.Invoke(message);
        }
    }

    public void SendMessageToServer(string jsonMessage)
    {   // 서버로 메시지 전송
        if (writer != null && client != null && client.Connected)
        {
            try
            {
                writer.WriteLine(jsonMessage);
                writer.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send message: " + e.Message);
                Disconnect();
            }
        }
        else
        {
            Debug.LogError("Not connected to the server.");
        }
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }
}
