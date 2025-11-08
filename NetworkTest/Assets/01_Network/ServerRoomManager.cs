
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Steamworks;

public class ServerRoomManager : MonoBehaviour
{
    public static ServerRoomManager Instance { get; private set; }

    private List<PlayerInfo> playersInRoom = new List<PlayerInfo>();
    private Dictionary<string, System.Action<ClientConnection, JObject>> messageHandlers;

    private void Awake()
    {
        Debug.Log("[ServerRoomManager] Awake called.");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMessageHandlers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        NetworkManager.OnClientMessageReceived += HandleClientMessage;
        // We also need to handle client disconnections to remove them from the list
    }

    private void OnDisable()
    {
        NetworkManager.OnClientMessageReceived -= HandleClientMessage;
    }

    private void InitializeMessageHandlers()
    {
        messageHandlers = new Dictionary<string, System.Action<ClientConnection, JObject>>
        {
            { "set_nickname", HandleSetNickname }
        };
    }

    private void HandleClientMessage(ClientConnection client, string jsonMsg)
    {
        JObject msg = JObject.Parse(jsonMsg);
        string type = msg["type"]?.ToString();

        if (type != null && messageHandlers.TryGetValue(type, out var handler))
        {
            handler(client, msg);
        }
    }
    
    private void HandleSetNickname(ClientConnection client, JObject data)
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host || client == null) return;

        Debug.Log("[ServerRoomManager] Received set_nickname message.");

        string nickname = data["nickname"]?.ToString();
        
        // Use the hash code of the client object for a unique and stable ID for the session.
        string newPlayerId = client.TcpClient.GetHashCode().ToString();

        // Avoid adding the same client twice
        if (playersInRoom.Any(p => p.player_id == newPlayerId)) 
        {
            Debug.LogWarning($"[ServerRoomManager] Player with ID {newPlayerId} already exists.");
            return;
        }

        Debug.Log($"[ServerRoomManager] New player '{nickname}' joined with ID {newPlayerId}");

        // Store the ID in the connection object for future reference (e.g., disconnects)
        client.PlayerId = newPlayerId;

        PlayerInfo newPlayer = new PlayerInfo
        {
            player_id = newPlayerId,
            nickname = nickname,
            is_ready = false
        };
        playersInRoom.Add(newPlayer);

        // After adding the new player, broadcast the updated room info to everyone.
        BroadcastRoomUpdate();
    }

    public void AddHostPlayer(PlayerInfo hostInfo)
    {
        if (playersInRoom.Any(p => p.player_id == hostInfo.player_id)) return;
        
        playersInRoom.Add(hostInfo);
        Debug.Log($"Host '{hostInfo.nickname}' added to room.");
        BroadcastRoomUpdate();
    }

    private void BroadcastRoomUpdate()
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;

        Debug.Log("[ServerRoomManager] Broadcasting room update...");
        
        UpdateRoomInfoPayload payload = new UpdateRoomInfoPayload
        {
            type = "update_room_info",
            room_name = "My Game Room", // Example name
            host_id = "0", // Host is always 0
            players = playersInRoom
        };

        string jsonPayload = JsonConvert.SerializeObject(payload);
        Debug.Log($"[ServerRoomManager] Broadcast content: {jsonPayload}");
        NetworkManager.Instance.SendTCPMessage(jsonPayload);
    }

    public void OnInviteFriendsButtonClicked()
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host)
        {
            Debug.LogWarning("[ServerRoomManager] Only the host can invite friends.");
            return;
        }

        CSteamID lobbyID = NetworkManager.Instance.CurrentLobbyID;
        if (!lobbyID.IsValid())
        {
            Debug.LogError("[ServerRoomManager] Cannot invite friends: Invalid Lobby ID.");
            return;
        }

        Debug.Log("[ServerRoomManager] Opening Steam invite dialog...");
        SteamFriends.ActivateGameOverlayInviteDialog(lobbyID);
    }
}
