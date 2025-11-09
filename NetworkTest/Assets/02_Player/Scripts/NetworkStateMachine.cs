using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using TMPro;

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
            type = "player_action",
            action = "shoot",
            player_id = NetworkManager.Instance.PlayerId
        };
        string jsonMessage = Newtonsoft.Json.JsonConvert.SerializeObject(shootAction, Newtonsoft.Json.Formatting.None);
        NetworkManager.Instance.SendTCPMessage(jsonMessage);
    }

    private void WeaponChangeEventSender(bool change)
    {
        if (!change || weaponController.nextID == 0)
            return;
        if (weaponController.IsProcessingRemoteWeaponChange) // 새로운 조건 추가
            return;

        var weaponChangeAction = new
        {
            type = "player_action",
            action = "weapon_change",
            weapon_id = weaponController.nextID,
            player_id = NetworkManager.Instance.PlayerId
        };
        string jsonMessage = Newtonsoft.Json.JsonConvert.SerializeObject(weaponChangeAction, Newtonsoft.Json.Formatting.None);
        NetworkManager.Instance.SendTCPMessage(jsonMessage);
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

    // Called by GameManager to process events received from the server
    public void OnNetworkEvent(Newtonsoft.Json.Linq.JObject eventData)
    {
        string eventName = eventData["action"]?.ToString();

        switch (eventName)
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
                    Debug.Log(weaponId);
                    weaponController.RemoteToChange(weaponId); // RemoteToChange 호출
                }
                break;
        }
    }
}
