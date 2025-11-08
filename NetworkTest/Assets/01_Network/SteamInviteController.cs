using UnityEngine;

public class SteamInviteController : MonoBehaviour
{
    PlayerInputs playerInputs;

    void Start()
    {
        playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerInputs.GetInteract())
                OnInviteFriends();
        }
    }

    public void OnInviteFriends()
    {
        // Use the static Instance to call the method
        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.Instance.OnInviteFriendsButtonClicked();
            Debug.Log("Steam Invite Friends Overlay Opened.");
        }
        else
        {
            Debug.LogError("ServerRoomManager.Instance is not found!");
        }
    }
}
