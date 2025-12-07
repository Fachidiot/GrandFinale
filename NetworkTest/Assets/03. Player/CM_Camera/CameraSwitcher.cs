using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSwitcher : MonoBehaviour
{
    // --- 의존성 ---
    public CharacterMove characterMove;
    public EventsCenter eventsCenter;

    // --- 카메라 ---
    [SerializeField] private CinemachineVirtualCamera fpvCamera;
    [SerializeField] private CinemachineVirtualCamera fpv_aimCamera;
    [SerializeField] private CinemachineVirtualCamera tpvCamera;
    [SerializeField] private CinemachineVirtualCamera tpv_aimCamera;

    [SerializeField]
    private Camera mainCamera;

    // --- 상태 ---
    public enum CameraState
    {
        TPV,
        FPV,
        TPV_Aim,
        FPV_Aim
    }
    private CameraState currentState;

    // --- 프로퍼티 ---
    public bool IsFirstPersonView => currentState == CameraState.FPV || currentState == CameraState.FPV_Aim;
    public bool IsAiming => currentState == CameraState.TPV_Aim || currentState == CameraState.FPV_Aim;
    public bool IsShortFpv = false;

    // --- 상태 변수 (이벤트 수신용) ---
    private bool isGrounded;
    private bool isWeaponChange;

    // --- 상수 ---
    private const int ActivePriority = 2;
    private const int InactivePriority = 0;
    private const int DefaultPriority = 1;


    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        // 초기 상태를 TPV로 설정
        SwitchState(CameraState.TPV);
    }

    private void OnEnable()
    {
        characterMove.OnGroundedValueChange += ApplyIsGround;
        eventsCenter.OnWeaponChange += ApplyIsWeaponChange;
    }

    private void OnDisable()
    {
        characterMove.OnGroundedValueChange -= ApplyIsGround;
        eventsCenter.OnWeaponChange -= ApplyIsWeaponChange;
    }

    private void Update()
    {
        // 조준 중에 조준 불가능 상태가 되면 강제로 조준 해제
        if (IsAiming && !CanAimCheck())
        {
            StopAiming();
        }
    }

    #region Public Methods (Input Handling)

    /// <summary>
    /// 3인칭 시점(TPV)에서 조준을 시작합니다. (롱클릭)
    /// </summary>
    public void StartTpvAim()
    {
        if (currentState == CameraState.TPV && CanAimCheck())
        {
            SwitchState(CameraState.TPV_Aim);
        }
        // FPV 상태일 때는 FPV 조준 토글을 대신 실행
        else if (currentState == CameraState.FPV)
        {
            ToggleFpvAim();
        }
    }

    /// <summary>
    /// 1인칭(FPV) 조준 상태를 토글합니다. (숏클릭)
    /// </summary>
    public void ToggleFpvAim()
    {
        // if (currentState == CameraState.FPV && CanAimCheck())
        if (currentState != CameraState.FPV_Aim && CanAimCheck())
        {
            if (currentState == CameraState.TPV)
                IsShortFpv = true;
            SwitchState(CameraState.FPV_Aim);
        }
        else if (!IsShortFpv && currentState == CameraState.FPV_Aim)
        {
            SwitchState(CameraState.FPV);
        }
        else
        {
            SwitchState(CameraState.TPV);
            IsShortFpv = false;
        }
    }

    /// <summary>
    /// 모든 조준 상태를 즉시 해제합니다.
    /// </summary>
    public void StopAiming()
    {
        if (!IsAiming) return;

        if (currentState == CameraState.TPV_Aim)
        {
            SwitchState(CameraState.TPV);
        }
        else if (currentState == CameraState.FPV_Aim)
        {
            SwitchState(CameraState.FPV);
        }
    }

    /// <summary>
    /// 1인칭(FPV)과 3인칭(TPV) 시점을 전환합니다. 조준 중에는 작동하지 않습니다.
    /// </summary>
    public void ViewChange()
    {
        if (IsAiming) return;

        if (currentState == CameraState.TPV)
        {
            SwitchState(CameraState.FPV);
        }
        else if (currentState == CameraState.FPV)
        {
            SwitchState(CameraState.TPV);
        }
    }

    /// <summary>
    /// (사용되지 않는 것으로 보임) 조준 시점을 변경하는 함수.
    /// </summary>
    public void AimViewChange(bool tps)
    {
        if (!CanAimCheck()) return;

        if (tps) // TPV 조준 토글
        {
            SwitchState(currentState == CameraState.TPV_Aim ? CameraState.TPV : CameraState.TPV_Aim);
        }
        else // FPV 조준 토글
        {
            SwitchState(currentState == CameraState.FPV_Aim ? CameraState.FPV : CameraState.FPV_Aim);
        }
    }

    #endregion

    #region State Machine Core

    /// <summary>
    /// 카메라의 상태를 전환하고 관련 설정을 적용합니다.
    /// </summary>
    private void SwitchState(CameraState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        // characterMove 상태는 조준 여부에 따라 결정
        characterMove.moveState.walk = IsAiming;

        // 카메라 우선순위와 컬링 마스크 설정
        switch (currentState)
        {
            case CameraState.TPV:
                tpvCamera.Priority = DefaultPriority;
                fpvCamera.Priority = InactivePriority;
                tpv_aimCamera.Priority = InactivePriority;
                fpv_aimCamera.Priority = InactivePriority;
                SetHeadLayerVisibility(true);
                break;

            case CameraState.FPV:
                tpvCamera.Priority = InactivePriority;
                fpvCamera.Priority = DefaultPriority;
                tpv_aimCamera.Priority = InactivePriority;
                fpv_aimCamera.Priority = InactivePriority;
                SetHeadLayerVisibility(false);
                break;

            case CameraState.TPV_Aim:
                tpvCamera.Priority = DefaultPriority; // TPV를 베이스로 깔아둠
                fpvCamera.Priority = InactivePriority;
                tpv_aimCamera.Priority = ActivePriority;
                fpv_aimCamera.Priority = InactivePriority;
                SetHeadLayerVisibility(true);
                break;

            case CameraState.FPV_Aim:
                tpvCamera.Priority = InactivePriority;
                fpvCamera.Priority = DefaultPriority; // FPV를 베이스로 깔아둠
                tpv_aimCamera.Priority = InactivePriority;
                fpv_aimCamera.Priority = ActivePriority;
                SetHeadLayerVisibility(false); // FPV 조준 시에만 머리 숨김
                break;
        }
    }

    #endregion

    #region Helpers and Event Handlers

    private bool CanAimCheck()
    {
        return isGrounded && !characterMove.moveState.isSprint && !isWeaponChange;
    }

    void ApplyIsGround(bool value) => isGrounded = value;
    void ApplyIsWeaponChange(bool value) => isWeaponChange = value;

    /// <summary>
    /// 카메라의 컬링 마스크에서 'Head' 레이어를 제어합니다.
    /// </summary>
    /// <param name="visible">'Head' 레이어를 보이게 하려면 true, 숨기려면 false.</param>
    private void SetHeadLayerVisibility(bool visible)
    {
        if (mainCamera == null)
        {
            // 한번 더 찾아보기.
            mainCamera = Camera.main;
            if (mainCamera == null)
                return;
        }

        int headLayer = LayerMask.NameToLayer("Head");
        if (headLayer == -1)
        {
            Debug.LogWarning("CameraSwitcher: 'Head' layer not found. Culling mask will not be changed.");
            return;
        }

        if (visible)
        {
            mainCamera.cullingMask |= (1 << headLayer);
        }
        else
        {
            mainCamera.cullingMask &= ~(1 << headLayer);
        }
    }

    #endregion
}
