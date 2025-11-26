using System.Collections;
using System.Collections.Generic;
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

    private bool isDoorOpen = false;
    private bool isLanded = false;

    // private Animator animator;

    // void Start()
    // {
    //     animator = GetComponent<Animator>();
    // }

    public void Landing()
    {
        if (isLanded)
            return;

        isLanded = true;
        LegAnimation(AnimShipLandHash, isLanded);
        WingAnimation(AnimShipLandHash, isLanded);
        ReactorAnimation(AnimShipLandHash, isLanded);
        Debug.Log("Lading...");
        // animator.SetBool(AnimShipLandHash, isLanded);
    }

    public void Launching()
    {
        if (!isLanded)
            return;

        isLanded = false;
        LegAnimation(AnimShipLandHash, isLanded);
        WingAnimation(AnimShipLandHash, isLanded);
        ReactorAnimation(AnimShipLandHash, isLanded);
        Debug.Log("Launching...");
        // animator.SetBool(AnimShipLandHash, isLanded);
    }

    public void OpenDoor()
    {
        if (isDoorOpen)
            return;

        isDoorOpen = true;
        doorAnimator.SetBool(AnimDoorOpenHash, isDoorOpen);
        Debug.Log("Openning...");
        // animator.SetBool(AnimDoorOpenHash, isDoorOpen);
    }

    public void CloseDoor()
    {
        if (!isDoorOpen)
            return;

        isDoorOpen = false;
        doorAnimator.SetBool(AnimDoorOpenHash, isDoorOpen);
        Debug.Log("Closing...");
        // animator.SetBool(AnimDoorOpenHash, isDoorOpen);
    }

    public void OnLaunchGameClicked()
    {
        // if (NetworkManager.Instance.Mode == NetworkMode.Client)
        //     return;
        if (ServerRoomManager.Instance.SelectedPlanetId == -1)
        {
            Debug.LogWarning("[RoomUIManager] Cannot launch, no planet selected.");
            return;
        }

        Debug.Log($"[RoomUIManager] Host clicked launch for planet {ServerRoomManager.Instance.SelectedPlanetId}.");
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
