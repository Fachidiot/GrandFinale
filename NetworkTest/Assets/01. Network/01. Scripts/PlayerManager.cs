using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private readonly Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    private readonly Dictionary<ushort, GameObject> monsters = new Dictionary<ushort, GameObject>();
    private readonly Dictionary<byte, string> byteIdToSteamId = new Dictionary<byte, string>();

    // Customization Data Storage
    private readonly Dictionary<byte, bool> _playerGenders = new Dictionary<byte, bool>();
    private readonly Dictionary<byte, ModelInfo> _playerModelInfos = new Dictionary<byte, ModelInfo>();

    public IReadOnlyDictionary<string, GameObject> Players => players;
    public IReadOnlyDictionary<ushort, GameObject> Monsters => monsters;
    public IPlayerControllable LocalPlayer { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (gameObject.scene.name != "DontDestroyOnLoad" && transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (NetworkManager.Instance)
        {
            NetworkManager.OnJsonMessageReceived -= HandleServerJsonMessage;
            NetworkManager.OnJsonMessageReceived += HandleServerJsonMessage;
        }

#if UNITY_EDITOR    // Fast Debug Needs it
        if (FindObjectOfType<CharacterMove>())
            LocalPlayer = FindObjectOfType<CharacterMove>().GetComponent<IPlayerControllable>();
#endif
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance)
        {
            NetworkManager.OnJsonMessageReceived -= HandleServerJsonMessage;
        }
    }

    public void SpawnInitialPlayer()
    {
        if (NetworkManager.Instance.Mode == NetworkMode.SinglePlayer)
        {
            SpawnSinglePlayer();
        }
        // In network modes, player spawning is handled by UpdatePlayerList
    }

    private void SpawnSinglePlayer()
    {
        if (PlayerCustomizer.Instance == null)
        {
            Debug.LogError("[PlayerManager] Single Player Prefab is not assigned!");
            return;
        }
        if (LocalPlayer != null && LocalPlayer.gameObject != null)
        {
            Destroy(LocalPlayer.gameObject);
        }

        GameObject playerObject = Instantiate(PlayerCustomizer.Instance.GetSinglePlayerPrefab(), GameManager.Instance != null && GameManager.Instance.GameSettings != null && GameManager.Instance.GameSettings.tutorialSpawnPoint != null ? GameManager.Instance.GameSettings.tutorialSpawnPoint.position : new Vector3(0, 1.4f, 0), Quaternion.identity);
        playerObject.name = "SinglePlayer";

        ModelInfo modelInfo = PlayerCustomizer.Instance.GetLocalPlayerInfo();
        playerObject.GetComponentInChildren<ModelCustom>().ApplyModelInfo(modelInfo);

        // No network ID for single player, so we can't use the dictionaries.
        // But we set the LocalPlayer which is enough.

        var singlePlayer = playerObject.GetComponent<SinglePlayer>();
        LocalPlayer = singlePlayer;
        singlePlayer.Initialize();
        DontDestroyOnLoad(playerObject);

        OptionDataManager.Instance.inGameUIPanel.SetActive(true);
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

    public void ClearAllNetworkEntities()
    {
        // Destroy all remote player objects
        foreach (var player in players.Values)
        {
            if (player != null) Destroy(player);
        }
        players.Clear();
        byteIdToSteamId.Clear();
        _playerGenders.Clear();
        _playerModelInfos.Clear();

        // Destroy the local player object (which could be a SinglePlayer or a NetworkPlayer)
        if (LocalPlayer != null && LocalPlayer.gameObject != null)
        {
            Destroy(LocalPlayer.gameObject);
            LocalPlayer = null;
        }

        // Destroy all monster objects
        foreach (var monster in monsters.Values)
        {
            if (monster != null) Destroy(monster);
        }
        monsters.Clear();
    }

    public void UpdatePlayerList(JArray playerList)
    {
        // Guard clause to prevent spawning players outside of designated playable scenes.
        if (!GameManager.Instance.GameSettings.playableScenes.Contains(SceneManager.GetActiveScene().name))
        {
            Debug.Log($"[PlayerManager] Skipping UpdatePlayerList because current scene '{SceneManager.GetActiveScene().name}' is not in the playableScenes list.");
            return;
        }

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
                RemovePlayer(steamId);
            }
        }

        byte myId = FindMyPlayerId();
        if (myId != NetworkManager.INVALID_PLAYER_ID)
        {
            NetworkManager.Instance.SetMyPlayerId(myId);
        }

        foreach (JObject playerInfoJson in playerList)
        {
            PlayerInfo playerInfo = playerInfoJson.ToObject<PlayerInfo>();
            if (!players.TryGetValue(playerInfo.steam_id, out GameObject playerGO) || playerGO == null)
            {
                SpawnNetworkPlayer(playerInfo);
            }
        }
    }

    private GameObject SpawnNetworkPlayer(PlayerInfo playerInfo)
    {
        if (PlayerCustomizer.Instance == null)
        {
            Debug.LogError("[PlayerManager] Player Prefab is not assigned!");
            return null;
        }

        if (players.ContainsKey(playerInfo.steam_id))
        {
            players.Remove(playerInfo.steam_id);
        }

        bool isMine = (playerInfo.steam_id == NetworkManager.Instance.selfSteamId.ToString());

        GameObject playerObject = Instantiate(PlayerCustomizer.Instance.GetNetworkPlayerPrefab(isMine, playerInfo.is_Male), GameManager.Instance != null && GameManager.Instance.GameSettings != null && GameManager.Instance.GameSettings.tutorialSpawnPoint != null ? GameManager.Instance.GameSettings.tutorialSpawnPoint.position : new Vector3(0, 1.4f, 0), Quaternion.identity);
        playerObject.name = $"Player_{playerInfo.nickname}";
        players.Add(playerInfo.steam_id, playerObject);

        var networkPlayer = playerObject.GetComponent<NetworkPlayer>();
        networkPlayer.Initialize(playerInfo.steam_id, isMine);

        if (networkPlayer.NicknameUI != null)
            networkPlayer.NicknameUI.SetNickname(playerInfo.nickname);

        ModelInfo modelInfo;
        if (isMine)
        {
            LocalPlayer = networkPlayer;
            modelInfo = PlayerCustomizer.Instance.GetLocalPlayerInfo();
            playerObject.GetComponentInChildren<ModelCustom>().ApplyModelInfo(modelInfo);
            OptionDataManager.Instance.inGameUIPanel.SetActive(true);
        }
        else
        {
            modelInfo = new ModelInfo(
                playerInfo.headIndex,
                playerInfo.bodyIndex,
                playerInfo.acc1Index,
                playerInfo.acc2Index);
            playerObject.GetComponentInChildren<ModelCustom>().ApplyModelInfo(modelInfo);
        }

        // Store initial customization
        if (byte.TryParse(playerInfo.player_id, out byte byteId))
        {
            _playerGenders[byteId] = playerInfo.is_Male;
            _playerModelInfos[byteId] = modelInfo;
        }

        DontDestroyOnLoad(playerObject);
        return playerObject;
    }

    public void RemovePlayer(string steamId)
    {
        if (players.TryGetValue(steamId, out GameObject playerToDestroy))
        {
            byte playerId = NetworkManager.INVALID_PLAYER_ID;
            foreach (var entry in byteIdToSteamId)
            {
                if (entry.Value == steamId)
                {
                    playerId = entry.Key;
                    break;
                }
            }

            if (playerId != NetworkManager.INVALID_PLAYER_ID)
            {
                byteIdToSteamId.Remove(playerId);
                _playerGenders.Remove(playerId);
                _playerModelInfos.Remove(playerId);
            }

            if (LocalPlayer != null && playerToDestroy == LocalPlayer.gameObject)
            {
                LocalPlayer = null;
            }
            Debug.Log($"[PlayerManager] Removing player {steamId}.");
            Destroy(playerToDestroy);
            players.Remove(steamId);
        }
    }

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

    // --- Customization Management ---

    public bool GetPlayerGender(byte playerId)
    {
        return _playerGenders.TryGetValue(playerId, out bool isMale) ? isMale : true;
    }

    public ModelInfo GetPlayerModelInfo(byte playerId)
    {
        return _playerModelInfos.TryGetValue(playerId, out ModelInfo modelInfo) ? modelInfo : new ModelInfo();
    }

    public void UpdatePlayerCustomization(byte playerId, bool isMale, ModelInfo modelInfo)
    {
        if (_playerGenders.TryGetValue(playerId, out bool oldGender) && oldGender != isMale)
        {
            Debug.LogWarning($"Player {playerId} changed gender. Prefab swapping is not implemented yet. Re-spawning player might be required.");
            // Here you would need logic to destroy the old player object and spawn a new one with the correct prefab.
            // This is complex and deferred for now.
        }

        _playerGenders[playerId] = isMale;
        _playerModelInfos[playerId] = modelInfo;

        if (byteIdToSteamId.TryGetValue(playerId, out string steamId) && players.TryGetValue(steamId, out GameObject playerObject))
        {
            var modelCustom = playerObject.GetComponentInChildren<ModelCustom>();
            if (modelCustom != null)
            {
                modelCustom.ApplyModelInfo(modelInfo);
            }
        }
    }


    // Monster Management and State Updates remain largely the same
    #region Monster Management

    public void SpawnMonsterFromState(MonsterState state)
    {
        if (monsters.ContainsKey(state.monsterId)) return;
        GameObject prefab = SpawnManager.Instance.GetPrefab(state.monsterType);
        if (prefab == null) return;
        GameObject newMonsterGO = Instantiate(prefab, state.position, state.rotation);
        NetworkMonster networkMonster = newMonsterGO.GetComponent<NetworkMonster>();
        if (networkMonster == null) { Destroy(newMonsterGO); return; }
        networkMonster.Initialize(state.monsterId, state.monsterType);
        newMonsterGO.name = $"{prefab.name}_{state.monsterId}";
        monsters.Add(state.monsterId, newMonsterGO);
        var monsterMovement = newMonsterGO.GetComponent<IMonsterMovement>();
        if (monsterMovement is MonoBehaviour) (monsterMovement as MonoBehaviour).enabled = false;
        var monsterAI = newMonsterGO.GetComponent<MonsterAIController>();
        if (monsterAI != null) monsterAI.enabled = false;
    }

    public void DespawnMonster(ushort monsterId)
    {
        if (monsters.TryGetValue(monsterId, out GameObject monsterToDestroy))
        {
            SpawnManager.Instance.ReturnMonsterToPool(monsterToDestroy);
            monsters.Remove(monsterId);
        }
    }

    #endregion

    #region State Update Handlers

    public void UpdateFromGameState(NetworkGameState state)
    {
        foreach (var playerState in state.players)
        {
            if (!byteIdToSteamId.TryGetValue(playerState.playerId, out string steamId)) continue;
            if (players.TryGetValue(steamId, out GameObject playerObject))
            {
                if (steamId == NetworkManager.Instance.PlayerId) continue;
                var networkPlayer = playerObject.GetComponent<NetworkPlayer>();
                if (networkPlayer == null) continue;
                if (networkPlayer.BodyTransformSync != null) networkPlayer.BodyTransformSync.OnTransformReceived(playerState.position, playerState.rotation);
                if (networkPlayer.CameraTransformSync != null) networkPlayer.CameraTransformSync.OnTransformReceived(networkPlayer.CameraTransformSync.transform.position, playerState.cameraRotation);
                if (networkPlayer.AnimatorSync != null) networkPlayer.AnimatorSync.OnAnimationDataReceived(
                    playerState.moveX,
                    playerState.moveY,
                    AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Walk),
                    AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Sprint),
                    AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Roll),
                    AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.IsGrounded),
                    AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.Crouch),
                    AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.UnarmedAttackJab),
                    AnimationBitmask.IsSet(playerState.animationMask, AnimationBitmask.UnarmedAttackCross)
                    );
                if (networkPlayer.WeaponController != null && networkPlayer.WeaponController.activeID != playerState.weaponId) networkPlayer.WeaponController.ToChange(playerState.weaponId);

                var bodySlopeHandler = playerObject.GetComponentInChildren<BodySlope_Handler>();
                if (bodySlopeHandler != null)
                {
                    bodySlopeHandler.SetSlopeFromNetwork(playerState.bending);
                }

                // Apply customization updates
                ModelInfo currentModelInfo = GetPlayerModelInfo(playerState.playerId);
                if (currentModelInfo.head != playerState.modelInfo.head ||
                    currentModelInfo.body != playerState.modelInfo.body ||
                    currentModelInfo.acc1 != playerState.modelInfo.acc1 ||
                    currentModelInfo.acc2 != playerState.modelInfo.acc2)
                {
                    UpdatePlayerCustomization(playerState.playerId, playerState.isMale, playerState.modelInfo);
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
                monsterGO.transform.position = monsterState.position;
                monsterGO.transform.rotation = monsterState.rotation;
                var monsterHealth = monsterGO.GetComponent<MonsterHealth>();
                if (monsterHealth != null) monsterHealth.SetHealthFromNetwork(monsterState.currentHP, monsterState.maxHP);
                var monsterAnimSync = monsterGO.GetComponent<NetworkMonsterAnimatorSync>();
                if (monsterAnimSync != null)
                {
                    var animData = NetworkMonsterAnimatorSync.Deserialize(monsterState.animationData);
                    monsterAnimSync.OnAnimationDataReceived(animData);
                }
            }
        }
    }

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