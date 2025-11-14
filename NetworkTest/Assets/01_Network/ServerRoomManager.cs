using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class ServerRoomManager : MonoBehaviour
{
    public static ServerRoomManager Instance { get; private set; }

    // This list is the single source of truth for all players in the room.
    // It's updated by the host and synchronized to clients.
    public List<PlayerInfo> PlayerList { get; private set; } = new List<PlayerInfo>();
    public int SelectedPlanetId { get; private set; } = -1;
    public string RoomName { get; private set; } = "Space Crew"; // Default name
    public string HostId { get; private set; }

    // Event for UI to subscribe to.
    public static event Action OnRoomDataUpdated;

    // Host-only data
    private Dictionary<CSteamID, PlayerInfo> playersInRoom = new Dictionary<CSteamID, PlayerInfo>();
    private Dictionary<CSteamID, byte> steamIdToByteId = new Dictionary<CSteamID, byte>();
    private Dictionary<byte, CSteamID> byteIdToSteamId = new Dictionary<byte, CSteamID>();
    private byte nextPlayerId = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Register message handlers based on network mode
            if (NetworkManager.Instance.Mode == NetworkMode.Host)
            {
                NetworkManager.OnJsonMessageReceived += HandleHostJsonMessage;
            }
            else // Client
            {
                NetworkManager.OnJsonMessageReceived += HandleServerJsonMessage;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // This logic is now centralized here instead of RoomUIManager
        if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            AddHostPlayer(NetworkManager.Instance.selfSteamId, CustomSteamManager.Instance.PlayerName);
        }
        else if (NetworkManager.Instance.Mode == NetworkMode.Client)
        {
            SendNickname();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            if (NetworkManager.Instance != null)
            {
                // Unregister all handlers
                NetworkManager.OnJsonMessageReceived -= HandleHostJsonMessage;
                NetworkManager.OnJsonMessageReceived -= HandleServerJsonMessage;
            }
        }
    }

    #region Host-Only Logic

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
        int planetId = data["planet_id"]?.ToObject<int>() ?? -1;
        if (planetId != -1)
        {
            SelectPlanet(planetId);
            Debug.Log($"[ServerRoomManager] Host received planet proposal from {sender}. Selected planet ID: {planetId}");
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

        StartCoroutine(DelayedBroadcast());
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

    public void SelectPlanet(int planetId)
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;
        SelectedPlanetId = planetId;
        Debug.Log($"[ServerRoomManager] Host authoritatively selected planet ID: {planetId}");
        BroadcastRoomUpdate();
    }

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

        // Broadcast to clients
        JObject message = new JObject { { "type", "load_scene" }, { "scene_name", sceneToLoad } };
        NetworkManager.Instance.BroadcastJsonMessage(message);

        // Host loads the scene directly
        Debug.Log($"[ServerRoomManager] Host is loading scene: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }

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
        
        // Also update host's local data directly
        UpdateLocalRoomData(roomInfo);
    }

    IEnumerator DelayedBroadcast()
    {
        yield return new WaitForEndOfFrame();
        BroadcastRoomUpdate();
    }

    #endregion

    #region Client & Host Logic

    private void HandleServerJsonMessage(CSteamID sender, string jsonMsg)
    {
        if (NetworkManager.Instance.Mode == NetworkMode.Host) return; // Host handles messages differently

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

    private void UpdateLocalRoomData(JObject data)
    {
        RoomName = data["room_name"]?.ToString() ?? RoomName;
        HostId = data["host_id"]?.ToString() ?? HostId;
        SelectedPlanetId = data["selected_planet_id"]?.ToObject<int>() ?? -1;

        JArray players = data["players"] as JArray;
        if (players != null)
        {
            PlayerList = players.ToObject<List<PlayerInfo>>();
            
            // This is the new central point for updating other managers
            if (NetworkPlayerManager.Instance != null)
            {
                NetworkPlayerManager.Instance.UpdatePlayerList(players);
            }
        }

        Debug.Log($"[ServerRoomManager] Local room data updated. Players: {PlayerList.Count}, Planet: {SelectedPlanetId}");
        OnRoomDataUpdated?.Invoke();
    }

    private void SendNickname()
    {
        string nickname = CustomSteamManager.Instance.PlayerName;
        JObject msg = new JObject { { "type", "set_nickname" }, { "nickname", nickname } };
        CSteamID hostId = SteamMatchmaking.GetLobbyOwner(NetworkManager.Instance.CurrentLobbyID);
        NetworkManager.Instance.SendJsonMessage(hostId, msg);
    }

    public void ClearRoom()
    {
        // Host data
        playersInRoom.Clear();
        steamIdToByteId.Clear();
        byteIdToSteamId.Clear();
        nextPlayerId = 0;

        // Shared data
        PlayerList.Clear();
        SelectedPlanetId = -1;
        OnRoomDataUpdated?.Invoke();
    }

    public void OnInviteFriendsButtonClicked()
    {
        if (NetworkManager.Instance.CurrentLobbyID.IsValid())
        {
            SteamFriends.ActivateGameOverlayInviteDialog(NetworkManager.Instance.CurrentLobbyID);
        }
    }

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
