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
            // 1. 가독성을 위해 변수로 추출
            Vector3 origin = characterMove.transform.position;
            float radius = characterMove.characterController.radius;
            Vector3 direction = Vector3.up;
            float maxDistance = characterMove.normalColliderHeight - radius + characterMove.characterController.skinWidth;

            // 2. [디버그] 쏘기 전에 예상 궤적 그리기 (노란색)
            // 중심선
            Debug.DrawRay(origin, direction * maxDistance, Color.yellow);
            // 구체의 부피를 체감하기 위한 외곽선 4개 (전, 후, 좌, 우)
            Debug.DrawRay(origin + Vector3.forward * radius, direction * maxDistance, Color.yellow);
            Debug.DrawRay(origin + Vector3.back * radius, direction * maxDistance, Color.yellow);
            Debug.DrawRay(origin + Vector3.left * radius, direction * maxDistance, Color.yellow);
            Debug.DrawRay(origin + Vector3.right * radius, direction * maxDistance, Color.yellow);

            // 3. 실제 SphereCast 수행
            if (Physics.SphereCast(origin, radius, direction, out RaycastHit hit2, maxDistance, characterMove.groundCheckMask))
            {
                // [디버그] 충돌 발생 시 충돌 지점까지 빨간색 선 표시
                Debug.DrawLine(origin, hit2.point, Color.red);
                // 충돌한 물체 이름과 거리 로그 출력
                Debug.Log($"Can't get up. Hit: {hit2.collider.name}, Dist: {hit2.distance}");

                return;
            }
            // // 일어서도 되는지 천장 체크
            // if (Physics.SphereCast(characterMove.transform.position, characterMove.characterController.radius, Vector3.up, out RaycastHit hit2, characterMove.normalColliderHeight - characterMove.characterController.radius + characterMove.characterController.skinWidth, characterMove.groundCheckMask))
            // {
            //     Debug.Log("Can't get up");
            //     return;
            // }
            // else
            // {
            //     characterMove.SetState(characterMove.moveState);
            // }
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
