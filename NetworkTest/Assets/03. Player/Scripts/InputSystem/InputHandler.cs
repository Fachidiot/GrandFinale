using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private WeaponPickup weaponPickUp;
    [SerializeField] private BodySlope_Handler bodySlope_Handler;
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private BodyTiltInSprint bodyTiltInSprint;
    [SerializeField] private Animator playerAnimator; // Added reference to Animator
    [SerializeField] private InventoryManager inventoryManager;

    private PlayerInputs playerInputs;
    private bool isPause = false;

    // Animator Parameter Hashes
    private readonly int CombatHash = Animator.StringToHash("isCombat"); // Added CombatHash

    // --- 마우스 우클릭 상태 변수 ---
    private bool isPressing = false;
    private float pressTime = 0f;
    private bool isLongAimTriggered = false;

    private void InitialCheck()
    {
        if (weaponController == null)
            Debug.LogError("InputHandler: WeaponController not found in Player. InputHandler will not function.");
        if (weaponPickUp == null)
            Debug.LogError("InputHandler: WeaponPickup not found in Player. Weapon pickup functionality will be disabled.");
        if (bodySlope_Handler == null)
            Debug.LogError("InputHandler: BodySlope_Handler not found in Player. Body leaning functionality will be disabled.");
        if (cameraSwitcher == null)
            Debug.LogError("InputHandler: CameraSwitcher not found in Player. Camera switching functionality will be disabled.");
        if (bodyTiltInSprint == null)
            Debug.LogError("InputHandler: BodyTiltInSprint not found in Player. Body tilt functionality will be disabled.");
        if (playerAnimator == null)
            Debug.LogError("InputHandler: Player Animator not found in Player. Animator-related functions will not work.");
        if (playerInputs == null)
            Debug.LogError("InputHandler: PlayerInputs not found in Player. Input will not be processed.");
    }

    private void Start()
    {
        GameManager.OnPauseStateChanged += OnPause;
        GameManager.Instance.TryGetComponent<PlayerInputs>(out playerInputs);
        inventoryManager = InventoryManager.Instance;

        InitialCheck();
    }

    void Update()
    {
        Inventory();

        if (isPause)
            return;

        TryShoot();

        bodySlope_Handler.setInput(playerInputs.GetBending());

        bodyTiltInSprint.SetMouseXMove(Input.GetAxis("Mouse X"));

        if (playerInputs.GetSlot0())
            weaponController.ToChange(0); // Changed to 0 for unarmed
        if (playerInputs.GetSlot1())
            weaponController.ToChange(1);
        if (playerInputs.GetSlot2())
            weaponController.ToChange(2);
        if (playerInputs.GetSlot3())
            weaponController.ToChange(3);
        if (playerInputs.GetSlot4())
            weaponController.ToChange(4);

        if (playerInputs.GetInteract() && weaponPickUp != null)
        {
            weaponPickUp.PickupCheck();
        }

        UnarmedAim();
        ArmedAim();
    }

    void Inventory()
    {
        if (!inventoryManager)
            return;

        // if (!inventoryManager.isExternalInteractionActive && )
        //     inventoryManager.ToggleSmallInventory();
        if (playerInputs.GetInventory())
            inventoryManager.ToggleFullInventory();
        else if (playerInputs.GetInventory() && inventoryManager.IsFocused)
            inventoryManager.CloseAllInventories();

        // if (inventoryManager.IsFocused && !inventoryManager.isExternalInteractionActive && playerInputs.GetAttack())
        // {
        //     if (EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject())
        //         CloseAllInventories();
        // }
    }

    void UnarmedAim()
    {   // Combat Stance Logic for Unarmed
        if (playerAnimator != null && weaponController != null)
        {
            var currentWeapon = weaponController.GETCurrentWeapon;
            if (currentWeapon != null && currentWeapon.Type == IWeapon.SlotType.unarmed)
            {
                playerAnimator.SetBool(CombatHash, playerInputs.GetAimed());
            }
            else
            {
                playerAnimator.SetBool(CombatHash, false); // Ensure combat stance is off if not unarmed
            }
        }
    }

    void ArmedAim()
    {
        // 1. 마우스 우클릭 시작 감지
        if (Input.GetMouseButtonDown(1))
        {
            isPressing = true;
            isLongAimTriggered = false;
            pressTime = 0f;
        }
        // 2. 마우스 우클릭 누르고 있는 동안 처리 (롱클릭 감지)
        if (Input.GetMouseButton(1) && isPressing)
        {
            pressTime += Time.deltaTime;

            if (!isLongAimTriggered && pressTime >= 0.2f)
            {
                isLongAimTriggered = true;
                cameraSwitcher.StartTpvAim(); // 롱클릭: TPV 조준 시작
            }
        }
        // 3. 마우스 우클릭에서 손을 뗐을 때 처리
        if (Input.GetMouseButtonUp(1) && isPressing)
        {
            // 롱클릭 상태에서 손을 뗐다면 조준 중지
            if (isLongAimTriggered)
            {
                cameraSwitcher.StopAiming();
            }
            // 롱클릭이 발동되기 전(0.3초 미만)에 손을 뗐다면 FPV 조준 토글
            else
            {
                cameraSwitcher.ToggleFpvAim();
            }
            isPressing = false;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            cameraSwitcher.ViewChange();
        }

        if (playerInputs.GetReload())
        {
            var weapon = weaponController.GETCurrentWeapon as RangedWeapon;
            if (weapon != null)
            {
                weapon.Reload();
            }
        }
    }

    void OnPause(bool pause)
    {
        isPause = pause;
    }

    void TryShoot()
    {
        if (bodyTiltInSprint == null)
            return;
        if (bodyTiltInSprint.standState.isSprint)
            return;

        if (weaponController == null)
            return;

        var weapon = weaponController.GETCurrentWeapon;
        if (weapon == null)
            return;

        bool singleshoot = false;
        if (weapon is RangedWeapon rangedWeapon)
        {
            singleshoot = rangedWeapon.SingleShoot;
        }

        if (singleshoot && Input.GetMouseButtonDown(0))
        {
            weaponController.StartShoot();
        }
        else if (!singleshoot && Input.GetMouseButton(0))
        {
            weaponController.StartShoot();
        }
    }
}
