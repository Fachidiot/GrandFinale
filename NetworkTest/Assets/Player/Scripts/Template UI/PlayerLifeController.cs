using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLifeController : MonoBehaviour
{
    [SerializeField] private GameObject playerRagdollObject;
    [SerializeField] private HitBoxColidersList hitBoxColidersList;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private InputHandler input_Handler;
    [SerializeField] private CharacterMove characterMove;
    public List<SkinnedMeshRenderer> disableRenderComponentsOnDeath = new List<SkinnedMeshRenderer>();
    public List<GameObject> disableGameObjectsOnDeath = new List<GameObject>();

    // Start is called before the first frame update
    public void Die()
    {
        SpawnRagdollCopy();

        hitBoxColidersList.HitboxesAsTriggers(true);
        characterController.enabled = false;

        input_Handler.enabled = false;
        characterMove.enabled = false;
        StartCoroutine(SayRespawn());
        // if (transform.root.GetComponent<NetworkTransformSync>().IsMine)
        // {
        //     input_Handler.enabled = false;
        //     characterMove.enabled = false;

        //     StartCoroutine(SayRespawn());
        // }

        foreach (var comp in disableRenderComponentsOnDeath)
        {
            comp.enabled = false;
        }

        foreach (var go in disableGameObjectsOnDeath)
        {
            go.SetActive(false);
        }
    }

    IEnumerator SayRespawn()
    {
        // UIManger.instance.respawnPanel.respawnPanelObject.SetActive(true);
        float t = 0;
        while (t < 3)
        {
            float value = (float)System.Math.Round(3 - t, 2);
            // UIManger.instance.respawnPanel.SetRespawnTimeText(value);
            t += Time.deltaTime;
            yield return null;
        }

        // transform.root.GetComponent<PhotonView>().RPC("RespawnRPC", RpcTarget.All);
        yield break;
    }

    public void Respawn()
    {
        transform.root.position = SpawnPointsController.instance.GetRandomSpawnPoints();

        characterController.enabled = true;

        foreach (var comp in disableRenderComponentsOnDeath)
        {
            comp.enabled = true;
        }

        foreach (var go in disableGameObjectsOnDeath)
        {
            go.SetActive(true);
        }

        hitBoxColidersList.HitboxesAsTriggers(false);

        input_Handler.enabled = true;
        characterMove.enabled = true;
        characterMove.enabled = true;
        // if (transform.root.GetComponent<NetworkTransformSync>().IsMine)
        // {
        //     input_Handler.enabled = true;
        //     characterMove.enabled = true;
        //     characterMove.enabled = true;

        //     // UIManger.instance.respawnPanel.respawnPanelObject.SetActive(false);
        //     // UIManger.instance.healthPanel.SetHealthValue(100f);
        // }
    }

    private void SpawnRagdollCopy()
    {
        var playerGO = Instantiate(playerRagdollObject, playerRagdollObject.transform.position, playerRagdollObject.transform.rotation);
        Destroy(playerGO.GetComponent<NetworkAnimatorSync>());
        Destroy(playerGO.GetComponent<Animator>());
        playerGO.GetComponent<RigBase>().rigActive = false;
        playerGO.GetComponent<HitBoxColidersList>().Activate();

        var slots = playerGO.GetComponentsInChildren<SlotController>();

        foreach (var slot in slots)
        {
            Transform weapon = slot.transform.GetChild(0);
            weapon.parent = null;
            weapon.GetComponent<Collider>().enabled = true;
            weapon.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
