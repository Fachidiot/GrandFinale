using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class CrouchState : StateMachineBase
{
    public CrouchState(CharacterMove characterMove) : base(characterMove)
    {
    }

    public override void Tick()
    {
        var horizontalInput = characterMove.Inputs.GetAxisHorizontal();
        var verticalInput = characterMove.Inputs.GetAxisVertical();

        // 1. 이동 방향 벡터 계산
        Quaternion moveForward = Quaternion.Euler(0, characterMove.directionOrienter.rotation.eulerAngles.y, 0);
        Vector3 rawMoveDirection = (moveForward * Vector3.forward * verticalInput) + (moveForward * Vector3.right * horizontalInput);

        // 2. 경사면을 고려하여 이동 방향을 지면에 투영
        Vector3 projectedMoveDirection = Vector3.ProjectOnPlane(rawMoveDirection, characterMove.groundNormal).normalized;

        // 3. 최종 수평 이동 속도 계산
        Vector3 horizontalVelocity = projectedMoveDirection * characterMove.crouchSpeed * rawMoveDirection.magnitude;

        // 4. 수직 속도(중력) 계산
        Vector3 verticalVelocity = new Vector3(0, characterMove.velocity.y, 0);
        if (characterMove.isGrounded && verticalVelocity.y > characterMove.gravity * Time.deltaTime)
        {
            verticalVelocity.y = -2f; // 땅에 붙어있도록 약한 중력 유지
        }

        // 5. 최종 속도 결합 및 적용
        characterMove.moveVelocity = horizontalVelocity + verticalVelocity;
        characterController.Move(characterMove.moveVelocity * Time.deltaTime);

        // --- 상태 변경 로직 ---
        if (characterMove.Inputs.GetCrouch())
        {
            // 일어서도 되는지 천장 체크
            if (Physics.SphereCast(characterMove.transform.position, characterMove.characterController.radius, Vector3.up, out RaycastHit hit2, characterMove.normalColliderHeight - characterMove.characterController.radius + characterMove.characterController.skinWidth, characterMove.groundCheckMask))
            {
                Debug.Log("Can't get up");
                return;
            }
            else
            {
                characterMove.SetState(characterMove.moveState);
            }
        }

        if (characterMove.Inputs.GetJump() || characterMove.Inputs.GetSprint())
        {
            characterMove.SetState(characterMove.moveState);
        }

        // --- 애니메이션 및 회전 로직 ---
        characterMove.bodyTurnHandler.momentaryTurn = rawMoveDirection.magnitude > 0;
        characterMove.animator.SetFloat(characterMove.horizontalInputID, horizontalInput);
        characterMove.animator.SetFloat(characterMove.verticalInputID, verticalInput);
    }

    public override void OnStateExit()
    {
        characterMove.animator.SetBool(characterMove.crouchID, false);
    }

    public override void OnStateEnter()
    {
        characterMove.animator.SetBool(characterMove.crouchID, true);
    }

}
