using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Steamworks;

/// <summary>
/// Manages the lifecycle of all networked entities (Players and Monsters).
/// This includes spawning, despawning, and applying state updates received from the network.
/// </summary>
public class NetworkPlayerManager : MonoBehaviour
{
    public static NetworkPlayerManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;

    // --- Entity Dictionaries ---
    // Master list of all player GameObjects, keyed by their SteamID string.
    private readonly Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    // Master list of all monster GameObjects, keyed by their unique monster ID.
    private readonly Dictionary<ushort, GameObject> monsters = new Dictionary<ushort, GameObject>();
    
    // Helper dictionary to map the host-assigned byte ID back to a player's SteamID.
    private readonly Dictionary<byte, string> byteIdToSteamId = new Dictionary<byte, string>();

    // --- Public Accessors ---
    public IReadOnlyDictionary<string, GameObject> Players => players;
    public IReadOnlyDictionary<ushort, GameObject> Monsters => monsters;
    public GameObject LocalPlayer { get; private set; }

    #region Unity Lifecycle & Setup

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

        // Unsubscribe first to prevent duplicates, then subscribe.
        NetworkManager.OnJsonMessageReceived -= HandleServerJsonMessage;
        NetworkManager.OnJsonMessageReceived += HandleServerJsonMessage;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnJsonMessageReceived -= HandleServerJsonMessage;
        }
    }

    private void HandleServerJsonMessage(CSteamID sender, string jsonMsg)
    {
        JObject msg = JObject.Parse(jsonMsg);
        string type = msg["type"]?.ToString();

        if (type == "player_action")
        {
            RoutePlayerEvent(sender, msg);
        }
    }

    /// <summary>
    /// Clears all spawned players and monsters. Called on disconnect.
    /// </summary>
    public void ClearAllNetworkEntities()
    {
        foreach (var player in players.Values) Destroy(player);
        players.Clear();
        byteIdToSteamId.Clear();
        LocalPlayer = null;

        foreach (var monster in monsters.Values) Destroy(monster);
        monsters.Clear();
    }

    #endregion

    #region Player Management

    /// <summary>
    /// Called by the host and clients when the room's player list is updated.
    /// This method synchronizes the spawned player objects with the official list from ServerRoomManager.
    /// </summary>
    public void UpdatePlayerList(JArray playerList)
    {
        byteIdToSteamId.Clear();
        List<string> steamIdsInMessage = new List<string>();

        // First, build a map of all players in the message
        foreach (JObject playerInfoJson in playerList)
        {
            PlayerInfo playerInfo = playerInfoJson.ToObject<PlayerInfo>();
            steamIdsInMessage.Add(playerInfo.steam_id);

            if (byte.TryParse(playerInfo.player_id, out byte byteId))
            {
                byteIdToSteamId[byteId] = playerInfo.steam_id;
            }
        }

        // Despawn any players that are no longer in the list
        List<string> currentPlayers = new List<string>(players.Keys);
        foreach (string steamId in currentPlayers)
        {
            if (!steamIdsInMessage.Contains(steamId))
            {
                RemovePlayer(steamId);
            }
        }
        
        // Find our own byte ID now that the map is populated
        byte myId = FindMyPlayerId();
        if (myId != NetworkManager.INVALID_PLAYER_ID)
        {
            NetworkManager.Instance.SetMyPlayerId(myId);
        }

        // Spawn any new players that have joined
        foreach (JObject playerInfoJson in playerList)
        {
            PlayerInfo playerInfo = playerInfoJson.ToObject<PlayerInfo>();
            // If the player is not in our list, or if their GameObject has been destroyed (stale entry), spawn them.
            if (!players.TryGetValue(playerInfo.steam_id, out GameObject playerGO) || playerGO == null)
            {
                SpawnPlayer(playerInfo);
            }
        }
    }

    private GameObject SpawnPlayer(PlayerInfo playerInfo)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[NetworkPlayerManager] Player Prefab is not assigned!");
            return null;
        }

        // If there's a stale entry, remove it before adding the new one.
        if (players.ContainsKey(playerInfo.steam_id))
        {
            players.Remove(playerInfo.steam_id);
        }

        GameObject playerObject = Instantiate(playerPrefab, new Vector3(0, 1.4f, 0), Quaternion.identity);
        playerObject.name = $"Player_{playerInfo.nickname}";
        players.Add(playerInfo.steam_id, playerObject);

        var networkPlayer = playerObject.AddComponent<NetworkPlayer>();
        bool isMine = (playerInfo.steam_id == NetworkManager.Instance.selfSteamId.ToString());
        networkPlayer.Initialize(playerInfo.steam_id, isMine);

        if (isMine)
        {
            LocalPlayer = playerObject;
        }

        if (networkPlayer.NicknameUI != null)
        {
            networkPlayer.NicknameUI.SetNickname(playerInfo.nickname);
        }

        DontDestroyOnLoad(playerObject);
        return playerObject;
    }

    public void RemovePlayer(string steamId)
    {
        if (players.TryGetValue(steamId, out GameObject playerToDestroy))
        {
            if (playerToDestroy == LocalPlayer)
            {
                LocalPlayer = null;
            }
            Debug.Log($"[NetworkPlayerManager] Removing player {steamId}.");
            Destroy(playerToDestroy);
            players.Remove(steamId);
        }
    }

    /// <summary>
    /// Iterates through the ID map to find the local player's assigned byte ID.
    /// </summary>
    public byte FindMyPlayerId()
    {
        foreach (var entry in byteIdToSteamId)
        {
            if (ulong.TryParse(entry.Value, out ulong steamIdUlong))
            {
                if (new CSteamID(steamIdUlong) == NetworkManager.Instance.selfSteamId)
                {
                    return entry.Key;
                }
            }
        }
        return NetworkManager.INVALID_PLAYER_ID;
    }

    #endregion

    #region Monster Management

    /// <summary>
    /// Called on clients when a reliable 'MonsterSpawn' message is received.
    /// Creates a new monster instance based on the state provided by the host.
    /// </summary>
    public void SpawnMonsterFromState(MonsterState state)
    {
        if (monsters.ContainsKey(state.monsterId))
        {
            // This can happen if a reliable packet is resent. It's safe to ignore.
            return;
        }

        GameObject prefab = SpawnManager.Instance.GetPrefab(state.monsterType);
        if (prefab == null)
        {
            Debug.LogError($"[NetworkPlayerManager] Could not find prefab for monster type: {state.monsterType}");
            return;
        }

        GameObject newMonsterGO = Instantiate(prefab, state.position, state.rotation);
        
        NetworkMonster networkMonster = newMonsterGO.GetComponent<NetworkMonster>();
        if (networkMonster == null)
        {
            Debug.LogError($"[NetworkPlayerManager] Monster prefab '{prefab.name}' is missing the NetworkMonster component!");
            Destroy(newMonsterGO);
            return;
        }

        networkMonster.Initialize(state.monsterId, state.monsterType);
        newMonsterGO.name = $"{prefab.name}_{state.monsterId}";
        monsters.Add(state.monsterId, newMonsterGO);
        Debug.Log($"[NetworkPlayerManager] Client spawned monster {newMonsterGO.name} from network message.");

        // Disable components that are host-authoritative
        var monsterMovement = newMonsterGO.GetComponent<IMonsterMovement>();
        if (monsterMovement is MonoBehaviour) (monsterMovement as MonoBehaviour).enabled = false;

        var monsterAI = newMonsterGO.GetComponent<MonsterAIController>();
        if (monsterAI != null) monsterAI.enabled = false;
    }

    /// <summary>
    /// Called on clients when a reliable 'MonsterDespawn' message is received.
    /// </summary>
    public void DespawnMonster(ushort monsterId)
    {
        if (monsters.TryGetValue(monsterId, out GameObject monsterToDestroy))
        {
            Debug.Log($"[NetworkPlayerManager] Despawning monster {monsterId} by network message.");
            // Note: We let SpawnManager handle the actual deactivation/pooling.
            SpawnManager.Instance.ReturnMonsterToPool(monsterToDestroy); 
            monsters.Remove(monsterId);
        }
    }

    #endregion

    #region State Update Handlers

    /// <summary>
    /// Called every FixedUpdate on clients. Applies the latest player state updates from the host.
    /// </summary>
    public void UpdateFromGameState(NetworkGameState state)
    {
        foreach (var playerState in state.players)
        {
            if (!byteIdToSteamId.TryGetValue(playerState.playerId, out string steamId))
            {
                continue;
            }

            if (players.TryGetValue(steamId, out GameObject playerObject))
            {
                // Do not apply network updates to our own player object
                if (steamId == NetworkManager.Instance.PlayerId)
                    continue;

                var networkPlayer = playerObject.GetComponent<NetworkPlayer>();
                if (networkPlayer == null) continue;

                // Apply transform and animation sync
                if (networkPlayer.BodyTransformSync != null)
                {
                    networkPlayer.BodyTransformSync.OnTransformReceived(playerState.position, playerState.rotation);
                }
                if (networkPlayer.CameraTransformSync != null)
                {
                    networkPlayer.CameraTransformSync.OnTransformReceived(networkPlayer.CameraTransformSync.transform.position, playerState.cameraRotation);
                }
                if (networkPlayer.AnimatorSync != null)
                {
                    networkPlayer.AnimatorSync.OnAnimationDataReceived(
                        playerState.moveX, playerState.moveY,
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Walk),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Sprint),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Roll),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.IsGrounded),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Crouch)
                    );
                }

                // Apply weapon sync
                if (networkPlayer.WeaponController != null && networkPlayer.WeaponController.activeID != playerState.weaponId)
                {
                    networkPlayer.WeaponController.ToChange(playerState.weaponId);
                }
            }
        }
    }

    /// <summary>
    /// Called every FixedUpdate on clients. Applies the latest monster state updates from the host.
    /// </summary>
    public void OnMonsterUpdateReceived(NetworkMonsterUpdateState state)
    {
        if (NetworkManager.Instance.Mode == NetworkMode.Host) return;

        foreach (var monsterState in state.monsters)
        {
            if (monsters.TryGetValue(monsterState.monsterId, out GameObject monsterGO))
            {
                monsterGO.transform.position = monsterState.position;
                monsterGO.transform.rotation = monsterState.rotation;

                var monsterAnimSync = monsterGO.GetComponent<NetworkMonsterAnimatorSync>();
                if (monsterAnimSync != null)
                {
                    var animData = NetworkMonsterAnimatorSync.Deserialize(monsterState.animationData);
                    monsterAnimSync.OnAnimationDataReceived(animData);
                }
            }
        }
    }

    /// <summary>
    /// Routes a custom JSON event to the appropriate player's state machine.
    /// </summary>
    public void RoutePlayerEvent(CSteamID sender, JObject eventData)
    {
        string senderSteamId = sender.ToString();

        if (players.TryGetValue(senderSteamId, out GameObject playerObject))
        {
            var nsm = playerObject.GetComponentInChildren<NetworkStateMachine>();
            if (nsm != null) nsm.OnNetworkEvent(eventData);
        }
    }

    #endregion
}
