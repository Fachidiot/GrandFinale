using System.Collections.Generic;
using UnityEngine;

public class SinglePlayer : MonoBehaviour, IPlayerControllable
{
    [SerializeField] private GameObject playerRagdollObject;
    public List<SkinnedMeshRenderer> disableRenderComponentsOnDeath = new List<SkinnedMeshRenderer>();
    public List<GameObject> disableGameObjectsOnDeath = new List<GameObject>();

    // IPlayerControllable implementation
    public bool IsMine => true; // Single player is always the local player
    public new GameObject gameObject => base.gameObject;
    public new Transform transform => base.transform;

    private HitBoxColidersList hitBoxColidersList;
    private CharacterController characterController;
    private InputHandler input_Handler;
    private CharacterMove characterMove;

    void Start()
    {
        hitBoxColidersList = GetComponentInChildren<HitBoxColidersList>();
        characterController = GetComponent<CharacterController>();
        input_Handler = GetComponentInChildren<InputHandler>();
        characterMove = GetComponent<CharacterMove>();
    }

    public void Die()
    {
        SpawnRagdollCopy();

        hitBoxColidersList.HitboxesAsTriggers(true);
        characterController.enabled = false;

        input_Handler.enabled = false;
        characterMove.enabled = false;
        // StartCoroutine(SayRespawn());
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


    public new T GetComponent<T>()
    {
        return base.GetComponent<T>();
    }

    public new T GetComponentInChildren<T>()
    {
        return base.GetComponentInChildren<T>();
    }

    public void Initialize()
    {
        if (IsMine)
            UIEvents.PlayerInitialized(GetComponent<WeaponController>(), GetComponent<PlayerStats>());
    }

    private void SpawnRagdollCopy()
    {
        var playerGO = Instantiate(playerRagdollObject, gameObject.transform.position, gameObject.transform.rotation);
        Destroy(playerGO.GetComponent<NetworkAnimatorSync>());
        Destroy(playerGO.GetComponent<Animator>());
        playerGO.GetComponent<RigBase>().rigActive = false;
        playerGO.GetComponent<HitBoxColidersList>().Activate();

        var slots = playerGO.GetComponentsInChildren<SlotController>();

        foreach (var slot in slots)
        {
            if (!slot.slotActive)
                continue;
            Transform weapon = slot.transform.GetChild(0);
            weapon.parent = null;
            weapon.GetComponent<Collider>().enabled = true;
            weapon.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
