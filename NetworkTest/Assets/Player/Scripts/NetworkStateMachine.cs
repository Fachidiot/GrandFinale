using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class NetworkStateMachine : MonoBehaviour
{
    // [SerializeField] private PhotonView photonView;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private BodySlope bodySlope;
    [SerializeField] private BodySlope_Handler bodySlope_Handler;
    [SerializeField] private BodyTurnHandler bodyTurnHandler;
    // [SerializeField] private PlayerHealth playerHealth;
    // [SerializeField] private PlayerLifeController playerLifeController;
    [SerializeField] private EventsCenter eventsCenter;
    bool weapSyncInStart = false;

    private void OnEnable()
    {
        weaponController.OnShoot += ShootEventSender;
        eventsCenter.OnWeaponChange += WeaponChangeEventSender;
    }
    private void OnDisable()
    {
        weaponController.OnShoot -= ShootEventSender;
        eventsCenter.OnWeaponChange -= WeaponChangeEventSender;
    }

    private void ShootEventSender()
    {
        var shootAction = new
        { 
            type = "player_event", 
            action = "shoot"
        };
        string jsonMessage = Newtonsoft.Json.JsonConvert.SerializeObject(shootAction);
        NetworkManager.Instance.SendMessageToServer(jsonMessage);
    }

    private void WeaponChangeEventSender(bool change)
    {
        if (!change)
            return;

        var weaponChangeAction = new 
        {
            type = "player_event", 
            action = "weapon_change",
            weapon_id = weaponController.nextID
        };
        string jsonMessage = Newtonsoft.Json.JsonConvert.SerializeObject(weaponChangeAction);
        NetworkManager.Instance.SendMessageToServer(jsonMessage);
    }

    // [PunRPC]
    public void DamageRPC(float damage, int photonViewID, bool hitOnTheHead, string weaponName)
    {
        Debug.Log("damage ebat");
        // float health = playerHealth.SetDamage(damage);

        // if (health <= 0)
        // {
        //     var killerPV = PhotonView.Find(photonViewID);

        //     bool isMine = killerPV.IsMine | photonView.IsMine;

        //     UIManger.instance.killPanel.CreateKillItemUI(killerPV.Owner.NickName, photonView.Owner.NickName, weaponName, hitOnTheHead, isMine);
        // }
    }

    // [PunRPC]
    public void RespawnRPC()
    {
        // playerLifeController.Respawn();
    }

    void SyncActiveWeaponInStart(int activeWeap)
    {
        weapSyncInStart = true;
        weaponController.animator.Play("GunPickUp", 1);
    }

    // Called by GameManager to process events received from the server
    public void OnNetworkEvent(Newtonsoft.Json.Linq.JObject eventData)
    {
        string eventName = eventData["event"]?.ToString();

        switch (eventName)
        {
            case "shoot":
                if (weaponController != null)
                {
                    weaponController.StartShoot();
                }
                break;
            case "weapon_change":
                if (weaponController != null) 
                {
                    int weaponId = eventData["weapon_id"].Value<int>();
                    Debug.Log(weaponId);
                    weaponController.ToChange(weaponId);
                }
                break;
        }
    }
}
