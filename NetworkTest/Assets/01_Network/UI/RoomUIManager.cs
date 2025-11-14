using UnityEngine;
using TMPro;
using Steamworks;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

public class RoomUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private GameObject launchButton;

    private void OnEnable()
    {
        // Subscribe to the central ServerRoomManager for UI updates
        ServerRoomManager.OnRoomDataUpdated += UpdateUI;
        NetworkManager.OnDisconnected += HandleDisconnection;

        // Update UI with current data on enable
        UpdateUI();
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.OnRoomDataUpdated -= UpdateUI;
        }
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnDisconnected -= HandleDisconnection;
        }
    }

    private void HandleDisconnection()
    {
        Debug.Log($"[RoomUIManager] Disconnected. Returning to {GameManager.Instance.GameSettings.mainmenuScene}.");
        // Ensure we are not destroying the manager if it's persistent
        if (gameObject.scene.name != "DontDestroyOnLoad")
        {
            SceneManager.LoadScene(GameManager.Instance.GameSettings.mainmenuScene);
        }
    }

    // Central UI update function
    private void UpdateUI()
    {
        if (playerListContent == null || roomNameText == null || playerListItemPrefab == null || ServerRoomManager.Instance == null)
        {
            return;
        }

        // Clear old list
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        // Update Room Name
        roomNameText.text = ServerRoomManager.Instance.RoomName;

        // Populate new player list from ServerRoomManager
        var playerList = ServerRoomManager.Instance.PlayerList;
        var hostId = ServerRoomManager.Instance.HostId;

        foreach (var playerInfo in playerList)
        {
            GameObject itemGO = Instantiate(playerListItemPrefab, playerListContent);
            PlayerListItem item = itemGO.GetComponent<PlayerListItem>();
            if (item != null)
            {
                item.Setup(playerInfo, playerInfo.player_id == hostId);
            }
        }

        // Update Launch Button
        UpdateLaunchButtonVisibility();
    }

    private void UpdateLaunchButtonVisibility()
    {
        if (launchButton != null && ServerRoomManager.Instance != null)
        {
            bool shouldBeActive = NetworkManager.Instance.Mode == NetworkMode.Host && ServerRoomManager.Instance.SelectedPlanetId != -1;
            launchButton.SetActive(shouldBeActive);
        }
    }

    // This method is called by the "Launch" button
    public void OnLaunchGameClicked()
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;
        if (ServerRoomManager.Instance.SelectedPlanetId == -1)
        {
            Debug.LogWarning("[RoomUIManager] Cannot launch, no planet selected.");
            return;
        }

        Debug.Log($"[RoomUIManager] Host clicked launch for planet {ServerRoomManager.Instance.SelectedPlanetId}.");
        ServerRoomManager.Instance.LaunchToPlanet(ServerRoomManager.Instance.SelectedPlanetId);
    }

    // This method is called by a UI button's OnClick event
    public void OnPlanetSelect(int planetId)
    {
        Debug.Log($"[RoomUIManager] UI button clicked. Proposing planet {planetId}.");
        ProposePlanet(planetId);
    }

    private void ProposePlanet(int planetId)
    {
        if (NetworkManager.Instance == null || ServerRoomManager.Instance == null) return;

        if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            ServerRoomManager.Instance.SelectPlanet(planetId);
        }
        else // Client
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
            }
        }
    }

    public void OnInviteFriends()
    {
        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.Instance.OnInviteFriendsButtonClicked();
        }
    }
}
