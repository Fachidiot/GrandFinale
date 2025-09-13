using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSwitcher : MonoBehaviour
{

    public CharacterMove characterMove;
    public EventsCenter eventsCenter;

    [SerializeField] private CinemachineVirtualCamera fpvCamera;
    [SerializeField] private CinemachineVirtualCamera fpv_aimCamera;
    [SerializeField] private CinemachineVirtualCamera tpvCamera;
    [SerializeField] private CinemachineVirtualCamera tpv_aimCamera;

    bool isGrounded;
    bool isWeaponChange;


    bool isFirstpersonView;
    bool isAim;

    bool canAimCheck()
    {
        var canAim = (isGrounded && !characterMove.standState.isSprint && !isWeaponChange);
        return canAim;
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

    void ApplyIsGround(bool value) => isGrounded = value;
    void ApplyIsWeaponChange(bool value) => isWeaponChange = value;



    public void AimViewChange()
    {
        if (canAimCheck())
        {
            fpv_aimCamera.Priority = fpv_aimCamera.Priority == 0 ? 2 : 0;
            isAim = !isAim;
            characterMove.standState.walk = isAim;
        }
    }
    public void ViewChange()
    {
        isFirstpersonView = !isFirstpersonView;
        tpvCamera.Priority = isFirstpersonView ? 0 : 1;
        fpvCamera.Priority = isFirstpersonView ? 1 : 0;
    }

    private void Update()
    {
        if (isAim)
        {
            if (!canAimCheck())
            {
                fpv_aimCamera.Priority = 0;
                isAim = false;
                characterMove.standState.walk = isAim;
            }
        }
    }
}
