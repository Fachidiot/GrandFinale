using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Steamworks;

public class NetworkPlayerManager : MonoBehaviour
{
    public static NetworkPlayerManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject playerPrefab;

    private readonly Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    private readonly Dictionary<byte, string> byteIdToSteamId = new Dictionary<byte, string>();
    private readonly Dictionary<ushort, GameObject> monsters = new Dictionary<ushort, GameObject>();

    public IReadOnlyDictionary<string, GameObject> Players => players;
    public IReadOnlyDictionary<ushort, GameObject> Monsters => monsters;

    public GameObject LocalPlayer { get; private set; }

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

    #region Player Management

    public void UpdatePlayerList(JArray playerList)
    {
        // Debug.Log("NetworkPlayerManager: UpdatePlayerList() called.");
        byteIdToSteamId.Clear();
        List<string> steamIdsInMessage = new List<string>();

        foreach (JObject playerInfoJson in playerList)
        {
            PlayerInfo playerInfo = playerInfoJson.ToObject<PlayerInfo>();

            steamIdsInMessage.Add(playerInfo.steam_id);

            if (byte.TryParse(playerInfo.player_id, out byte byteId))
            {
                byteIdToSteamId[byteId] = playerInfo.steam_id;
            }
        }

        List<string> currentPlayers = new List<string>(players.Keys);

        foreach (string steamId in currentPlayers)
        {
            if (!steamIdsInMessage.Contains(steamId))
            {
                Destroy(players[steamId]);
                players.Remove(steamId);
            }
        }
        // After populating the map, find our own ID and set it in the NetworkManager

        byte myId = GetMyByteId();

        if (myId != 255)
        {
            NetworkManager.Instance.SetMyPlayerId(myId);
        }

        foreach (JObject playerInfoJson in playerList)
        {
            PlayerInfo playerInfo = playerInfoJson.ToObject<PlayerInfo>();
            if (!players.ContainsKey(playerInfo.steam_id))
            {
                SpawnPlayer(playerInfo);
            }
        }
    }

