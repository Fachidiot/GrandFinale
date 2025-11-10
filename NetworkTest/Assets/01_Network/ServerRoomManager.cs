using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using System.Collections;

public class ServerRoomManager : MonoBehaviour
{
    public static ServerRoomManager Instance { get; private set; }

    private Dictionary<CSteamID, PlayerInfo> playersInRoom = new Dictionary<CSteamID, PlayerInfo>();
    private Dictionary<CSteamID, byte> steamIdToByteId = new Dictionary<CSteamID, byte>();
    private Dictionary<byte, CSteamID> byteIdToSteamId = new Dictionary<byte, CSteamID>();
    private byte nextPlayerId = 0;
    private int selectedPlanetId = -1; // -1 means no planet is selected

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        NetworkManager.OnJsonMessageReceived += HandleClientJsonMessage;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnJsonMessageReceived -= HandleClientJsonMessage;
        }
    }

    private void HandleClientJsonMessage(CSteamID sender, string jsonMsg)
    {
        JObject msg = JObject.Parse(jsonMsg);
        string type = msg["type"]?.ToString();

        if (type == "set_nickname")
        {
            HandleSetNickname(sender, msg);
        }
        else if (type == "propose_planet") // New: Handle planet proposal from client
        {
            HandlePlanetProposal(sender, msg);
        }
    }

    private void HandleSetNickname(CSteamID sender, JObject data)
    {
        string nickname = data["nickname"]?.ToString();
        if (string.IsNullOrEmpty(nickname)) return;

        if (!playersInRoom.ContainsKey(sender))
        {
            AddPlayer(sender, nickname);
        }
    }

    private void HandlePlanetProposal(CSteamID sender, JObject data)
    {
        // Only the host can authoritatively select a planet.
        // This method is called when a client (or host via RoomUIManager) proposes a planet.
        if (NetworkManager.Instance.Mode != NetworkMode.Host)
        {
            Debug.LogWarning("[ServerRoomManager] Received planet proposal but not host. Ignoring.");
            return;
        }

        int planetId = data["planet_id"]?.ToObject<int>() ?? -1;
        if (planetId != -1)
        {
            SelectPlanet(planetId); // Use the authoritative method
            Debug.Log($"[ServerRoomManager] Host received planet proposal from {sender}. Selected planet ID: {planetId}");
        }
        else
        {
            Debug.LogWarning($"[ServerRoomManager] Invalid planet ID received in proposal from {sender}.");
        }
    }

    public void AddHostPlayer(CSteamID hostSteamId, string nickname)
    {
        if (playersInRoom.ContainsKey(hostSteamId)) return;
        AddPlayer(hostSteamId, nickname, true);
    }

    private void AddPlayer(CSteamID steamId, string nickname, bool isHost = false)
    {
        byte newId = nextPlayerId++;
        steamIdToByteId[steamId] = newId;
        byteIdToSteamId[newId] = steamId;

        var playerInfo = new PlayerInfo
        {
            steam_id = steamId.ToString(),
            player_id = newId.ToString(),
            nickname = nickname,
            is_ready = false
        };
        playersInRoom[steamId] = playerInfo;

        // Debug.Log($"[ServerRoomManager] Player {nickname} ({steamId}) joined as ID {newId}");

        // Broadcast at the end of the frame to ensure all listeners are ready
        // Debug.Log("ServerRoomManager: AddPlayer() called. Starting DelayedBroadcast.");
        StartCoroutine(DelayedBroadcast());
    }

    IEnumerator DelayedBroadcast()
    {
        // Debug.Log("ServerRoomManager: DelayedBroadcast() coroutine started.");
        // Wait until the end of the frame to ensure all Start/OnEnable methods have run
        yield return new WaitForEndOfFrame();
        // Debug.Log("ServerRoomManager: EndOfFrame reached. Calling BroadcastRoomUpdate.");
        BroadcastRoomUpdate();
    }

    public void RemovePlayer(CSteamID steamId)
    {
        if (playersInRoom.Remove(steamId) && steamIdToByteId.TryGetValue(steamId, out byte id))
        {
            steamIdToByteId.Remove(steamId);
            byteIdToSteamId.Remove(id);
            Debug.Log($"[ServerRoomManager] Player {steamId} removed.");
            BroadcastRoomUpdate();
        }
    }

    // Authoritative method for the host to select a planet
    public void SelectPlanet(int planetId)
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host)
        {
            Debug.LogWarning("Only the host can authoritatively select a planet. This method should only be called on the host.");
            return;
        }

        selectedPlanetId = planetId;
        Debug.Log($"[ServerRoomManager] Host authoritatively selected planet ID: {planetId}");

        // Immediately notify all clients of the change
        BroadcastRoomUpdate();
    }

    public void BroadcastRoomUpdate()
    {
        // Debug.Log("ServerRoomManager: BroadcastRoomUpdate() called.");
        // Debug.Log($"BroadcastRoomUpdate: Checking mode. Current mode is: {NetworkManager.Instance.Mode}");
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;

        JObject roomInfo = new JObject
        {
            { "type", "update_room_info" },
            { "room_name", "Test Room" },
            { "host_id", steamIdToByteId[NetworkManager.Instance.selfSteamId].ToString() },
            { "selected_planet_id", selectedPlanetId } // Add selected planet info
        };

        JArray playersArray = new JArray();
        foreach (var entry in playersInRoom)
        {
            playersArray.Add(JObject.FromObject(entry.Value));
        }
        roomInfo["players"] = playersArray;

        NetworkManager.Instance.BroadcastJsonMessage(roomInfo);
    }

    public string GetPlayerId(CSteamID steamId)
    {
        if (steamIdToByteId.TryGetValue(steamId, out byte id))
        {
            return id.ToString();
        }
        return "255";
    }

    public string GetSteamId(byte byteId)
    {
        if (byteIdToSteamId.TryGetValue(byteId, out CSteamID steamId))
        {
            return steamId.ToString();
        }
        return null;
    }

    public void ClearRoom()
    {
        playersInRoom.Clear();
        steamIdToByteId.Clear();
        byteIdToSteamId.Clear();
        nextPlayerId = 0;
        selectedPlanetId = -1; // Reset planet selection
    }

    public void OnInviteFriendsButtonClicked()
    {
        if (NetworkManager.Instance.CurrentLobbyID.IsValid())
        {
            SteamFriends.ActivateGameOverlayInviteDialog(NetworkManager.Instance.CurrentLobbyID);
        }
        else
        {
            Debug.LogWarning("Cannot invite friends, not in a valid lobby.");
        }
    }
}