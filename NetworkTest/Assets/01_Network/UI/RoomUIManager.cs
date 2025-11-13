using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using TMPro;
using Steamworks;
using UnityEngine.SceneManagement; // Add this for scene management

public class RoomUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private GameObject launchButton;

    private int currentSelectedPlanetId = -1;

    void Start()
    {
        if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            ServerRoomManager.Instance.AddHostPlayer(NetworkManager.Instance.selfSteamId, CustomSteamManager.Instance.PlayerName);
        }
        else if (NetworkManager.Instance.Mode == NetworkMode.Client)
        {
            SendNickname();
        }
        UpdateLaunchButtonVisibility();
    }

    private void OnEnable()
    {
        NetworkManager.OnJsonMessageReceived += HandleServerJsonMessage;
        NetworkManager.OnDisconnected += HandleDisconnection;
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnJsonMessageReceived -= HandleServerJsonMessage;
            NetworkManager.OnDisconnected -= HandleDisconnection;
        }
    }

    private void HandleDisconnection()
    {
        Debug.Log($"[RoomUIManager] Disconnected. Returning to {GameManager.Instance.GameSettings.mainmenuScene}.");
        SceneManager.LoadScene(GameManager.Instance.GameSettings.mainmenuScene);
    }

    private void SendNickname()
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Client) return;

        string nickname = CustomSteamManager.Instance.PlayerName;
        JObject msg = new JObject
        {
            { "type", "set_nickname" },
            { "nickname", nickname }
        };

        CSteamID hostId = SteamMatchmaking.GetLobbyOwner(NetworkManager.Instance.CurrentLobbyID);
        NetworkManager.Instance.SendJsonMessage(hostId, msg);
    }

    private void HandleServerJsonMessage(CSteamID sender, string jsonMsg)
    {
        try
        {
            JObject response = JObject.Parse(jsonMsg);
            string type = response["type"]?.ToString();

            switch (type)
            {
                case "update_room_info":
                    HandleRoomUpdate(response);
                    break;

                case "load_scene":
                    string sceneToLoad = response["scene_name"]?.ToString();
                    if (!string.IsNullOrEmpty(sceneToLoad))
                    {
                        Debug.Log($"[RoomUIManager] Received command to load scene: {sceneToLoad}");
                        SceneManager.LoadScene(sceneToLoad);
                    }
                    break;

                default:
                    // Optional: Log unknown message types
                    // Debug.LogWarning($"[RoomUIManager] Received unknown message type: {type}");
                    break;
            }
        }
        catch (JsonReaderException e)
        {
            Debug.LogError($"[RoomUIManager] Failed to parse server message: {e.Message}\nMessage: {jsonMsg}");
        }
    }

    private void HandleRoomUpdate(JObject data)
    {
        if (playerListContent == null || roomNameText == null || playerListItemPrefab == null)
        {
            return;
        }

        // Update selected planet
        currentSelectedPlanetId = data["selected_planet_id"]?.ToObject<int>() ?? -1;
        Debug.Log($"[RoomUIManager] Room updated. Selected planet ID is now: {currentSelectedPlanetId}");

        JArray players = data["players"] as JArray;

        if (NetworkPlayerManager.Instance != null && players != null)
        {
            NetworkPlayerManager.Instance.UpdatePlayerList(players);
        }

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        string roomName = data["room_name"]?.ToString();
        roomNameText.text = roomName;

        string hostId = data["host_id"]?.ToString();
        if (players != null)
        {
            foreach (JObject playerInfoJson in players)
            {
                PlayerInfo playerInfo = playerInfoJson.ToObject<PlayerInfo>();
                GameObject itemGO = Instantiate(playerListItemPrefab, playerListContent);
                PlayerListItem item = itemGO.GetComponent<PlayerListItem>();
                if (item != null)
                {
                    item.Setup(playerInfo, playerInfo.player_id == hostId);
                }
            }
        }
        UpdateLaunchButtonVisibility();
    }

    private void UpdateLaunchButtonVisibility()
    {
        if (launchButton != null)
        {
            bool shouldBeActive = NetworkManager.Instance.Mode == NetworkMode.Host && currentSelectedPlanetId != -1;
            launchButton.SetActive(shouldBeActive);
        }
    }

    // This method is called by the new "Launch" button
    public void OnLaunchGameClicked()
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;
        if (currentSelectedPlanetId == -1)
        {
            Debug.LogWarning("[RoomUIManager] Cannot launch, no planet selected.");
            return;
        }

        Debug.Log($"[RoomUIManager] Host clicked launch for planet {currentSelectedPlanetId}.");
        ServerRoomManager.Instance.LaunchToPlanet(currentSelectedPlanetId);
    }

    // This method would be called by a UI button's OnClick event in the RoomScene.
    public void OnPlanetSelect(int planetId)
    {
        Debug.Log($"[RoomUIManager] UI button clicked. Proposing planet {planetId}.");
        ProposePlanet(planetId);
    }

    private void ProposePlanet(int planetId)
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[RoomUIManager] NetworkManager not found!");
            return;
        }

        if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            if (ServerRoomManager.Instance != null)
            {
                ServerRoomManager.Instance.SelectPlanet(planetId);
            }
            else
            {
                Debug.LogError("[RoomUIManager] ServerRoomManager not found for host!");
            }
        }
        else
        {
            JObject message = new JObject
            {
                { "type", "propose_planet" },
                { "planet_id", planetId }
            };

            CSteamID hostId = NetworkManager.Instance.LobbyHostID;
            if (hostId.IsValid())
            {
                NetworkManager.Instance.SendJsonMessage(hostId, message);
                Debug.Log($"[RoomUIManager] Sent 'propose_planet' (ID: {planetId}) message to host ({hostId}).");
            }
            else
            {
                Debug.LogError("[RoomUIManager] Could not send proposal, invalid host ID.");
            }
        }
    }

    public void OnInviteFriends()
    {
        // Use the static Instance to call the method
        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.Instance.OnInviteFriendsButtonClicked();
            Debug.Log("Steam Invite Friends Overlay Opened.");
        }
        else
        {
            Debug.LogError("ServerRoomManager.Instance is not found!");
        }
    }
}