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
using Steamworks;

public class RoomUIManager : MonoBehaviour
{
    public static RoomUIManager Instance { get; private set; }

    [Header("In-Room UI")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TextMeshProUGUI roomIDText;
    [SerializeField] private Button copyRoomIDButton;
    [SerializeField] private TextMeshProUGUI chatText;
    [SerializeField] private TMP_InputField chatMessageInput;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private AssetReference playerListItemPrefab;

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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterRoom();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null in RoomUIManager.Start()");
        }

        if (NetworkManager.Instance != null)
        {
            if (NetworkManager.Instance.Mode == NetworkMode.Host)
            {
                NetworkPlayerManager.Instance?.SpawnLocalHostPlayer();
                if (roomIDText != null) roomIDText.text = $"Lobby ID: {NetworkManager.Instance.CurrentLobbyID.ToString()}";
                if (copyRoomIDButton != null) copyRoomIDButton.gameObject.SetActive(true);
            }
            else
            {
                if (roomIDText != null) roomIDText.gameObject.SetActive(false);
                if (copyRoomIDButton != null) copyRoomIDButton.gameObject.SetActive(false);

                if (!string.IsNullOrEmpty(NetworkManager.Instance.LastRoomUpdateInfo))
                {
                    HandleUpdateRoomInfo(NetworkManager.Instance.LastRoomUpdateInfo);
                    NetworkManager.Instance.LastRoomUpdateInfo = null; // Consume the data
                }
            }
        }
    }

    private void OnEnable()
    {
        NetworkManager.OnMessageReceived += HandleServerMessage;
        NetworkManager.OnDisconnected += HandleDisconnected;
        NetworkManager.OnLobbyIDUpdated += HandleLobbyIDUpdated;
    }

    private void OnDisable()
    {
        NetworkManager.OnMessageReceived -= HandleServerMessage;
        NetworkManager.OnDisconnected -= HandleDisconnected;
        NetworkManager.OnLobbyIDUpdated -= HandleLobbyIDUpdated;
    }

    private void HandleLobbyIDUpdated(CSteamID lobbyID)
    {
        if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            if (roomIDText != null) roomIDText.text = $"Lobby ID: {lobbyID.ToString()}";
            if (copyRoomIDButton != null) copyRoomIDButton.gameObject.SetActive(true);
        }
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

    private void HandleDisconnected()
    {
        // 임시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("ConnectionScene");
    }

    private void HandleLeaveRoomSuccess(string json)
    {
        Debug.Log("Handle Leave Room");
        SceneManager.LoadScene("ConnectionScene");
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

        if (roomIDText != null) roomIDText.text = payload.room_name;

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
                AsyncOperationHandle<GameObject> handle = playerListItemPrefab.InstantiateAsync(playerListContainer);
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

    public void OnCopyLobbyIDButtonClicked()
    {
        if (NetworkManager.Instance.Mode == NetworkMode.Host && NetworkManager.Instance.CurrentLobbyID.IsValid())
        {
            GUIUtility.systemCopyBuffer = NetworkManager.Instance.CurrentLobbyID.ToString();
            Debug.Log($"Lobby ID {NetworkManager.Instance.CurrentLobbyID} copied to clipboard.");
        }
    }

    #endregion
}
