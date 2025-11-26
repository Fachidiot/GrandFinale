using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class RoomUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private TextMeshProUGUI roomNameText;

    private void OnEnable()
    {
        if (NetworkManager.Instance.Mode == NetworkMode.SinglePlayer)
        {
            SinglePlayerUI();
        }
        else
        {
            // Subscribe to the central ServerRoomManager for UI updates
            ServerRoomManager.OnRoomDataUpdated += UpdateUI;
            NetworkManager.OnDisconnected += HandleDisconnection;

            // Update UI with current data on enable
            UpdateUI();
        }
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
    }

    private void SinglePlayerUI()
    {
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }
        roomNameText.gameObject.SetActive(false);
    }
}
