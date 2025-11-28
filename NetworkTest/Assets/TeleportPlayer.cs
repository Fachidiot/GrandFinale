using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class TeleportPlayer : MonoBehaviour
{
    [SerializeField] private Transform entryPos;
    [SerializeField] private Transform outryPos;

    public void OnEntry(GameObject localPlayer)
    {
        Debug.Log("OnEntry");
        localPlayer.transform.position = entryPos.position;
    }

    public void OnOutry(GameObject localPlayer)
    {
        Debug.Log("OnOutry");
        localPlayer.transform.position = outryPos.position;
    }
}
