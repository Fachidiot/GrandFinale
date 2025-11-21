using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "NetworkTest/Game Settings", order = 0)]
public class GameSettings : ScriptableObject
{
    [Header("Scene Names")]
    public string mainmenuScene = "MainMenuScene";
    public string spaceroomScene = "SpaceShipScene";

    [Header("Room Settings")]
    public string defaultRoomName = "Test Room";

    [Header("Control")]
    public string interactableLayer = "Interactable";

    [Header("SpawnPoint")]
    public Transform spacestationSpawnPoint;
}
