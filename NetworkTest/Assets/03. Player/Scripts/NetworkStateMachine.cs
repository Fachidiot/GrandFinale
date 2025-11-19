using UnityEngine;
using Newtonsoft.Json.Linq;

public class NetworkStateMachine : MonoBehaviour
{
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private EventsCenter eventsCenter;

    private bool isMine;

    public void Initialize(bool isLocalPlayer)
    {
        isMine = isLocalPlayer;
        
        // Only local players should send events
        if (isMine)
        {
            weaponController.OnShoot += ShootEventSender;
            eventsCenter.OnWeaponChange += WeaponChangeEventSender;
        }
    }

    private void OnDisable()
    {
        if (isMine)
        {
            weaponController.OnShoot -= ShootEventSender;
            eventsCenter.OnWeaponChange -= WeaponChangeEventSender;
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

    private void WeaponChangeEventSender(bool change)
    {
        if (!change || weaponController.nextID == 0)
            return;
        if (weaponController.IsProcessingRemoteWeaponChange)
            return;

        JObject parameters = new JObject(
            new JProperty("weapon_id", weaponController.nextID)
        );
        SendAction("weapon_change", parameters);
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
                    weaponController.RemoteShoot();
                }
                break;
            case "weapon_change":
                if (weaponController != null)
                {
                    int weaponId = eventData["weapon_id"].Value<int>();
                    weaponController.RemoteToChange(weaponId);
                }
                break;
        }
    }
}