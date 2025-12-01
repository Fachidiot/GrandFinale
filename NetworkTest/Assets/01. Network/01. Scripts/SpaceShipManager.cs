using System.Collections;
using System.Collections.Generic;
using FIMSpace;
using FIMSpace.Generating.Planning.ModNodes.Transforming;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine;

public class SpaceShipManager : MonoBehaviour
{
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

    void Update()
    {
        if (!IsDoorOpen && null != GameObject.FindWithTag("Player"))
        {
            OpenDoor();
            Landing();
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
        Debug.Log("Lading...");
    }

    public void Launching()
    {
        if (!IsLanded)
            return;

        IsLanded = false;
        LegAnimation(AnimShipLandHash, IsLanded);
        WingAnimation(AnimShipLandHash, IsLanded);
        ReactorAnimation(AnimShipLandHash, IsLanded);
        Debug.Log("Launching...");
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
        Debug.Log("Openning...");
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
        Debug.Log("Closing...");
    }

    public void ToggleDoor()
    {
        if (!IsDoorOpen)
            OpenDoor();
        else
            CloseDoor();
    }

    public void OnLaunchGameClicked()
    {
        Debug.Log($"[RoomUIManager] Clicked launch for planet {ServerRoomManager.Instance.SelectedPlanetId}.");
        ServerRoomManager.Instance.LaunchToPlanet(ServerRoomManager.Instance.SelectedPlanetId);
    }

    public void OnBackToSpaceClicked()
    {
        ServerRoomManager.Instance.LaunchToPlanet(-1);
    }

    public void OnInviteFriends()
    {
        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.Instance.OnInviteFriendsButtonClicked();
        }
    }
}
