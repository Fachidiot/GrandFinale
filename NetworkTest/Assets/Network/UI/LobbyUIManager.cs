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

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance { get; private set; }

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

    private Dictionary<string, Action<string>> messageHandlers;
    // private string ip = "grandfinale.o-r.kr";   // localhost: 127.0.0.1
    private string ip = "127.0.0.1";

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
        GameManager.Instance.EnterLobby();
    }

    private void Start()
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (NetworkManager.Instance.isConnected)
            HandleConnection();
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
            { "update_room_info", TriggerRoomSceneLoad },
            { "find_rooms_response", HandleFindRoomsResponse }
        };
    }

    private void TriggerRoomSceneLoad(string json)
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.LastRoomUpdateInfo = json;
        }
        SceneManager.LoadScene("RoomScene");
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

    #region Connection and UI Panel Management

    private void HandleConnection()
    {
        connectionPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        errorText.gameObject.SetActive(false);

        // 플레이어 닉네임의 변경사항이 없다면 return
        string nickname = string.IsNullOrEmpty(nickNameInput.text) ? $"Player{UnityEngine.Random.Range(100, 1000)}" : nickNameInput.text;
        if (NetworkManager.Instance.PlayerNickname == nickname)
            return;

        JObject request = new JObject
        {
            ["type"] = "set_nickname",
            ["nickname"] = nickname
        };
        NetworkManager.Instance.SendTCPMessage(request.ToString());
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

    private async void HandleFindRoomsResponse(string json)
    {
        Debug.Log("Handle Find Rooms");
        var payload = JsonConvert.DeserializeObject<FindRoomsResponse>(json);

        if (roomListContainer != null)
        {
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
    }

    #endregion

    #region UI Button Clicks

    public void SetNickName(string nickname)
    {
        nickNameInput.text = nickname;
    }

    public void OnSetNicknameClicked()
    {
        if (NetworkManager.Instance.PlayerNickname == nickNameInput.text)
            return;

        string nickname = string.IsNullOrEmpty(nickNameInput.text) ? $"Player{UnityEngine.Random.Range(100, 1000)}" : nickNameInput.text;
        JObject request = new JObject
        {
            ["type"] = "set_nickname",
            ["nickname"] = nickname
        };
        NetworkManager.Instance.SendTCPMessage(request.ToString());
    }

    public void OnCreateRoomPanelActive()
    {
        roomNameInput.text = NetworkManager.Instance.PlayerNickname + "'s Room";
    }

    public void OnCreateRoomClicked()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
            return;

        JObject request = new JObject
        {
            ["type"] = "create_room",
            ["room_name"] = roomName
        };
        NetworkManager.Instance.SendTCPMessage(request.ToString());
    }

    public void OnFindRoomsClicked()
    {
        JObject request = new JObject { ["type"] = "find_rooms" };
        NetworkManager.Instance.SendTCPMessage(request.ToString());
    }

    public void JoinRoomById(int roomId)
    {
        JObject request = new JObject
        {
            ["type"] = "join_room",
            ["room_id"] = roomId
        };
        NetworkManager.Instance.SendTCPMessage(request.ToString());
    }

    #endregion
}