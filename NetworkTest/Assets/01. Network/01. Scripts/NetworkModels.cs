using System;
using System.Collections.Generic;

[Serializable]
public class PlayerInfo
{
    public string steam_id; // The CSteamID as a string
    public string player_id; // The internal byte ID (0, 1, 2...) as a string
    public string nickname;
    public bool is_ready;
}

[Serializable]
public class RoomInfo
{
    public int room_id;
    public string room_name;
    public int player_count;
}

[Serializable]
public class FindRoomsResponse
{
    public List<RoomInfo> rooms;
    public static FindRoomsResponse FromJson(string json) => UnityEngine.JsonUtility.FromJson<FindRoomsResponse>(json);
}

[Serializable]
public class UpdateRoomInfoPayload
{
    public string type;
    public string room_name;
    public string host_id;
    public List<PlayerInfo> players;
    public static UpdateRoomInfoPayload FromJson(string json) => UnityEngine.JsonUtility.FromJson<UpdateRoomInfoPayload>(json);
}

[Serializable]
public class PlayerLeftPayload
{
    public string player_id;
    public static PlayerLeftPayload FromJson(string json) => UnityEngine.JsonUtility.FromJson<PlayerLeftPayload>(json);
}

[Serializable]
public class PlayerJoinedPayload
{
    public string player_id;
    public static PlayerJoinedPayload FromJson(string json) => UnityEngine.JsonUtility.FromJson<PlayerJoinedPayload>(json);
}

[Serializable]
public class ChatBroadcastPayload
{
    public string sender_id;
    public string message;
    public static ChatBroadcastPayload FromJson(string json) => UnityEngine.JsonUtility.FromJson<ChatBroadcastPayload>(json);
}
