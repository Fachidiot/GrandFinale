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
        weaponController.activeID = 1;
        weaponController.animator.Play("GunPickUp", 1);
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

        // if (Input.GetKeyDown(KeyCode.Alpha1))
        //     weaponController.ToChange(1);
        // if (Input.GetKeyDown(KeyCode.Alpha2))
        //     weaponController.ToChange(2);
        // if (Input.GetKeyDown(KeyCode.Alpha3))
        //     weaponController.ToChange(3);
        // if (Input.GetKeyDown(KeyCode.Alpha4))
        //     weaponController.ToChange(4);

        if (playerInputs.GetSlot0())
            weaponController.ToChange(5);
        if (playerInputs.GetSlot1())
            weaponController.ToChange(1);
        if (playerInputs.GetSlot2())
            weaponController.ToChange(2);
        if (playerInputs.GetSlot3())
            weaponController.ToChange(3);
        if (playerInputs.GetSlot4())
            weaponController.ToChange(4);


        if (Input.GetKeyDown(KeyCode.F) && weaponPickUp != null)
        // if (playerInputs.GetInteract() && weaponPickUp != null)
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
        // if (playerInputs.GetReload())
        {
            weaponController.GETCurrentWeapon.Reload();
        }
    }

    void OnPause(bool pause)
    {
        isPause = pause;
    }

    void TryShoot()
    {
        // Prevent shooting while sprinting
        if (bodyTiltInSprint.standState.isSprint) return;

        if (!weaponController.GETCurrentWeapon)
            return;

        bool singleshoot = weaponController.GETCurrentWeapon.SingleShoot;
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
