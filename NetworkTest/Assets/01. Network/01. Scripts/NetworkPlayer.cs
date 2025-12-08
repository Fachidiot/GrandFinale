using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class NetworkPlayer : MonoBehaviour, IPlayerControllable
{
    [SerializeField] private GameObject playerRagdollObject;
    public List<SkinnedMeshRenderer> disableRenderComponentsOnDeath = new List<SkinnedMeshRenderer>();
    public List<GameObject> disableGameObjectsOnDeath = new List<GameObject>();

    [Header("Component References")]
    public NetworkTransformSync BodyTransformSync;
    public NetworkTransformSync CameraTransformSync;
    public NetworkAnimatorSync AnimatorSync;
    public WeaponController WeaponController;
    public CharacterMove CharacterMove;
    public InputHandler InputHandler;
    public CameraController CameraController;
    public CameraSwitcher CameraSwitcher;
    public PlayerNicknameUI NicknameUI;
    public NetworkStateMachine StateMachine;
    private HitBoxColidersList hitBoxColidersList;
    private CharacterController characterController;

    public bool IsMine { get; private set; }

    public void Awake()
    {
        // Attempt to automatically find components if not assigned in Inspector
        if (BodyTransformSync == null || CameraTransformSync == null)
        {
            var transformSyncs = GetComponentsInChildren<NetworkTransformSync>();
            BodyTransformSync = transformSyncs.FirstOrDefault(s => s.viewId == 0);
            CameraTransformSync = transformSyncs.FirstOrDefault(s => s.viewId == 1);
        }

        if (AnimatorSync == null) AnimatorSync = GetComponentInChildren<NetworkAnimatorSync>();
        if (WeaponController == null) WeaponController = GetComponent<WeaponController>();
        if (CharacterMove == null) CharacterMove = GetComponent<CharacterMove>();
        if (InputHandler == null) InputHandler = GetComponentInChildren<InputHandler>();
        if (CameraController == null) CameraController = GetComponentInChildren<CameraController>();
        if (CameraSwitcher == null) CameraSwitcher = GetComponentInChildren<CameraSwitcher>();
        if (NicknameUI == null) NicknameUI = GetComponentInChildren<PlayerNicknameUI>();
        if (StateMachine == null) StateMachine = GetComponentInChildren<NetworkStateMachine>();
        hitBoxColidersList = GetComponentInChildren<HitBoxColidersList>();
        characterController = GetComponent<CharacterController>();
    }

    public void Initialize(string steamId, bool isMine)
    {
        this.IsMine = isMine;

        if (BodyTransformSync != null) BodyTransformSync.Initialize(steamId, isMine);
        if (CameraTransformSync != null) CameraTransformSync.Initialize(steamId, isMine);
        if (AnimatorSync != null) AnimatorSync.Initialize(steamId, isMine);
        if (StateMachine != null) StateMachine.Initialize(isMine);

        var bodySlopeHandler = GetComponentInChildren<BodySlope_Handler>();
        if (bodySlopeHandler != null)
        {
            bodySlopeHandler.Initialize(isMine);
        }

        if (isMine)
        {
            if (NicknameUI != null) NicknameUI.gameObject.SetActive(false);

            UIEvents.PlayerInitialized(WeaponController, GetComponent<PlayerStats>());
        }
        else
        {
            if (CharacterMove != null) CharacterMove.enabled = false;
            if (InputHandler != null) InputHandler.enabled = false;
            if (CameraController != null) CameraController.enabled = false;
            if (CameraSwitcher != null) CameraSwitcher.gameObject.SetActive(false);
        }
    }

    public void Die()
    {
        SpawnRagdollCopy();

        hitBoxColidersList.HitboxesAsTriggers(true);
        characterController.enabled = false;

        InputHandler.enabled = false;
        CharacterMove.enabled = false;
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
            if (slot.transform.childCount <= 0)
                continue;
            Transform weapon = slot.transform.GetChild(0);
            weapon.parent = null;
            weapon.GetComponent<Collider>().enabled = true;
            weapon.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
