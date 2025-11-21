using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the state of the lobby/room, including the list of players and game settings like the selected planet.
/// This manager is host-authoritative, meaning the host is the single source of truth for all room data.
/// </summary>
public class ServerRoomManager : MonoBehaviour
{
    public static ServerRoomManager Instance { get; private set; }

    // --- Public State (synchronized from host) ---
    public List<PlayerInfo> PlayerList { get; private set; } = new List<PlayerInfo>();
    public int SelectedPlanetId { get; private set; } = -1;
    public string RoomName { get; private set; } = "Space Crew"; // Default name
    public string HostId { get; private set; }

    /// <summary>
    /// Fired whenever room data is updated by the host. UI subscribes to this.
    /// </summary>
    public static event Action OnRoomDataUpdated;

    // --- Host-Only Data ---
    // The authoritative dictionary of players currently in the room.
    private readonly Dictionary<CSteamID, PlayerInfo> playersInRoom = new Dictionary<CSteamID, PlayerInfo>();
    // Host-only lookups to map between a player's permanent SteamID and their temporary, session-specific byte ID.
    private readonly Dictionary<CSteamID, byte> steamIdToByteId = new Dictionary<CSteamID, byte>();
    private readonly Dictionary<byte, CSteamID> byteIdToSteamId = new Dictionary<byte, CSteamID>();
    private byte nextPlayerId = 0; // Simple counter for assigning player IDs. Host is always 0.

