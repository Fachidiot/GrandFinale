using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "NetworkTest/Game Settings", order = 0)]
public class GameSettings : ScriptableObject
{
    [Header("Scene Names")]
    public string tutorialScene = "TutorialScene";
    public string mainmenuScene = "MainMenuScene";
    public string spaceroomScene = "SpaceShipScene";
    [Tooltip("A list of all scenes where player prefabs should be spawned.")]
    public List<string> playableScenes = new List<string> { "SpaceShipScene", "TutorialScene" };

    [Header("Room Settings")]
    public string defaultRoomName = "Test Room";

    [Header("Control")]
    public string interactableLayer = "Interactable";

    [Header("SpawnPoint")]
    public Transform tutorialSpawnPoint;
    public Transform spacestationSpawnPoint;
}
