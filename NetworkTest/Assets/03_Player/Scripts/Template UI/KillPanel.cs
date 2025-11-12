using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillPanel : MonoBehaviour
{
    [SerializeField] private GameObject killPanelItemPrefab;
    [SerializeField] Transform killPanelContent;

    public void CreateKillItemUI(string killerName, string deathName, string weaponName, bool headShoot, bool isMine)
    {
        GameObject kItem = Instantiate(killPanelItemPrefab, Vector3.zero, Quaternion.identity, killPanelContent);
        kItem.GetComponent<Image>().enabled = isMine;
        kItem.transform.GetChild(0).GetComponent<TMP_Text>().text = killerName;
        kItem.transform.GetChild(1).transform.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("UI/" + weaponName + "_UI");
        kItem.transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(headShoot);
        kItem.transform.GetChild(2).GetComponent<TMP_Text>().text = deathName;
        Destroy(kItem, 6f);
    }
}