    #region Unity Lifecycle & Initialization

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
        }
    }

    /// <summary>
    /// Called by NetworkManager after the network mode has been determined.
    /// Subscribes to the correct message handlers based on whether we are the host or a client.
    /// </summary>
    public void Initialize(NetworkMode mode)
    {
        // Unsubscribe first to prevent duplicate subscriptions on re-join
        NetworkManager.OnJsonMessageReceived -= HandleHostJsonMessage;
        NetworkManager.OnJsonMessageReceived -= HandleServerJsonMessage;

        if (mode == NetworkMode.Host)
        {
            NetworkManager.OnJsonMessageReceived += HandleHostJsonMessage;
        }
        else // Client
        {
            NetworkManager.OnJsonMessageReceived += HandleServerJsonMessage;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            if (NetworkManager.Instance != null)
            {
                // Unsubscribe from all possible handlers to prevent errors on shutdown
                NetworkManager.OnJsonMessageReceived -= HandleHostJsonMessage;
                NetworkManager.OnJsonMessageReceived -= HandleServerJsonMessage;
            }
        }
    }

    #endregion

    #region Host-Only Logic

    /// <summary>
    /// (Host-only) Main router for JSON messages received from clients.
    /// </summary>
    private void HandleHostJsonMessage(CSteamID sender, string jsonMsg)
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;

        JObject msg = JObject.Parse(jsonMsg);
        string type = msg["type"]?.ToString();

        switch (type)
        {
            case "set_nickname":
                HandleSetNickname(sender, msg);
                break;
            case "propose_planet":
                HandlePlanetProposal(sender, msg);
                break;
            case "player_dealt_damage":
                HandlePlayerDealtDamage(msg);
                break;
            case "picked_up_loot":
                HandleLootPickup(msg);
                break;
        }
    }

    /// <summary>
    /// (Host-only) Handles a client's report that they have picked up a loot item.
    /// </summary>
    private void HandleLootPickup(JObject data)
    {
        if (LootManager.Instance == null) return;

        ushort lootNetId = data["lootNetId"]?.ToObject<ushort>() ?? 0;
        if (lootNetId == 0) return;

        // Create the destroy message
        JObject destroyMsg = new JObject
        {
            ["type"] = "destroy_loot",
            ["lootNetId"] = lootNetId
        };

        // Broadcast to all clients (and run on host)
        NetworkManager.Instance.BroadcastJsonMessage(destroyMsg);
    }

    /// <summary>
    /// (Host-only) Handles a client's report that they have dealt damage to a monster.
    /// </summary>
    private void HandlePlayerDealtDamage(JObject data)
    {
        if (SpawnManager.Instance == null) return;

        ushort monsterId = data["monsterId"]?.ToObject<ushort>() ?? 0;
        float damage = data["damage"]?.ToObject<float>() ?? 0f;

        if (monsterId == 0 || damage == 0f) return;

        foreach (var monsterGO in SpawnManager.Instance.SpawnedMonsters)
        {
            var networkMonster = monsterGO.GetComponent<NetworkMonster>();
            if (networkMonster != null && networkMonster.MonsterId == monsterId)
            {
                var monsterHealth = monsterGO.GetComponent<MonsterHealth>();
                if (monsterHealth != null)
                {
                    monsterHealth.TakeDamage(damage);
                    // No need to broadcast, the health change will be sent in the next MonsterUpdate
                }
                return; // Found the monster, no need to loop further
            }
        }
    }

    /// <summary>
    /// (Host-only) Handles a new client's request to join the room.
    /// </summary>
    private void HandleSetNickname(CSteamID sender, JObject data)
    {
        string nickname = data["nickname"]?.ToString();
        if (string.IsNullOrEmpty(nickname) || playersInRoom.ContainsKey(sender)) return;

        AddPlayer(sender, nickname);
    }

    /// <summary>
    /// (Host-only) Handles a client's vote for a planet.
    /// </summary>
    private void HandlePlanetProposal(CSteamID sender, JObject data)
    {
        int planetId = data["planet_id"]?.ToObject<int>() ?? -1;
        if (planetId != -1)
        {
            SelectPlanet(planetId);
            Debug.Log($"[ServerRoomManager] Host received planet proposal from {sender}. Selected planet ID: {planetId}");
        }
    }

    /// <summary>
    /// (Host-only) Adds the host player to the room.
    /// </summary>
    public void AddHostPlayer(CSteamID hostSteamId, string nickname)
    {
        if (playersInRoom.ContainsKey(hostSteamId)) return;
        AddPlayer(hostSteamId, nickname, true);
    }

    /// <summary>
    /// (Host-only) Core logic to add a player, assign an ID, and update all clients.
    /// </summary>
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

        // Broadcast the updated room state to everyone.
        StartCoroutine(DelayedBroadcast());
    }

    /// <summary>
    /// (Host-only) Removes a player who has disconnected and broadcasts the change.
    /// </summary>
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

    /// <summary>
    /// (Host-only) Sets the selected planet and broadcasts the change.
    /// </summary>
    public void SelectPlanet(int planetId)
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;
        SelectedPlanetId = planetId;
        Debug.Log($"[ServerRoomManager] Host authoritatively selected planet ID: {planetId}");
        BroadcastRoomUpdate();
    }

    /// <summary>
    /// (Host-only) Tells all clients to load the game scene, then loads it locally.
    /// </summary>
    public void LaunchToPlanet(int planetId)
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;

        PlanetData planet = GameManager.Instance.PlanetDatabase.GetPlanetById(planetId);
        if (planet == null)
        {
            Debug.LogError($"[ServerRoomManager] Cannot launch. No planet found with ID: {planetId}");
            return;
        }

        string sceneToLoad = planet.sceneName;

        JObject message = new JObject { { "type", "load_scene" }, { "scene_name", sceneToLoad } };
        NetworkManager.Instance.BroadcastJsonMessage(message);

        Debug.Log($"[ServerRoomManager] Host is loading scene: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }

    /// <summary>
    /// (Host-only) Gathers all current room data and broadcasts it to all clients.
    /// </summary>
    public void BroadcastRoomUpdate()
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;

        JObject roomInfo = new JObject
        {
            { "type", "update_room_info" },
            { "room_name", GameManager.Instance.GameSettings.defaultRoomName },
            { "host_id", steamIdToByteId[NetworkManager.Instance.selfSteamId].ToString() },
            { "selected_planet_id", SelectedPlanetId }
        };

        JArray playersArray = new JArray(playersInRoom.Values.Select(p => JObject.FromObject(p)).ToList());
        roomInfo["players"] = playersArray;

        NetworkManager.Instance.BroadcastJsonMessage(roomInfo);

        // The host also needs to process this message to update its own local state (e.g., PlayerList).
        UpdateLocalRoomData(roomInfo);
    }

    // Waits until the end of the frame to broadcast. This can prevent race conditions
    // where a client receives an update before its own local setup is complete.
    IEnumerator DelayedBroadcast()
    {
        yield return new WaitForEndOfFrame();
        BroadcastRoomUpdate();
    }

    #endregion

    #region Client & Shared Logic

    /// <summary>
    /// (Client-only) Main router for JSON messages received from the host.
    /// </summary>
    private void HandleServerJsonMessage(CSteamID sender, string jsonMsg)
    {
        if (NetworkManager.Instance.Mode == NetworkMode.Host) return;

        JObject response = JObject.Parse(jsonMsg);
        string type = response["type"]?.ToString();

        switch (type)
        {
            case "update_room_info":
                UpdateLocalRoomData(response);
                break;

            case "load_scene":
                string sceneToLoad = response["scene_name"]?.ToString();
                if (!string.IsNullOrEmpty(sceneToLoad))
                {
                    Debug.Log($"[ServerRoomManager] Received command to load scene: {sceneToLoad}");
                    SceneManager.LoadScene(sceneToLoad);
                }
                break;
        }
    }

    /// <summary>
    /// (Client & Host) Updates the local, synchronized state from a host broadcast.
    /// </summary>
    private void UpdateLocalRoomData(JObject data)
    {
        RoomName = data["room_name"]?.ToString() ?? RoomName;
        HostId = data["host_id"]?.ToString() ?? HostId;
        SelectedPlanetId = data["selected_planet_id"]?.ToObject<int>() ?? -1;

        JArray players = data["players"] as JArray;
        if (players != null)
        {
            PlayerList = players.ToObject<List<PlayerInfo>>();

            // Trigger the NetworkPlayerManager to sync player GameObjects with this new list.
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.UpdatePlayerList(players);
            }
        }

        Debug.Log($"[ServerRoomManager] Local room data updated. Players: {PlayerList.Count}, Planet: {SelectedPlanetId}");
        OnRoomDataUpdated?.Invoke();
    }

    /// <summary>
    /// (Client-only) Sends the local player's nickname to the host to formally join the room.
    /// </summary>
    public void SendNickname()
    {
        string nickname = CustomSteamManager.Instance.PlayerName;
        JObject msg = new JObject { { "type", "set_nickname" }, { "nickname", nickname } };
        CSteamID hostId = SteamMatchmaking.GetLobbyOwner(NetworkManager.Instance.CurrentLobbyID);
        NetworkManager.Instance.SendJsonMessage(hostId, msg);
    }

    /// <summary>
    /// Resets all room data. Called on disconnect.
    /// </summary>
    public void ClearRoom()
    {
        playersInRoom.Clear();
        steamIdToByteId.Clear();
        byteIdToSteamId.Clear();
        nextPlayerId = 0;

        PlayerList.Clear();
        SelectedPlanetId = -1;
        OnRoomDataUpdated?.Invoke();
    }

    public void OnInviteFriendsButtonClicked()
    {
        if (NetworkManager.Instance.CurrentLobbyID.IsValid())
        {
            Debug.Log("[ServerRoomManager] : SteamInvite Overlay Open.");
            SteamFriends.ActivateGameOverlayInviteDialog(NetworkManager.Instance.CurrentLobbyID);
        }
    }

    // --- ID Lookups ---

    public string GetPlayerId(CSteamID steamId)
    {
        if (steamIdToByteId.TryGetValue(steamId, out byte id))
            return id.ToString();
        return "255";
    }

    public string GetSteamId(byte byteId)
    {
        if (byteIdToSteamId.TryGetValue(byteId, out CSteamID steamId))
            return steamId.ToString();
        return null;
    }

    #endregion
}
