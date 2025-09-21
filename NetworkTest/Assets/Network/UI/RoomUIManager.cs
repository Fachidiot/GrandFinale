using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class RoomUIManager : MonoBehaviour
{
    public static RoomUIManager Instance { get; private set; }

    [Header("In-Room UI")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI chatText;
    [SerializeField] private TMP_InputField chatMessageInput;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Transform playerListContainer;

    private Dictionary<string, Action<string>> messageHandlers;
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
        if (NetworkManager.Instance != null && !string.IsNullOrEmpty(NetworkManager.Instance.LastRoomUpdateInfo))
        {
            HandleUpdateRoomInfo(NetworkManager.Instance.LastRoomUpdateInfo);
            NetworkManager.Instance.LastRoomUpdateInfo = null; // Consume the data
        }
    }

    private void OnEnable()
    {
        NetworkManager.OnMessageReceived += HandleServerMessage;
    }

    private void OnDisable()
    {
        NetworkManager.OnMessageReceived -= HandleServerMessage;
    }

    private void InitializeMessageHandlers()
    {
        messageHandlers = new Dictionary<string, Action<string>>
        {
            { "update_room_info", HandleUpdateRoomInfo },
            { "player_joined", HandlePlayerJoined },
            { "player_left", HandlePlayerLeft },
            { "chat_broadcast", HandleChatBroadcast },
            { "game_start", HandleGameStart },
            { "leave_room_success", HandleLeaveRoomSuccess }
        };
    }

    private void HandleLeaveRoomSuccess(string json)
    {
        Debug.Log("Handle Leave Room");
        SceneManager.LoadScene("NetworkScene");
    }

    private void HandleServerMessage(string jsonMsg)
    {
        JObject response = JObject.Parse(jsonMsg);
        string type = response["type"]?.ToString();

        if (messageHandlers.TryGetValue(type, out var handler))
        {
            handler(jsonMsg);
        }
    }

    #region Message Handling

    private async void HandleUpdateRoomInfo(string json)
    {
        var payload = JsonConvert.DeserializeObject<UpdateRoomInfoPayload>(json);
        player_count = payload.players.Count;

        if (roomNameText != null) roomNameText.text = payload.room_name;

        bool amIHost = NetworkManager.Instance.PlayerId == payload.host_id;
        if (startGameButton != null) startGameButton.gameObject.SetActive(amIHost);
        if (readyButton != null) readyButton.gameObject.SetActive(!amIHost);

        if (playerListContainer != null)
        {
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
    }

    private void HandlePlayerJoined(string json)
    {
        Debug.Log("Handle Player Join");
        var payload = JsonConvert.DeserializeObject<PlayerJoinedPayload>(json);
        if (chatText != null) chatText.text += "--- " + payload.player_id + " has joined the room. ---\n";
    }

    private void HandlePlayerLeft(string json)
    {
        Debug.Log("Handle Player Left");
        var payload = JsonConvert.DeserializeObject<PlayerLeftPayload>(json);
        if (chatText != null) chatText.text += "--- " + payload.player_id + " has left the room. ---\n";
    }

    private void HandleChatBroadcast(string json)
    {
        Debug.Log("Handle Chat Broadcast");
        var payload = JsonConvert.DeserializeObject<ChatBroadcastPayload>(json);
        if (chatText != null) chatText.text += "[" + payload.sender_id + "]: " + payload.message + "\n";
    }

    private void HandleGameStart(string json)
    {
        Debug.Log("Handle Game Start");
        if (chatText != null) chatText.text += "--- The game is starting! ---\n";
        // Add actual game start logic here, like loading a new scene
    }

    #endregion

    #region UI Button Clicks

    public void OnSendChatMessageClicked()
    {
        string message = chatMessageInput.text;
        if (string.IsNullOrEmpty(message)) return;

        JObject request = new JObject
        {
            ["type"] = "chat_message",
            ["message"] = message
        };
        NetworkManager.Instance.SendTCPMessage(request.ToString());
        chatMessageInput.text = "";
    }

    public void OnReadyButtonClicked()
    {
        JObject request = new JObject { ["type"] = "toggle_ready" };
        NetworkManager.Instance.SendTCPMessage(request.ToString());
    }

    public void OnStartGameButtonClicked()
    {
        if (player_count <= 1)
        {
            Debug.Log("must be more than 2 player required to play game");
            return;
        }

        JObject request = new JObject { ["type"] = "start_game" };
        NetworkManager.Instance.SendTCPMessage(request.ToString());
    }

    public void OnLeaveRoomClicked()
    {
        JObject request = new JObject { ["type"] = "leave_room" };
        NetworkManager.Instance.SendTCPMessage(request.ToString());
    }

    #endregion
}
