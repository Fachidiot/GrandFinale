using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "NetworkTest/Game Settings", order = 0)]
public class GameSettings : ScriptableObject
{
    [Header("Scene Names")]
    public string connectionScene = "ConnectionScene";
    public string roomScene = "RoomScene";

    [Header("Room Settings")]
    public string defaultRoomName = "Test Room";
}
