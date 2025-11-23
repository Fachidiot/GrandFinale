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

    private PlayerInputs playerInputs;
    private bool isPause = false;

    // --- 마우스 우클릭 상태 변수 ---
    private bool isPressing = false;
    private float pressTime = 0f;
    private bool isLongAimTriggered = false;

    private void Start()
    {
        GameManager.OnPauseStateChanged += OnPause;
        //weaponController.activeID = 1; // Replaced by EquipWeapon
        weaponController.EquipWeapon(1); // Equip weapon in slot 1 on start
        weaponController.animator.Play("GunPickUp", 1); // This might need to be adjusted with new animation states
        GameManager.Instance.TryGetComponent<PlayerInputs>(out playerInputs);
    }

    void Update()
    {
        if (isPause)
            return;

        TryShoot();

        // bodySlope_Handler.setInput(-Input.GetAxisRaw("Slope")); // Q E
        bodySlope_Handler.setInput(playerInputs.GetBending());


        bodyTiltInSprint.SetMouseXMove(Input.GetAxis("Mouse X"));

        if (playerInputs.GetSlot0())
            weaponController.EquipWeapon(0); // Equip unarmed
        if (playerInputs.GetSlot1())
            weaponController.EquipWeapon(1);
        if (playerInputs.GetSlot2())
            weaponController.EquipWeapon(2);
        if (playerInputs.GetSlot3())
            weaponController.EquipWeapon(3);
        if (playerInputs.GetSlot4())
            weaponController.EquipWeapon(4);


        if (Input.GetKeyDown(KeyCode.F) && weaponPickUp != null)
        {
            weaponPickUp.PickupCheck();
        }

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

        if (Input.GetKeyDown(KeyCode.R))
        {
            (weaponController.GETCurrentWeapon as RangedWeapon)?.Reload();
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
        if (weaponController.GETCurrentWeapon == null || weaponController.GETCurrentWeapon.Type == BaseWeapon.SlotType.unarmed)
            return;

        RangedWeapon currentRangedWeapon = weaponController.GETCurrentWeapon as RangedWeapon;
        if (currentRangedWeapon != null)
        {
            bool singleshoot = currentRangedWeapon.SingleShoot;
            if (singleshoot && Input.GetMouseButtonDown(0))
            {
                weaponController.StartAttack();
            }
            else if (!singleshoot && Input.GetMouseButton(0))
            {
                weaponController.StartAttack();
            }
        }
        else if (weaponController.GETCurrentWeapon.Type == BaseWeapon.SlotType.melee)
        {
            if (Input.GetMouseButtonDown(0))
            {
                weaponController.StartAttack();
            }
        }
    }
}