    private GameObject SpawnPlayer(PlayerInfo playerInfo)
    {
        Debug.Log($"NetworkPlayerManager: SpawnPlayer() called for steam_id: {playerInfo.steam_id}");
        if (playerPrefab == null) return null;

        GameObject playerObject = Instantiate(playerPrefab, new Vector3(0, 1.4f, 0), Quaternion.identity);
        playerObject.name = $"Player_{playerInfo.nickname}";
        players.Add(playerInfo.steam_id, playerObject);

        // Add and initialize the NetworkPlayer component
        var networkPlayer = playerObject.AddComponent<NetworkPlayer>();
        networkPlayer.Initialize(playerInfo.steam_id, playerInfo.steam_id == NetworkManager.Instance.selfSteamId.ToString());

        if (networkPlayer.IsMine)
        {
            LocalPlayer = playerObject;
        }

        if (networkPlayer.NicknameUI != null)
        {
            networkPlayer.NicknameUI.SetNickname(playerInfo.nickname);
        }

        // playerObject.tag = "NetworkPlayer"; // Add tag for easy lookup

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

    #endregion

    #region Game State Update

    public void UpdateFromGameState(NetworkGameState state)
    {
        // --- Player States ---
        foreach (var playerState in state.players)
        {
            if (!byteIdToSteamId.TryGetValue(playerState.playerId, out string steamId))
            {
                continue;
            }

            if (players.TryGetValue(steamId, out GameObject playerObject))
            {
                if (steamId == NetworkManager.Instance.PlayerId)
                    continue;

                var networkPlayer = playerObject.GetComponent<NetworkPlayer>();
                if (networkPlayer == null) continue;

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
                        playerState.moveX,
                        playerState.moveY,
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Walk),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Sprint),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Roll),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.IsGrounded),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Crouch)
                    );
                }

                if (networkPlayer.WeaponController != null && networkPlayer.WeaponController.activeID != playerState.weaponId)
                {
                    networkPlayer.WeaponController.ToChange(playerState.weaponId);
                }
            }
        }
    }

    public void OnMonsterUpdateReceived(NetworkMonsterUpdateState state)
    {
        if (NetworkManager.Instance.Mode == NetworkMode.Host) return;

        foreach (var monsterState in state.monsters)
        {
            if (monsters.TryGetValue(monsterState.monsterId, out GameObject monsterGO))
            {
                // Monster exists, update its state
                monsterGO.transform.position = monsterState.position;
                monsterGO.transform.rotation = monsterState.rotation;

                // Update monster animation state
                var monsterAnimSync = monsterGO.GetComponent<NetworkMonsterAnimatorSync>();
                if (monsterAnimSync != null)
                {
                    var animData = NetworkMonsterAnimatorSync.Deserialize(monsterState.animationData);
                    monsterAnimSync.OnAnimationDataReceived(animData);
                }
            }
            // Note: We no longer spawn monsters from the unreliable update packet.
            // Spawning is handled by the reliable MonsterSpawn message.
        }
    }

    public void DespawnMonster(ushort monsterId)
    {
        if (monsters.TryGetValue(monsterId, out GameObject monsterToDestroy))
        {
            Debug.Log($"[NetworkPlayerManager] Despawning monster {monsterId} by network message.");
            SpawnManager.Instance.ReturnMonsterToPool(monsterToDestroy);
            monsters.Remove(monsterId);
        }
    }
    #endregion

    #region Spawning

    private void SpawnMonster(MonsterState state)
    {
        GameObject monsterGO = SpawnManager.Instance.GetMonsterFromPool(state.monsterType);
        if (monsterGO == null)
        {
            Debug.LogError($"[NetworkPlayerManager] Could not get monster from pool for type: {state.monsterType}");
            return;
        }

        monsterGO.transform.position = state.position;
        monsterGO.transform.rotation = state.rotation;

        NetworkMonster networkMonster = monsterGO.GetComponent<NetworkMonster>();
        if (networkMonster == null)
        {
            Debug.LogError($"Monster prefab '{monsterGO.name}' is missing the NetworkMonster component!");
            SpawnManager.Instance.ReturnMonsterToPool(monsterGO); // Return to pool
            return;
        }

        // Initialize with both ID and Type
        networkMonster.Initialize(state.monsterId, state.monsterType);
        monsterGO.name = $"{GetPrefabName(state.monsterType)}_{state.monsterId}";

        monsters.Add(state.monsterId, monsterGO);
        Debug.Log($"[NetworkPlayerManager] Spawned monster {monsterGO.name} from network state.");

        // Disable components that are host-authoritative
        var monsterMovement = monsterGO.GetComponent<IMonsterMovement>();
        if (monsterMovement != null && monsterMovement is MonoBehaviour)
        {
            (monsterMovement as MonoBehaviour).enabled = false;
        }

        var monsterAI = monsterGO.GetComponent<MonsterAIController>();
        if (monsterAI != null)
        {
            monsterAI.enabled = false;
        }
    }

    private string GetPrefabName(MonsterType type)
    {
        GameObject prefab = SpawnManager.Instance.GetPrefab(type);
        return prefab != null ? prefab.name : "UnknownMonster";
    }

    public void SpawnMonsterFromState(MonsterState state)
    {
        // This method is called on clients when a reliable spawn message is received.
        if (monsters.ContainsKey(state.monsterId))
        {
            // We already know about this monster, maybe just update its state
            GameObject monsterGO = monsters[state.monsterId];
            monsterGO.transform.position = state.position;
            monsterGO.transform.rotation = state.rotation;
            return;
        }

        // If we get here, it's a new monster for us.
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
        if (monsterMovement != null && monsterMovement is MonoBehaviour)
        {
            (monsterMovement as MonoBehaviour).enabled = false;
        }

        var monsterAI = newMonsterGO.GetComponent<MonsterAIController>();
        if (monsterAI != null)
        {
            monsterAI.enabled = false;
        }
    }

    #endregion

    #region Player Actions

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

    public void ClearAllNetworkEntities()
    {
        foreach (var player in players.Values) Destroy(player);
        players.Clear();
        byteIdToSteamId.Clear();
        LocalPlayer = null;

        foreach (var monster in monsters.Values) Destroy(monster);
        monsters.Clear();
    }

    public byte GetMyByteId()
    {
        foreach (var entry in byteIdToSteamId)
        {
            if (ulong.TryParse(entry.Value, out ulong steamIdUlong))
            {
                CSteamID steamId = new CSteamID(steamIdUlong);
                // Debug.Log($"GetMyByteId: Comparing map CSteamID {steamId.m_SteamID} with my CSteamID {NetworkManager.Instance.selfSteamId.m_SteamID}");
                if (steamId == NetworkManager.Instance.selfSteamId)
                {
                    return entry.Key;
                }
            }
        }
        return 255; // Invalid ID
    }
}
