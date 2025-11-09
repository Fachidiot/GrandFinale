using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using TMPro;
using Steamworks;

public class RoomUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private TextMeshProUGUI roomNameText;

    void Start()
    {
        if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            Debug.Log("RoomUIManager: Start() called. Mode: Host");
            ServerRoomManager.Instance.AddHostPlayer(NetworkManager.Instance.selfSteamId, CustomSteamManager.Instance.PlayerName);
        }
        else if (NetworkManager.Instance.Mode == NetworkMode.Client)
        {
            SendNickname();
        }
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
        Debug.Log("[RoomUIManager] Disconnected. Returning to ConnectionScene.");
        UnityEngine.SceneManagement.SceneManager.LoadScene("ConnectionScene");
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
        Debug.Log($"Sent nickname message to host: {msg.ToString(Formatting.None)}");
    }

    private void HandleServerJsonMessage(CSteamID sender, string jsonMsg)
    {
        try
        {
            JObject response = JObject.Parse(jsonMsg);
            string type = response["type"]?.ToString();

            if (type == "update_room_info")
            {
                Debug.Log("RoomUIManager: HandleServerJsonMessage() received update_room_info.");
                HandleRoomUpdate(response);
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
    }
}