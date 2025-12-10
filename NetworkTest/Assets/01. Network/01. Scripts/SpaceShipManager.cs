using System.Collections;
using System.Collections.Generic;
using FIMSpace.Generating.Planning.ModNodes.Transforming;
using Newtonsoft.Json.Linq;
using Steamworks;
using Unity.VisualScripting;
using UnityEngine;

public class SpaceShipManager : MonoBehaviour
{
    [SerializeField] private bool initLanding;
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private List<Animator> legAnimator;
    private void LegAnimation(string hash, bool value)
    {
        foreach (var anim in legAnimator)
            anim.SetBool(hash, value);
    }
    [SerializeField] private List<Animator> wingAnimator;
    private void WingAnimation(string hash, bool value)
    {
        foreach (var anim in wingAnimator)
            anim.SetBool(hash, value);
    }
    [SerializeField] private List<Animator> reactorAnimator;
    private void ReactorAnimation(string hash, bool value)
    {
        foreach (var anim in reactorAnimator)
            anim.SetBool(hash, value);
    }

    private readonly string AnimDoorOpenHash = "isOpened";
    private readonly string AnimShipLandHash = "isLanded";

    public bool IsDoorOpen { get; private set; } = false;
    public bool IsLanded { get; private set; } = false;

    private int CurrentScenePlanetId
    {
        get
        {
            if (GameManager.Instance == null || GameManager.Instance.PlanetDatabase == null) return -1;
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            PlanetData planet = GameManager.Instance.PlanetDatabase.allPlanets.Find(p => p.sceneName == currentSceneName);
            return planet != null ? planet.planetId : -1;
        }
    }

    private void Start()
    {
        if (initLanding)
        {
            Landing();
            OpenDoor();
        }
        else
        {
            Launching();
            CloseDoor();
        }
    }

    public void UpdateStateFromNetwork(bool landed, bool doorOpen)
    {
        if (landed != IsLanded)
        {
            if (landed) Landing(); else Launching();
        }
        if (doorOpen != IsDoorOpen)
        {
            if (doorOpen) OpenDoor(); else CloseDoor();
        }
    }

    public void Landing()
    {
        if (IsLanded)
            return;

        IsLanded = true;
        LegAnimation(AnimShipLandHash, IsLanded);
        WingAnimation(AnimShipLandHash, IsLanded);
        ReactorAnimation(AnimShipLandHash, IsLanded);
        Debug.Log("[SpaceShipManager] Landing...");
    }

    public void Launching()
    {
        if (!IsLanded)
            return;

        IsLanded = false;
        LegAnimation(AnimShipLandHash, IsLanded);
        WingAnimation(AnimShipLandHash, IsLanded);
        ReactorAnimation(AnimShipLandHash, IsLanded);
        Debug.Log("[SpaceShipManager] Launching...");
    }

    private float cooltime = 0f;
    public void OpenDoor()
    {
        if (IsDoorOpen)
            return;
        if (4f + cooltime > Time.time)
            return;

        cooltime = Time.time;
        IsDoorOpen = true;
        doorAnimator.SetBool(AnimDoorOpenHash, IsDoorOpen);
        Debug.Log("[SpaceShipManager] Opening...");
    }

    public void CloseDoor()
    {
        if (!IsDoorOpen)
            return;
        if (4f + cooltime > Time.time)
            return;

        cooltime = Time.time;
        IsDoorOpen = false;
        doorAnimator.SetBool(AnimDoorOpenHash, IsDoorOpen);
        Debug.Log("[SpaceShipManager] Closing...");
    }

    public void ToggleDoor()
    {
        if (!IsDoorOpen)
            OpenDoor();
        else
            CloseDoor();
    }

    public void OnBackToSpaceClicked()
    {
        ServerRoomManager.Instance.LaunchToPlanet(-1);
    }

    public void OnLaunchGameClicked()
    {
        // --- New logic for preventing unnecessary scene loading ---
        if (CurrentScenePlanetId != -1 && ServerRoomManager.Instance.SelectedPlanetId == CurrentScenePlanetId)
        {
            Debug.Log($"[SpaceShipManager] Already on planet ID {ServerRoomManager.Instance.SelectedPlanetId}. Not reloading scene.");
            return;
        }
        // --------------------------------------------------------

        Debug.Log($"[SpaceShipManager] Host clicked launch for planet ID {ServerRoomManager.Instance.SelectedPlanetId}.");
        ServerRoomManager.Instance.LaunchToPlanet(ServerRoomManager.Instance.SelectedPlanetId);
    }

    public void OnInviteFriends()
    {
        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.Instance.OnInviteFriendsButtonClicked();
        }
    }
}
