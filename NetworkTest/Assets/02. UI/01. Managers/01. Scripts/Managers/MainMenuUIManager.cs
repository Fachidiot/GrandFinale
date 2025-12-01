using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Cinemachine;
using System.Linq;
using Newtonsoft.Json.Linq;
using Michsky.MUIP;

public class MainMenuUIManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera menuCamera;
    [SerializeField] private CinemachineVirtualCamera roomCamera;

    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject roomPanel;

    [Header("Lobby 3D View")]
    [SerializeField] private GameObject localCustomView;
    [SerializeField] private GameObject clientCustomView;
    [SerializeField] private List<Transform> slotList;

    [Header("Lobby UI List")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private TextMeshProUGUI roomNameText;

    [Header("Room Buttons")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject readyButton;

    [SerializeField] private CustomizeManager customizeManager;

    private OptionController optionController;

    private void Start()
    {
        // Start with buttons hidden
        if (startButton != null) startButton.SetActive(false);
        if (readyButton != null) readyButton.SetActive(false);

        menuPanel.SetActive(true);
        roomPanel.SetActive(false);
    }

    public void OnReadyButtonClicked()
    {
        if (NetworkManager.Instance == null) return;

        // For now, we'll just toggle ready state. A more robust implementation
        // would track the state locally.
        JObject readyMsg = new JObject
        {
            { "type", "player_ready" },
            { "is_ready", true } // Simple implementation: clicking ready always sets to true.
        };

        // In multiplayer, send to host. Single player is handled by default.
        if (NetworkManager.Instance.Mode == NetworkMode.Client)
        {
            NetworkManager.Instance.SendJsonMessage(NetworkManager.Instance.LobbyHostID, readyMsg);
        }
        else if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            // Host handles their own ready message directly through the server room manager
            ServerRoomManager.Instance.HandlePlayerReady(NetworkManager.Instance.selfSteamId, readyMsg);
        }

        if (readyButton != null)
        {
            readyButton.SetActive(false); // Hide ready button after clicking
        }
    }

    public void OnStartGameButtonClicked()
    {
        if (ServerRoomManager.Instance != null)
        {
            // Use the planet ID already stored in the manager
            ServerRoomManager.Instance.LaunchToPlanet(GameManager.Instance.GameSettings.spaceroomScene);
        }
    }

    public void OnRoomButtonClicked()
    {
        menuCamera.Priority = 0;
        roomCamera.Priority = 1;
        menuPanel.SetActive(false);
        roomPanel.SetActive(true);

        if (NetworkManager.Instance != null)
        {
            if (NetworkManager.Instance.Mode == NetworkMode.Client)
            {
                if (readyButton != null) readyButton.SetActive(true);
                if (startButton != null) startButton.SetActive(false);
            }
            else // Host or SinglePlayer
            {
                if (readyButton != null) readyButton.SetActive(false); // Host doesn't need a ready button
                if (startButton != null) startButton.SetActive(true);
            }
        }
    }

    public void OnMenuButtonClicked()
    {
        menuCamera.Priority = 1;
        roomCamera.Priority = 0;
        menuPanel.SetActive(true);
        roomPanel.SetActive(false);

        // if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        // {
        //     NetworkManager.Instance.Disconnect(); // This will trigger HandleDisconnection
        // }
        // else // Handle leaving single player room
        // {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.ClearAllNetworkEntities();
        }
        if (ServerRoomManager.Instance != null)
        {
            // Unsubscribe first to prevent UI updates while we are cleaning up.
            ServerRoomManager.OnRoomDataUpdated -= UpdatePlayerSlots;
            ServerRoomManager.Instance.ClearRoom();
        }
        // }
    }

    public void OnMultiplayerButtonClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetPause(false);
        // The new flow is to create a Steam lobby, which then handles connection and scene loading.
        NetworkManager.Instance.CreateSteamLobby();
    }

    public void OnSingleplayerButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPause(false);

            // Set mode and initialize managers for single player
            NetworkManager.Instance.SetMode(NetworkMode.SinglePlayer);
            if (ServerRoomManager.Instance != null)
            {
                ServerRoomManager.Instance.Initialize(NetworkMode.SinglePlayer);
                ServerRoomManager.OnRoomDataUpdated += UpdatePlayerSlots; // Subscribe here for single player
            }

            // Switch to room view
            OnRoomButtonClicked();

            if (startButton != null) startButton.SetActive(true);

            // Add the single player. This will trigger OnRoomDataUpdated, which in turn calls UpdatePlayerSlots.
            if (ServerRoomManager.Instance != null)
                ServerRoomManager.Instance.AddSinglePlayer();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null in MainMenuUIManager.OnSingleplayerButtonClicked()");
        }
    }

    public void OnSettingsButtonClicked()
    {
        if (!optionController)
            optionController = FindObjectOfType<OptionController>();
        optionController.Toggle();
    }

    public void OnExitButtonClicked()
    {
        Application.Quit();
    }

    private void OnEnable()
    {
        NetworkManager.OnConnected += HandleConnection;
        NetworkManager.OnConnectionFailed += HandleConnectionFailed;
        NetworkManager.OnDisconnected += HandleDisconnection;
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnConnected -= HandleConnection;
            NetworkManager.OnConnectionFailed -= HandleConnectionFailed;
            NetworkManager.OnDisconnected -= HandleDisconnection;
        }

        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.OnRoomDataUpdated -= UpdatePlayerSlots;
        }
    }

    private void UpdatePlayerSlots()
    {
        if (ServerRoomManager.Instance == null)
            return;

        var playerList = ServerRoomManager.Instance.PlayerList;
        var hostId = ServerRoomManager.Instance.HostId;
        
        // --- 1. Update Text-based Player List ---
        if (playerListContent != null && playerListItemPrefab != null)
        {
            // Clear old list
            foreach (Transform child in playerListContent)
            {
                Destroy(child.gameObject);
            }

            // Update Room Name
            if (roomNameText != null)
            {
                roomNameText.text = ServerRoomManager.Instance.RoomName;
            }

            // Populate new player list
            if(playerList != null)
            {
                foreach (var playerInfo in playerList)
                {
                    GameObject itemGO = Instantiate(playerListItemPrefab, playerListContent);
                    PlayerListItem item = itemGO.GetComponent<PlayerListItem>();
                    if (item != null)
                    {
                        item.Setup(playerInfo, playerInfo.player_id == hostId);
                    }
                }
            }
        }
        
        // --- 2. Update 3D Player Models in Slots ---
        if (slotList != null)
        {
            // Clear all existing 3D custom views
            foreach (Transform slot in slotList)
            {
                foreach (Transform child in slot)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        if (playerList == null)
            return;

        // --- 3. Update Ready/Start Button States ---
        bool allPlayersReady = playerList.Count > 0 && playerList.All(p => p.IsReady);

        if (NetworkManager.Instance != null)
        {
            if (NetworkManager.Instance.Mode == NetworkMode.Host)
            {
                if (startButton != null) startButton.SetActive(allPlayersReady);
            }
            else if (NetworkManager.Instance.Mode == NetworkMode.SinglePlayer)
            {
                if (startButton != null) startButton.SetActive(true);
            }
        }

        // --- 4. Re-populate 3D Player Models ---
        // Only run this logic in the MainMenuScene
        if (SceneManager.GetActiveScene().name == GameManager.Instance.GameSettings.mainmenuScene)
        {
            for (int i = 0; i < playerList.Count; i++)
            {
                if (i >= slotList.Count)
                    break; // Do not exceed available slots

                var player = playerList[i];
                GameObject prefabToSpawn = null;

                bool isLocalPlayer = (NetworkManager.Instance != null && player.steam_id == NetworkManager.Instance.selfSteamId.m_SteamID.ToString()) || (NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.SinglePlayer && player.steam_id == "0");

                prefabToSpawn = isLocalPlayer ? localCustomView : clientCustomView;

                if (prefabToSpawn != null)
                {
                    GameObject view = Instantiate(prefabToSpawn, slotList[i]);

                    if (isLocalPlayer)
                    {
                        customizeManager.FModel = view.GetComponentsInChildren<ModelCustom>(true)[0];
                        customizeManager.MModel = view.GetComponentsInChildren<ModelCustom>(true)[1];
                        customizeManager.LocalPlayerSet();
                    }
                    else
                    {
                        ModelCustom[] models = view.GetComponentsInChildren<ModelCustom>(true);
                        ModelCustom fModel = models[0];
                        ModelCustom mModel = models[1];

                        bool isMale = player.is_Male;

                        mModel.transform.parent.gameObject.SetActive(isMale);
                        fModel.transform.parent.gameObject.SetActive(!isMale);

                        ModelInfo modelInfo = new ModelInfo(player.headIndex, player.bodyIndex, player.acc1Index, player.acc2Index);

                        if (isMale)
                        {
                            mModel.ApplyModelInfo(modelInfo);
                            mModel.transform.parent.GetComponent<Animator>().SetBool("Sit", true);
                        }
                        else
                        {
                            fModel.ApplyModelInfo(modelInfo);
                            fModel.transform.parent.GetComponent<Animator>().SetBool("Sit", true);
                        }
                    }

                    var readyIndicator = view.transform.Find("ReadyIndicator");
                    if (readyIndicator != null)
                    {
                        readyIndicator.gameObject.SetActive(player.IsReady);
                    }
                }
            }
        }
    }
    #region Connection and UI Panel Management

    private void HandleConnection()
    {
        Debug.Log("[MainMenuUIManager] NetworkManager connected.");

        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.OnRoomDataUpdated += UpdatePlayerSlots;
        }
        OnRoomButtonClicked();
    }

    private void HandleConnectionFailed(string errorMessage)
    {
        Debug.Log("Connection failed. Please check the server and try again.");
        Debug.Log($"Connection Failed: {errorMessage}");
    }

    private void HandleDisconnection()
    {
        Debug.Log("Disconnected. You can try connecting again.");
        Debug.Log("Disconnected from server.");
        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.OnRoomDataUpdated -= UpdatePlayerSlots;
        }
    }
    #endregion
}