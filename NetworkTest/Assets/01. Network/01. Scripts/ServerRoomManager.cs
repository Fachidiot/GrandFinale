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
    private bool _needsPlayerSpawn = false; // Flag to check if we need to spawn players when the scene loads.

    #region Unity Lifecycle & Initialization

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

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
        else if (mode == NetworkMode.Client) // Client
        {
            NetworkManager.OnJsonMessageReceived += HandleServerJsonMessage;
        }
        // In SinglePlayer mode, we don't handle any network messages.
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

    /// <summary>
    /// When a scene is loaded, check if we have a pending player spawn action.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If we've loaded into a playable scene and have a pending spawn, execute it now.
        if (GameManager.Instance.GameSettings.playableScenes.Contains(scene.name) && _needsPlayerSpawn)
        {
            Debug.Log($"[ServerRoomManager] Scene {scene.name} loaded, processing pending player spawns.");
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.UpdatePlayerList(JArray.FromObject(PlayerList));
            }
            _needsPlayerSpawn = false; // Reset the flag
        }
    }

    #endregion

    #region Single Player Logic
    public void AddSinglePlayer()
    {
        ClearRoom(); // Ensure we're starting fresh

        ModelInfo localModelInfo = PlayerCustomizer.Instance.GetLocalPlayerInfo();
        bool isLocalMale = PlayerCustomizer.Instance.IsMale;

        var playerInfo = new PlayerInfo
        {
            steam_id = "0",
            player_id = "0",
            nickname = "Player",
            is_Male = isLocalMale,
            headIndex = localModelInfo.head,
            bodyIndex = localModelInfo.body,
            acc1Index = localModelInfo.acc1,
            acc2Index = localModelInfo.acc2,
            IsReady = true // Single player is always ready
        };

        PlayerList.Add(playerInfo);
        HostId = "0"; // In single player, we are our own host.

        Debug.Log("[ServerRoomManager] Single Player room initialized.");
        OnRoomDataUpdated?.Invoke();
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
            case "player_customization":
                HandlePlayerCustomization(sender, msg);
                break;
            case "player_ready":
                HandlePlayerReady(sender, msg);
                break;
        }
    }

    /// <summary>
    /// (Host-only) Handles a client's ready status update.
    /// </summary>
    public void HandlePlayerReady(CSteamID sender, JObject data)
    {
        if (playersInRoom.TryGetValue(sender, out PlayerInfo playerInfo))
        {
            playerInfo.IsReady = data["is_ready"]?.ToObject<bool>() ?? false;
            playersInRoom[sender] = playerInfo; // Write the modified struct back
            Debug.Log($"[ServerRoomManager] Player {playerInfo.nickname} set ready status to: {playerInfo.IsReady}. Broadcasting update.");
            BroadcastRoomUpdate();
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
    /// (Host-only) Handles a client's customization data and updates the room state.
    /// </summary>
    private void HandlePlayerCustomization(CSteamID sender, JObject data)
    {
        if (playersInRoom.TryGetValue(sender, out PlayerInfo playerInfo))
        {
            bool incomingIsMale = data["isMale"]?.ToObject<bool>() ?? playerInfo.is_Male;
            Debug.Log($"[ServerRoomManager] Handling Customization for {sender}. Incoming isMale: {incomingIsMale}.");

            // Update the PlayerInfo for the lobby UI
            playerInfo.is_Male = incomingIsMale;
            playerInfo.headIndex = data["head"]?.ToObject<int>() ?? playerInfo.headIndex;
            playerInfo.bodyIndex = data["body"]?.ToObject<int>() ?? playerInfo.bodyIndex;
            playerInfo.acc1Index = data["acc1"]?.ToObject<int>() ?? playerInfo.acc1Index;
            playerInfo.acc2Index = data["acc2"]?.ToObject<int>() ?? playerInfo.acc2Index;
            
            playersInRoom[sender] = playerInfo; // Write the modified struct back
            Debug.Log($"[ServerRoomManager] PlayerInfo for {playerInfo.nickname} is now: is_Male={playerInfo.is_Male}");

            // Also update the authoritative data in PlayerManager for in-game visuals
            if (PlayerManager.Instance != null && byte.TryParse(playerInfo.player_id, out byte byteId))
            {
                ModelInfo modelInfo = new ModelInfo(playerInfo.headIndex, playerInfo.bodyIndex, playerInfo.acc1Index, playerInfo.acc2Index);
                PlayerManager.Instance.UpdatePlayerCustomization(byteId, playerInfo.is_Male, modelInfo);
            }

            Debug.Log($"[ServerRoomManager] Player {playerInfo.nickname} ({sender}) customization updated. Broadcasting room update.");
            BroadcastRoomUpdate();
        }
    }

    /// <summary>
    /// (Host-only) Updates the host's own customization data and broadcasts the change.
    /// </summary>
    public void UpdateHostCustomization(ModelInfo modelInfo, bool isMale)
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;

        CSteamID hostSteamId = NetworkManager.Instance.selfSteamId;
        if (playersInRoom.TryGetValue(hostSteamId, out PlayerInfo playerInfo))
        {
            playerInfo.is_Male = isMale;
            playerInfo.headIndex = modelInfo.head;
            playerInfo.bodyIndex = modelInfo.body;
            playerInfo.acc1Index = modelInfo.acc1;
            playerInfo.acc2Index = modelInfo.acc2;

            playersInRoom[hostSteamId] = playerInfo; // Write the modified struct back

            Debug.Log($"[ServerRoomManager] Host customization updated. Broadcasting room update.");
            BroadcastRoomUpdate();
        }
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

        PlayerInfo playerInfo;

        if (isHost)
        {
            ModelInfo localModelInfo = PlayerCustomizer.Instance.GetLocalPlayerInfo();
            bool isLocalMale = PlayerCustomizer.Instance.IsMale;

            playerInfo = new PlayerInfo
            {
                steam_id = steamId.ToString(),
                player_id = newId.ToString(),
                nickname = nickname,
                is_Male = isLocalMale,
                headIndex = localModelInfo.head,
                bodyIndex = localModelInfo.body,
                acc1Index = localModelInfo.acc1,
                acc2Index = localModelInfo.acc2,
                IsReady = true // Host is always ready
            };
        }
        else
        {
            playerInfo = new PlayerInfo
            {
                steam_id = steamId.ToString(),
                player_id = newId.ToString(),
                nickname = nickname,
                is_Male = true, // Default to male for clients until they send customization
                headIndex = 0,
                bodyIndex = 0,
                acc1Index = 0,
                acc2Index = 0
            };
        }

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
    /// (Host or SinglePlayer) Sets the selected planet. Broadcasts the change if host.
    /// </summary>
    public void SelectPlanet(int planetId)
    {
        var mode = NetworkManager.Instance.Mode;
        if (mode != NetworkMode.Host && mode != NetworkMode.SinglePlayer) return;

        SelectedPlanetId = planetId;
        Debug.Log($"[ServerRoomManager] Authoritatively selected planet ID: {planetId}");

        if (mode == NetworkMode.Host)
        {
            BroadcastRoomUpdate();
        }
        else // SinglePlayer
        {
            OnRoomDataUpdated?.Invoke();
        }
    }

    /// <summary>
    /// (Host or SinglePlayer) Loads the game scene for the selected planet.
    /// </summary>
    public void LaunchToPlanet(int planetId)
    {
        var mode = NetworkManager.Instance.Mode;
        if (mode != NetworkMode.Host && mode != NetworkMode.SinglePlayer) return;

        PlanetData planet = GameManager.Instance.PlanetDatabase.GetPlanetById(planetId);
        if (planet == null)
        {
            Debug.LogError($"[ServerRoomManager] Cannot launch. No planet found with ID: {planetId}");
            return;
        }

        string sceneToLoad = planet.sceneName;

        if (mode == NetworkMode.Host)
        {
            JObject message = new JObject { { "type", "load_scene" }, { "scene_name", sceneToLoad } };
            NetworkManager.Instance.BroadcastJsonMessage(message);
        }

        Debug.Log($"[ServerRoomManager] Loading scene: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }

    public void LaunchToPlanet(string planetId)
    {
        var mode = NetworkManager.Instance.Mode;
        if (mode != NetworkMode.Host && mode != NetworkMode.SinglePlayer) return;

        if (mode == NetworkMode.Host)
        {
            JObject message = new JObject { { "type", "load_scene" }, { "scene_name", planetId } };
            NetworkManager.Instance.BroadcastJsonMessage(message);
        }

        Debug.Log($"[ServerRoomManager] Loading scene: {planetId}");
        SceneManager.LoadScene(planetId);
    }

    /// <summary>
    /// (Host-only) Gathers all current room data and broadcasts it to all clients.
    /// </summary>
    public void BroadcastRoomUpdate()
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;

        Debug.Log("--- Broadcasting Room Update ---");
        foreach (var player in playersInRoom.Values)
        {
            Debug.Log($"[Broadcast] Player: {player.nickname}, is_Male: {player.is_Male}");
        }
        Debug.Log("-----------------------------");

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
        }

        // --- Player Spawning Logic ---
        // If we are in the correct scene, spawn players immediately.
        // Otherwise, flag that we need to spawn them when the scene loads.
        if (GameManager.Instance.GameSettings.playableScenes.Contains(SceneManager.GetActiveScene().name))
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.UpdatePlayerList(players);
            }
        }
        else
        {
            _needsPlayerSpawn = true;
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
