using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Steamworks;

public class MainMenuUIManager : MonoBehaviour
{
        [Header("Connection UI")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private TMP_InputField inviteCodeInputField;
        [SerializeField] private Button joinLobbyButton;
    
        private Dictionary<string, Action<string>> messageHandlers;
        private string ip = "127.0.0.1";
    
        private void Awake()
        {
            InitializeMessageHandlers();
        }
    
        private void Start()
        {
            // 이 로직은 클라이언트로 자동 연결하는 데 사용됩니다.
            // 필요에 따라 버튼 클릭이나 다른 이벤트로 이동할 수 있습니다.
            /*
            if (CustomSteamManager.Instance == null)
            {
                GameObject steamManagerGO = new GameObject("CustomSteamManager");
                steamManagerGO.AddComponent<CustomSteamManager>();
            }
    
            if (CustomSteamManager.Instance.IsSteamInitialized)
            {
                infoText.text = "Fetching Steam nickname...";
                string steamName = CustomSteamManager.Instance.PlayerName;
                infoText.text = $"Welcome, {steamName}! Connecting to server...";
                NetworkManager.Instance.ConnectAsClient(ip);
            }
            else
            {
                infoText.text = "Steam not detected. Please run Steam and restart the game.";
                errorText.text = "Steam initialization failed.";
                errorText.gameObject.SetActive(true);
            }
            */
        }
    
        public void OnMultiplayerButtonClicked()
        {
            Debug.Log("Multiplayer button clicked. Starting host...");
            NetworkManager.Instance.StartHost(8080);
        }
    
        public void OnSingleplayerButtonClicked()
        {
            Debug.Log("Singleplayer button clicked. Starting offline mode...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartOffline();
            }
            else
            {
                Debug.LogError("GameManager.Instance is null in MainMenuUIManager.OnSingleplayerButtonClicked()");
            }
        }
    
        public void OnSettingsButtonClicked()
        {
            Debug.Log("Settings button clicked. Opening settings...");
            // 여기에 설정 UI를 여는 코드를 추가합니다.
        }
    
        public void OnJoinLobbyButtonClicked()
        {
            if (string.IsNullOrEmpty(inviteCodeInputField.text))
            {
                errorText.text = "Please enter a Lobby ID.";
                errorText.gameObject.SetActive(true);
                return;
            }
    
            if (ulong.TryParse(inviteCodeInputField.text, out ulong lobbyIDRaw))
            {
                CSteamID lobbyID = new CSteamID(lobbyIDRaw);
                NetworkManager.Instance.JoinSteamLobby(lobbyID);
                errorText.gameObject.SetActive(false);
            }
            else
            {
                errorText.text = "Invalid Lobby ID format.";
                errorText.gameObject.SetActive(true);
            }
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
                { "update_room_info", TriggerRoomSceneLoad }
            };
        }
    
        private void TriggerRoomSceneLoad(string json)
        {
            // 씬 전환 직전에 방 정보를 NetworkManager의 변수에 저장합니다.
            NetworkManager.Instance.LastRoomUpdateInfo = json;
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
            // 클라이언트로 연결되었을 때 닉네임을 설정하는 로직
            if (NetworkManager.Instance.Mode == NetworkMode.Client)
            {
                infoText.text = "Connection successful. Setting nickname...";
                string nickname = CustomSteamManager.Instance.PlayerName;
    
                JObject request = new JObject
                {
                    ["type"] = "set_nickname",
                    ["nickname"] = nickname
                };
                NetworkManager.Instance.SendTCPMessage(request.ToString());
    
                JObject findRoomsRequest = new JObject { ["type"] = "find_rooms" };
                NetworkManager.Instance.SendTCPMessage(findRoomsRequest.ToString());
            }
            else if (NetworkManager.Instance.Mode == NetworkMode.Host)
            {
                // 호스트로 시작했을 때의 로직 (예: 로비 씬으로 바로 이동)
                SceneManager.LoadScene("RoomScene");
            }
        }
    
        private void HandleConnectionFailed(string errorMessage)
        {
            infoText.text = "Connection failed. Please check the server and try again.";
            errorText.text = $"Connection Failed: {errorMessage}";
            errorText.gameObject.SetActive(true);
        }
    
        private void HandleDisconnection()
        {
            infoText.text = "Disconnected. You can try connecting again.";
            connectionPanel.SetActive(true);
            errorText.text = "Disconnected from server.";
            errorText.gameObject.SetActive(true);
        }
    
        #endregion
    }
    