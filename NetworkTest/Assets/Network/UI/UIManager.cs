using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Connection UI")]
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private TMP_InputField nickNameInput;
    [SerializeField] private Button connectButton;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Lobby UI")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private TMP_InputField roomNameInput;

    [Header("Room List UI")]
    [SerializeField] private Transform roomListContainer;

    [Header("In-Room UI")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI chatText;
    [SerializeField] private TMP_InputField chatMessageInput;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Transform playerListContainer;

    private Dictionary<string, Action<string>> messageHandlers;
    private string ip = "127.0.0.1";

    // private values
    private int player_count = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeMessageHandlers();
    }

    private void Start()
    {
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);
    }

    private void OnEnable()
    {
        NetworkManager.OnConnected += HandleConnection;
        NetworkManager.OnConnectionFailed += HandleConnectionFailed;
        NetworkManager.OnDisconnected += HandleDisconnection;
        NetworkManager.OnMessageReceived += HandleServerMessage;
    }

    private void OnDisable()
    {
        NetworkManager.OnConnected -= HandleConnection;
        NetworkManager.OnConnectionFailed -= HandleConnectionFailed;
        NetworkManager.OnDisconnected -= HandleDisconnection;
        NetworkManager.OnMessageReceived -= HandleServerMessage;
    }

    private void InitializeMessageHandlers()
    {
        messageHandlers = new Dictionary<string, Action<string>>
        {
            { "update_room_info", HandleUpdateRoomInfo },
            { "player_joined", HandlePlayerJoined },
            { "player_left", HandlePlayerLeft },
            { "find_rooms_response", HandleFindRoomsResponse },
            { "leave_room_success", HandleLeaveRoomSuccess },
            { "chat_broadcast", HandleChatBroadcast },
            { "game_start", HandleGameStart }
        };
    }

    #region Connection and UI Panel Management

    private void HandleConnection()
    {
        connectionPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
        errorText.gameObject.SetActive(false);

        string nickname = string.IsNullOrEmpty(nickNameInput.text) ? $"Player{UnityEngine.Random.Range(100, 1000)}" : nickNameInput.text;
        JObject request = new JObject();
        request["type"] = "set_nickname";
        request["nickname"] = nickname;
        NetworkManager.Instance.SendMessageToServer(request.ToString());
    }

    private void HandleConnectionFailed(string errorMessage)
    {
        errorText.text = $"Connection Failed: {errorMessage}";
        errorText.gameObject.SetActive(true);
        connectButton.interactable = true;
    }

    private void HandleDisconnection()
    {
        connectionPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);
        errorText.text = "Disconnected from server.";
        errorText.gameObject.SetActive(true);
        connectButton.interactable = true;
    }

    public void OnConnectButtonClicked()
    {
        errorText.gameObject.SetActive(false);
        connectButton.interactable = false;
        NetworkManager.Instance.Connect(ip);
    }

    #endregion

    #region Message Handling

    private void HandleServerMessage(string jsonMsg)
    {
        JObject response = JObject.Parse(jsonMsg);
        string type = response["type"]?.ToString();

        if (messageHandlers.TryGetValue(type, out var handler))
        {
            handler(jsonMsg);
        }
        else
        {
            Debug.LogWarning($"No handler for message type: {type}");
        }
    }

    private async void HandleUpdateRoomInfo(string json)
    {
        var payload = JsonConvert.DeserializeObject<UpdateRoomInfoPayload>(json);
        player_count = payload.players.Count;

        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
        roomNameText.text = payload.room_name;

        bool amIHost = NetworkManager.Instance.PlayerId == payload.host_id;
        startGameButton.gameObject.SetActive(amIHost);
        readyButton.gameObject.SetActive(!amIHost);

        // Update player list
        foreach (Transform child in playerListContainer)
        {
            Addressables.ReleaseInstance(child.gameObject);
        }

        foreach (var playerInfo in payload.players)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync("Assets/Network Scripts/PlayerListItem.prefab", playerListContainer);
            GameObject playerItemGO = await handle.Task;
            playerItemGO.GetComponent<PlayerListItem>().Setup(playerInfo, playerInfo.player_id == payload.host_id);
        }
    }

    private void HandlePlayerJoined(string json)
    {
        Debug.Log("Handle Player Join");
        var payload = JsonConvert.DeserializeObject<PlayerJoinedPayload>(json);
        chatText.text += $"--- {payload.player_id} has joined the room. ---\n";
    }

    private void HandlePlayerLeft(string json)
    {
        Debug.Log("Handle Player Left");
        var payload = JsonConvert.DeserializeObject<PlayerLeftPayload>(json);
        chatText.text += $"--- {payload.player_id} has left the room. ---\n";
    }

    private async void HandleFindRoomsResponse(string json)
    {
        Debug.Log("Handle Find Rooms");
        var payload = JsonConvert.DeserializeObject<FindRoomsResponse>(json);

        foreach (Transform child in roomListContainer)
        {
            Addressables.ReleaseInstance(child.gameObject);
        }

        foreach (var roomInfo in payload.rooms)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync("Assets/Network Scripts/RoomList Item.prefab", roomListContainer);
            GameObject roomItemGO = await handle.Task;
            roomItemGO.GetComponent<RoomListItem>().Setup(roomInfo);
        }
    }

    private void HandleLeaveRoomSuccess(string json)
    {
        Debug.Log("Handle Leave Room");
        roomPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        chatText.text = "";
    }

    private void HandleChatBroadcast(string json)
    {
        Debug.Log("Handle Chat Broadcast");
        var payload = JsonConvert.DeserializeObject<ChatBroadcastPayload>(json);
        chatText.text += $"[{payload.sender_id}]: {payload.message}\n";
    }

    private void HandleGameStart(string json)
    {
        Debug.Log("Handle Game Start");
        chatText.text += "--- The game is starting! ---\n";
        // Add actual game start logic here, like loading a new scene
    }

    #endregion

    #region UI Button Clicks

    public void OnCreateRoomClicked()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
            return;

        JObject request = new JObject();
        request["type"] = "create_room";
        request["room_name"] = roomName;
        NetworkManager.Instance.SendMessageToServer(request.ToString());
    }

    public void OnFindRoomsClicked()
    {
        JObject request = new JObject();
        request["type"] = "find_rooms";
        NetworkManager.Instance.SendMessageToServer(request.ToString());
    }

    public void JoinRoomById(int roomId)
    {
        JObject request = new JObject();
        request["type"] = "join_room";
        request["room_id"] = roomId;
        NetworkManager.Instance.SendMessageToServer(request.ToString());
    }

    public void OnSendChatMessageClicked()
    {
        string message = chatMessageInput.text;
        if (string.IsNullOrEmpty(message)) return;

        JObject request = new JObject();
        request["type"] = "chat_message";
        request["message"] = message;
        NetworkManager.Instance.SendMessageToServer(request.ToString());
        chatMessageInput.text = "";
    }

    public void OnReadyButtonClicked()
    {
        JObject request = new JObject();
        request["type"] = "toggle_ready";
        NetworkManager.Instance.SendMessageToServer(request.ToString());
    }

    public void OnStartGameButtonClicked()
    {
        if (player_count <= 1)
        {   // Multiplay를 위해 최소 2명이상부터 시작할 수 있도록 하게 함.
            Debug.Log("must be more than 2 player required to play game");
        }

        JObject request = new JObject();
        request["type"] = "start_game";
        NetworkManager.Instance.SendMessageToServer(request.ToString());
    }

    public void OnLeaveRoomClicked()
    {
        JObject request = new JObject();
        request["type"] = "leave_room";
        NetworkManager.Instance.SendMessageToServer(request.ToString());
    }

    #endregion
}
