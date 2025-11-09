using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Steamworks;

public class ServerRoomManager : MonoBehaviour
{
    public static ServerRoomManager Instance { get; private set; }

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
        NetworkManager.OnJsonMessageReceived -= HandleClientJsonMessage;
    }

    private void HandleClientJsonMessage(CSteamID sender, string jsonMsg)
    {
        JObject msg = JObject.Parse(jsonMsg);
        string type = msg["type"]?.ToString();

        if (type == "set_nickname")
        {
            HandleSetNickname(sender, msg);
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

    public void AddHostPlayer(CSteamID hostSteamId, string nickname)
    {
        if (playersInRoom.ContainsKey(hostSteamId)) return;
        AddPlayer(hostSteamId, nickname);
    }

    private void AddPlayer(CSteamID steamId, string nickname)
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

        Debug.Log($"[ServerRoomManager] Player {nickname} ({steamId}) joined as ID {newId}");

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

    public void BroadcastRoomUpdate()
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;

        JObject roomInfo = new JObject
        {
            { "type", "update_room_info" },
            { "room_name", "Test Room" },
            { "host_id", steamIdToByteId[NetworkManager.Instance.selfSteamId].ToString() }
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