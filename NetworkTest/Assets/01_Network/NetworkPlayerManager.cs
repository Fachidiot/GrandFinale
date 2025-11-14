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
        PlayerNicknameUI nicknameUI = playerObject.GetComponentInChildren<PlayerNicknameUI>();

        if (nicknameUI != null)
            nicknameUI.SetNickname(playerInfo.nickname);

        bool isMine = (playerInfo.steam_id == NetworkManager.Instance.selfSteamId.ToString());
        var transformSyncs = playerObject.GetComponentsInChildren<NetworkTransformSync>();

        foreach (var view in transformSyncs)
            view.Initialize(playerInfo.steam_id, isMine);

        var animSync = playerObject.GetComponentInChildren<NetworkAnimatorSync>();

        if (animSync != null)
            animSync.Initialize(playerInfo.steam_id, isMine);

        var nsm = playerObject.GetComponentInChildren<NetworkStateMachine>();

        if (nsm != null)
            nsm.Initialize(isMine);

        if (isMine)
        {
            if (nicknameUI != null)
                nicknameUI.gameObject.SetActive(false);
            var inGameUI = FindObjectOfType<InGameUIManager>();
            if (inGameUI != null)
            {
                inGameUI.SetInit(playerObject.GetComponent<WeaponController>());
            }
        }
        else
        {
            playerObject.GetComponent<CharacterMove>().enabled = false;
            playerObject.GetComponentInChildren<InputHandler>().enabled = false;
            playerObject.GetComponentInChildren<CameraController>().enabled = false;
            playerObject.GetComponentInChildren<CameraSwitcher>()?.gameObject.SetActive(false);
        }

        DontDestroyOnLoad(playerObject);

        return playerObject;
    }

    public void RemovePlayer(string steamId)
    {
        if (players.TryGetValue(steamId, out GameObject playerToDestroy))
        {
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

                var transformSyncs = playerObject.GetComponentsInChildren<NetworkTransformSync>();
                var bodySync = transformSyncs.FirstOrDefault(s => s.viewId == 0);
                var cameraSync = transformSyncs.FirstOrDefault(s => s.viewId == 1);
                if (bodySync != null)
                {
                    bodySync.OnTransformReceived(playerState.position, playerState.rotation);
                }
                if (cameraSync != null)
                {
                    cameraSync.OnTransformReceived(cameraSync.transform.position, playerState.cameraRotation);
                }

                var animSync = playerObject.GetComponentInChildren<NetworkAnimatorSync>();
                if (animSync != null)
                {
                    animSync.OnAnimationDataReceived(
                        playerState.moveX,
                        playerState.moveY,
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Walk),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Sprint),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Roll),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.IsGrounded),
                        AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Crouch)
                    );
                }

                var weaponController = playerObject.GetComponentInChildren<WeaponController>();
                if (weaponController != null && weaponController.activeID != playerState.weaponId)
                {
                    weaponController.ToChange(playerState.weaponId);
                }
            }
        }

        // --- Monster States ---
        if (NetworkManager.Instance.Mode == NetworkMode.Host) return; // Host manages its own monsters

        HashSet<ushort> receivedMonsterIds = new HashSet<ushort>();

        foreach (var monsterState in state.monsters)
        {
            receivedMonsterIds.Add(monsterState.monsterId);

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
            else
            {
                // Monster is new, spawn it
                SpawnMonster(monsterState);
            }
        }

        // Despawn any monsters that are no longer in the game state
        List<ushort> monstersToDestroy = new List<ushort>();
        foreach (var monsterId in monsters.Keys)
        {
            if (!receivedMonsterIds.Contains(monsterId))
            {
                monstersToDestroy.Add(monsterId);
            }
        }

        foreach (var monsterId in monstersToDestroy)
        {
            if (monsters.TryGetValue(monsterId, out GameObject monsterToDestroy))
            {
                Destroy(monsterToDestroy);
            }
            monsters.Remove(monsterId);
        }
    }
    #endregion

    #region Spawning

    private void SpawnMonster(MonsterState state)
    {
        // Get the correct prefab from the SpawnManager using the monsterType from the state
        GameObject prefabToSpawn = SpawnManager.Instance.GetPrefab(state.monsterType);
        if (prefabToSpawn == null)
        {
            Debug.LogError($"[NetworkPlayerManager] No prefab found for monster type: {state.monsterType}");
            return;
        }

        GameObject monsterGO = Instantiate(prefabToSpawn, state.position, state.rotation);

        NetworkMonster networkMonster = monsterGO.GetComponent<NetworkMonster>();
        if (networkMonster == null)
        {
            Debug.LogError($"Monster prefab '{prefabToSpawn.name}' is missing the NetworkMonster component!");
            Destroy(monsterGO);
            return;
        }

        // Initialize with both ID and Type
        networkMonster.Initialize(state.monsterId, state.monsterType);
        monsterGO.name = $"{prefabToSpawn.name}_{state.monsterId}";

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
