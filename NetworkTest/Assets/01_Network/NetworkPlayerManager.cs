using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public class NetworkPlayerManager : MonoBehaviour
{
    public static NetworkPlayerManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject monsterPrefab; // To be assigned in the Inspector

    private Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    private Dictionary<ushort, GameObject> monsters = new Dictionary<ushort, GameObject>();

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

    #region Player Management

    // This function is now only for Lobby setup
    public void UpdatePlayerList(JArray playerList)
    {
        Debug.Log($"[NetworkPlayerManager] Updating player list. Current player count in dictionary: {players.Count}");
        List<string> playerIdsInMessage = playerList.Select(p => p["player_id"].ToString()).ToList();

        // Remove players that are no longer in the list
        List<string> currentPlayers = new List<string>(players.Keys);
        foreach (string playerId in currentPlayers)
        {
            if (!playerIdsInMessage.Contains(playerId))
            {
                Destroy(players[playerId]);
                players.Remove(playerId);
            }
        }

        // Add new players
        foreach (JObject playerInfo in playerList.Cast<JObject>())
        {
            string playerId = playerInfo["player_id"].ToString();
            string nickname = playerInfo["nickname"]?.ToString();

            if (!players.ContainsKey(playerId))
            {
                Debug.Log($"[NetworkPlayerManager] Player ID {playerId} is new. Spawning now.");
                SpawnPlayer(playerId, Vector3.zero, nickname);
            }
            else
            {
                Debug.Log($"[NetworkPlayerManager] Player ID {playerId} already exists in dictionary. Skipping spawn.");
            }
        }
    }

    private GameObject SpawnPlayer(string playerId, Vector3 position, string nickname)
    {
        if (playerPrefab == null) return null;

        GameObject playerObject = Instantiate(playerPrefab, position, Quaternion.identity);
        playerObject.name = $"Player_{playerId}";
        players.Add(playerId, playerObject);

        // Setup Nickname UI
        PlayerNicknameUI nicknameUI = playerObject.GetComponentInChildren<PlayerNicknameUI>();
        if (nicknameUI != null) nicknameUI.SetNickname(nickname);

        bool isMine = (playerId == NetworkManager.Instance.PlayerId);

        // Initialize Sync components
        var transformSyncs = playerObject.GetComponentsInChildren<NetworkTransformSync>();
        foreach (var view in transformSyncs) view.Initialize(playerId, isMine);

        var animSync = playerObject.GetComponentInChildren<NetworkAnimatorSync>();
        if (animSync != null) animSync.Initialize(playerId, isMine);

        // Disable components for remote players
        if (isMine)
        {
            if (nicknameUI != null) nicknameUI.gameObject.SetActive(false);

            // Initialize InGameUIManager for the local player
            var inGameUI = FindAnyObjectByType<InGameUIManager>();
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

    #endregion

    #region Game State Update (Optimized)

    public void UpdateFromGameState(NetworkGameState state)
    {
        // --- Update Players ---
        foreach (var playerState in state.players)
        {
            // TODO: The key for the dictionary should be the byte ID, not the string ID.
            // This will require a mapping from the initial string ID to a byte ID when a player joins a room.
            // For now, we will assume a temporary mapping or linear search.
            string playerId = playerState.playerId.ToString(); // This is a temporary conversion

            if (players.TryGetValue(playerId, out GameObject playerObject))
            {
                // Don't update the local player's state from the server, as the local player has authority over their own movement.
                if (playerId == NetworkManager.Instance.PlayerId && NetworkManager.Instance.Mode == NetworkMode.Client)
                    continue;

                // --- Handle Transform Sync ---
                var transformSyncs = playerObject.GetComponentsInChildren<NetworkTransformSync>();
                if (transformSyncs.Length > 0)
                {
                    // In the new model, we only receive one position and rotation for the body.
                    // The camera rotation is handled locally or via a separate mechanism if needed.
                    transformSyncs[0].OnTransformReceived(playerState.position, playerState.rotation);
                }

                // --- Handle Animator Sync ---
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

                // --- Handle Weapon State Sync ---
                var weaponController = playerObject.GetComponentInChildren<WeaponController>();
                if (weaponController != null && weaponController.activeID != playerState.weaponId)
                {
                    weaponController.ToChange(playerState.weaponId);
                }
            }
            else
            {
                // TODO: Player doesn't exist locally, maybe request info or spawn them.
            }
        }

        // --- Update Monsters ---
        foreach (var monsterState in state.monsters)
        {
            if (monsters.TryGetValue(monsterState.monsterId, out GameObject monsterObject))
            {
                // TODO: Add NetworkTransformSync and NetworkAnimatorSync to monster prefab
                // monsterObject.transform.position = monsterState.position;
                // monsterObject.transform.rotation = monsterState.rotation;
            }
            else
            {
                SpawnMonster(monsterState.monsterId, monsterState.position);
            }
        }
    }

    private void SpawnMonster(ushort monsterId, Vector3 position)
    {
        if (monsterPrefab == null) return;

        GameObject monsterObject = Instantiate(monsterPrefab, position, Quaternion.identity);
        monsterObject.name = $"Monster_{monsterId}";
        monsters.Add(monsterId, monsterObject);

        // TODO: Initialize monster-specific components if any
    }

    #endregion

    #region Legacy and Cleanup

    // This is the old method. It will be phased out.
    public void UpdatePlayersStateFromJson(JArray playersState)
    {
        // This logic is now replaced by UpdateFromGameState
    }

    public void RoutePlayerEvent(JObject eventData)
    {
        string playerId = eventData["player_id"]?.ToString();
        if (playerId == NetworkManager.Instance.PlayerId) return;

        if (players.TryGetValue(playerId, out GameObject playerObject))
        {
            Debug.Log($"{playerObject.name} : event invoke");
            var nsm = playerObject.GetComponentInChildren<NetworkStateMachine>();
            if (nsm != null) nsm.OnNetworkEvent(eventData);
        }
    }

    public void ClearPlayers()
    {
        foreach (var player in players.Values) Destroy(player);
        players.Clear();

        foreach (var monster in monsters.Values) Destroy(monster);
        monsters.Clear();
    }

    #endregion
}