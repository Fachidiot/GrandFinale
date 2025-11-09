using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class RoomUIManager : MonoBehaviour
{
    public static RoomUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private TextMeshProUGUI roomNameText;

    private bool nicknameSent = false;
    private bool isDuplicate = false;

    // --- New fields for delayed UI update ---
    private JObject pendingUpdateData = null;
    private bool isSceneReady = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[RoomUIManager] Another instance ({Instance.gameObject.GetInstanceID()}) already exists. Destroying this one ({gameObject.GetInstanceID()}).");
            isDuplicate = true;
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (isDuplicate) return;

        if (nicknameSent)
        {
            Debug.LogWarning("[RoomUIManager] Start logic has already run on a previous instance. Skipping for this instance.");
            isSceneReady = true; // Still mark as ready
            return;
        }

        if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            Debug.Log("[RoomUIManager] Host has entered the room. Registering host player.");
            if (ServerRoomManager.Instance != null)
            {
                ServerRoomManager.Instance.AddHostPlayer(NetworkManager.Instance.HostPlayerInfo);
            }
            else
            {
                Debug.LogError("[RoomUIManager] ServerRoomManager.Instance is null! Cannot register host.");
            }
            nicknameSent = true;
        }
        else if (NetworkManager.Instance.Mode == NetworkMode.Client)
        {
            Debug.Log("[RoomUIManager] Client has entered the room. Sending nickname.");
            SendNickname();
            nicknameSent = true;
        }

        isSceneReady = true; // Signal that the Start method has completed and the scene is ready for UI updates.
    }

    void Update()
    {
        // If the scene is ready and there is pending data to process, process it now.
        if (isSceneReady && pendingUpdateData != null)
        {
            HandleRoomUpdate(pendingUpdateData);
            pendingUpdateData = null; // Clear the data after processing
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

    private void SendNickname()
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Client) return;

        string nickname = CustomSteamManager.Instance.PlayerName;

        JObject msg = new JObject
        {
            { "type", "set_nickname" },
            { "nickname", nickname }
        };

        string jsonMessage = msg.ToString(Formatting.None);
        NetworkManager.Instance.SendTCPMessage(jsonMessage);
        Debug.Log($"Sent nickname message: {jsonMessage}");
    }

    private void HandleServerMessage(string jsonMsg)
    {
        try
        {
            JObject response = JObject.Parse(jsonMsg);
            string type = response["type"]?.ToString();

            if (type == "update_room_info")
            {
                // Instead of processing immediately, store the data.
                pendingUpdateData = response;
            }
        }
        catch (JsonReaderException e)
        {
            Debug.LogError($"[RoomUIManager] Failed to parse server message: {e.Message}\nMessage: {jsonMsg}");
        }
    }

    private void HandleRoomUpdate(JObject data)
    {
        JArray players = data["players"] as JArray;

        if (NetworkPlayerManager.Instance != null && players != null)
        {
            NetworkPlayerManager.Instance.UpdatePlayerList(players);
        }

        if (playerListContent != null)
        {
            foreach (Transform child in playerListContent)
            {
                Destroy(child.gameObject);
            }
        }

        string roomName = data["room_name"]?.ToString();

        if (roomNameText != null)
            roomNameText.text = roomName;

        string hostId = data["host_id"]?.ToString();

        if (players != null && playerListContent != null)
        {
            foreach (JObject playerInfoJson in players)
            {
                PlayerInfo playerInfo = playerInfoJson.ToObject<PlayerInfo>();
                if (playerListItemPrefab == null)
                {
                    continue;
                }
                GameObject itemGO = Instantiate(playerListItemPrefab, playerListContent);
                if (itemGO == null)
                {
                    Debug.LogError("[RoomUIManager] Instantiate returned NULL!");
                }
                PlayerListItem item = itemGO.GetComponent<PlayerListItem>();
                if (item != null)
                {
                    item.Setup(playerInfo, playerInfo.player_id == hostId);
                }
            }
        }
    }
}
