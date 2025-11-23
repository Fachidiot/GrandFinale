using UnityEngine;
using System.Linq;

public class NetworkPlayer : MonoBehaviour, IPlayerControllable
{
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
            
            UIEvents.PlayerInitialized(WeaponController);
        }
        else
        {
            if (CharacterMove != null) CharacterMove.enabled = false;
            if (InputHandler != null) InputHandler.enabled = false;
            if (CameraController != null) CameraController.enabled = false;
            if (CameraSwitcher != null) CameraSwitcher.gameObject.SetActive(false);
        }
    }
}
