using System;
using System.Collections.Generic;

// 이 파일은 서버와 클라이언트 간에 교환되는 JSON 데이터 구조를 정의합니다.
// JsonUtility를 사용하기 위해 모든 클래스와 필드는 public이어야 합니다.

[Serializable]
public class PlayerInfo
{
    public string player_id;
    public string nickname;
    public bool is_ready;
}

[Serializable]
public class RoomInfo
{
    public int room_id;
    public string room_name;
    public int player_count; // find_rooms_response에서 사용
}

// 서버 응답을 위한 래퍼 클래스들

[Serializable]
public class FindRoomsResponse
{
    public List<RoomInfo> rooms;

    public static FindRoomsResponse FromJson(string json)
    {
        return UnityEngine.JsonUtility.FromJson<FindRoomsResponse>(json);
    }
}

[Serializable]
public class UpdateRoomInfoPayload
{
    public string room_name;
    public string host_id;
    public List<PlayerInfo> players;

    public static UpdateRoomInfoPayload FromJson(string json)
    {
        return UnityEngine.JsonUtility.FromJson<UpdateRoomInfoPayload>(json);
    }
}

[Serializable]
public class PlayerLeftPayload
{
    public string player_id;

    public static PlayerLeftPayload FromJson(string json)
    {
        return UnityEngine.JsonUtility.FromJson<PlayerLeftPayload>(json);
    }
}

[Serializable]
public class PlayerJoinedPayload
{
    public string player_id;

    public static PlayerJoinedPayload FromJson(string json)
    {
        return UnityEngine.JsonUtility.FromJson<PlayerJoinedPayload>(json);
    }
}

[Serializable]
public class ChatBroadcastPayload
{
    public string sender_id;
    public string message;

    public static ChatBroadcastPayload FromJson(string json)
    {
        return UnityEngine.JsonUtility.FromJson<ChatBroadcastPayload>(json);
    }
}
