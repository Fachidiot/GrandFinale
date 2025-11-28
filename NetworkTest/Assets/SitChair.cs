using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChairAction : MonoBehaviour, IAction
{
    public Vector3 offset;
    private bool isSit = false;
    private GameObject sittedPlayer;

    public bool Value => isSit;

    public void PlayerSit(GameObject player)
    {
        if (!isSit)
        {
            isSit = true;
            sittedPlayer = player;
            sittedPlayer.GetComponentInChildren<Animator>().SetBool("sit", true);

            if (GameManager.Instance)
                GameManager.Instance.GetComponent<PlayerInputs>().SetActionState(this);
            player.GetComponent<CharacterController>().enabled = false;
            player.transform.position = transform.position + offset;
        }
        else
        {
            ReleaseAction();
        }
    }

    public void ReleaseAction()
    {
        sittedPlayer.GetComponentInChildren<Animator>().SetBool("sit", false);
        sittedPlayer.GetComponent<CharacterController>().enabled = true;
        isSit = false;
        sittedPlayer = null;
    }
}
