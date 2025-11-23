using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine;

public class SpaceShipManager : MonoBehaviour
{

    // This method is called by the "Launch" button
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
