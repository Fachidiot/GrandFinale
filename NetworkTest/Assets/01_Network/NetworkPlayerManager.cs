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
    public GameObject monsterPrefab;

    private Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    private Dictionary<ushort, GameObject> monsters = new Dictionary<ushort, GameObject>();

    public Dictionary<string, GameObject> Players => players;

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
        List<string> steamIdsInMessage = playerList.Select(p => p["steam_id"].ToString()).ToList();

        List<string> currentPlayers = new List<string>(players.Keys);
        foreach (string steamId in currentPlayers)
        {
            if (!steamIdsInMessage.Contains(steamId))
            {
                Destroy(players[steamId]);
                players.Remove(steamId);
            }
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
        if (playerPrefab == null) return null;

        GameObject playerObject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerObject.name = $"Player_{playerInfo.nickname}";
        players.Add(playerInfo.steam_id, playerObject);

        PlayerNicknameUI nicknameUI = playerObject.GetComponentInChildren<PlayerNicknameUI>();
        if (nicknameUI != null) nicknameUI.SetNickname(playerInfo.nickname);

        bool isMine = (playerInfo.steam_id == NetworkManager.Instance.PlayerId);

        var transformSyncs = playerObject.GetComponentsInChildren<NetworkTransformSync>();
        foreach (var view in transformSyncs) view.Initialize(playerInfo.steam_id, isMine);

        var animSync = playerObject.GetComponentInChildren<NetworkAnimatorSync>();
        if (animSync != null) animSync.Initialize(playerInfo.steam_id, isMine);
        
        var nsm = playerObject.GetComponentInChildren<NetworkStateMachine>();
        if (nsm != null) nsm.Initialize(isMine);

        if (isMine)
        {
            if (nicknameUI != null) nicknameUI.gameObject.SetActive(false);
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

    #endregion

    #region Game State Update

    public void UpdateFromGameState(NetworkGameState state)
    {
        foreach (var playerState in state.players)
        {
            string steamId = ServerRoomManager.Instance.GetSteamId(playerState.playerId);
            if (string.IsNullOrEmpty(steamId)) continue;

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

    public void ClearPlayers()
    {
        foreach (var player in players.Values) Destroy(player);
        players.Clear();

        foreach (var monster in monsters.Values) Destroy(monster);
        monsters.Clear();
    }
}
