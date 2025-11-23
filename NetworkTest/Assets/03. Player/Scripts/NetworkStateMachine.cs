using UnityEngine;
using Newtonsoft.Json.Linq;

public class NetworkStateMachine : MonoBehaviour
{
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private EventsCenter eventsCenter; // Still needed for other events, if any

    private bool isMine;

    public void Initialize(bool isLocalPlayer)
    {
        isMine = isLocalPlayer;
        
        // Only local players should send events
        if (isMine)
        {
            weaponController.OnShoot += ShootEventSender;
            // eventsCenter.OnWeaponChange += WeaponChangeEventSender; // No longer used for sending weapon change
            weaponController.OnWeaponEquipped += WeaponEquippedEventSender; // New event
        }
    }

    private void OnDisable()
    {
        if (isMine)
        {
            weaponController.OnShoot -= ShootEventSender;
            // eventsCenter.OnWeaponChange -= WeaponChangeEventSender;
            weaponController.OnWeaponEquipped -= WeaponEquippedEventSender;
        }
    }

    private void SendAction(string actionName, JObject parameters = null)
    {
        if (!isMine) return;

        JObject actionData = new JObject(
            new JProperty("type", "player_action"),
            new JProperty("action", actionName)
        );

        if (parameters != null)
        {
            actionData.Merge(parameters);
        }

        NetworkManager.Instance.BroadcastJsonMessage(actionData);
    }

    private void ShootEventSender()
    {
        SendAction("shoot");
    }

    // New event sender for weapon equipped
    private void WeaponEquippedEventSender(int newSlotIndex)
    {
        // Don't send if it's the unarmed slot (0)
        if (newSlotIndex == 0) return;

        // Ensure this is not a remote change being processed locally
        if (weaponController.IsProcessingRemoteWeaponChange)
            return;

        JObject parameters = new JObject(
            new JProperty("weapon_id", newSlotIndex)
        );
        SendAction("weapon_equipped", parameters);
    }

    public void OnNetworkEvent(JObject eventData)
    {
        // Don't process events sent by ourselves
        if (isMine) return;

        string action = eventData["action"]?.ToString();

        switch (action)
        {
            case "shoot":
                if (weaponController != null)
                {
                    weaponController.RemoteAttack(); // Updated call
                }
                break;
            case "weapon_equipped": // Updated event type
                if (weaponController != null)
                {
                    int weaponId = eventData["weapon_id"].Value<int>();
                    weaponController.BeginRemoteWeaponChange(); // Set flag to prevent local loop
                    weaponController.EquipWeapon(weaponId); // Updated call
                    // Flag reset should be handled by WeaponController via Coroutine or Animation Event
                }
                break;
        }
    }
}