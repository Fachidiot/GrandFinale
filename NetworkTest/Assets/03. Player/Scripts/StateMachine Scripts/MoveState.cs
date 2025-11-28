using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MoveState : StateMachineBase
{
    private float currentSpeed;
    private float targetSpeed;

    private float speedOffset = 0.1f;
    private float speedChangeRate = 5f;

    private float forwardMoveSpeed;
    private float rightMoveSpeed;

    private float animationBlendMultiply;

    private bool _walk;
    public bool walk
    {
        get => _walk;
        set
        {
            characterMove.animator.SetBool(characterMove.walkID, value);
            _walk = value;
            isSprint = false;

            targetSpeed = value == true ? characterMove.walkSpeed : characterMove.sprintSpeed;
            animationBlendMultiply = value == true ? characterMove.walkSpeed : targetSpeed / 2f;
        }
    }

    private bool _isSprint;
    public bool isSprint
    {
        get => _isSprint;
        set
        {
            if (value == _isSprint) return;
            _isSprint = value;

            characterMove.animator.SetBool(characterMove.sprintID, value);

            targetSpeed = value == true ? characterMove.sprintSpeed : characterMove.sprintSpeed;
            animationBlendMultiply = value == true ? targetSpeed / 3f : targetSpeed / 2f;
        }
    }

    private bool isPause = false;

    public MoveState(CharacterMove characterMove) : base(characterMove)
    {
        GameManager.OnPauseStateChanged += OnPause;
    }

    public void OnDestroy()
    {
        GameManager.OnPauseStateChanged -= OnPause;
    }

    public override void Tick()
    {
        if (isPause)
            return;

        var inputVector = new Vector2(characterMove.Inputs.GetAxisHorizontal(), characterMove.Inputs.GetAxisVertical());
        isSprint = (characterMove.Inputs.GetSprint() && inputVector.y > 0 && !walk);

        // 1. 속도 계산
        var isMoving = inputVector.magnitude > 0 && characterMove.edgeSlipVelocity.magnitude == 0;
        float targetSpeed = isMoving ? (isSprint ? characterMove.sprintSpeed : (walk ? characterMove.walkSpeed : characterMove.walkSpeed)) : 0;
        currentSpeed = SpeedValueChange(currentSpeed, targetSpeed, speedChangeRate);

        // 2. 이동 방향 벡터 계산
        Quaternion moveForward = Quaternion.Euler(0, characterMove.directionOrienter.rotation.eulerAngles.y, 0);
        Vector3 rawMoveDirection = (moveForward * Vector3.forward * inputVector.y) + (moveForward * Vector3.right * inputVector.x);

        // 3. 경사면을 고려하여 이동 방향을 지면에 투영
        Vector3 projectedMoveDirection = Vector3.ProjectOnPlane(rawMoveDirection, characterMove.groundNormal).normalized;

        // 4. 최종 이동 속도 계산
        Vector3 horizontalVelocity = projectedMoveDirection * currentSpeed;

        // 5. 수직 속도(중력) 계산
        // isGrounded일 때는 중력을 계속 적용하여 경사면에서 뜨지 않도록 함
        Vector3 verticalVelocity = new Vector3(0, characterMove.velocity.y, 0);
        if (characterMove.isGrounded && verticalVelocity.y > characterMove.gravity * Time.deltaTime)
        {
            // 땅에 붙어있도록 약한 중력(-2)을 유지
            verticalVelocity.y = -2f;
        }

        // 6. 최종 속도 결합 및 적용
        characterMove.moveVelocity = horizontalVelocity + verticalVelocity + characterMove.edgeSlipVelocity;
        characterController.Move(characterMove.moveVelocity * Time.deltaTime);

        // 7. 입력에 따른 상태 변경
        if (characterMove.Inputs.GetCrouch())
        {
            characterMove.SetState(isSprint ? characterMove.rollState : characterMove.crouchState);
        }

        if (characterMove.Inputs.GetJump())
        {
            characterMove.SetState(characterMove.jumpState);
        }

        // 8. 기타 로직 (애니메이션, 몸 회전)
        characterMove.bodyTurnHandler.momentaryTurn = inputVector.magnitude > 0;
        characterMove.animator.SetFloat(characterMove.horizontalInputID, inputVector.x);
        characterMove.animator.SetFloat(characterMove.verticalInputID, inputVector.y);
    }

    private float SpeedValueChange(float inputValue, float targetvalue, float changeRateValue)
    {
        if (inputValue < targetvalue - speedOffset ||
               inputValue > targetvalue + speedOffset)
        {
            inputValue = Mathf.Lerp(inputValue, targetvalue,
                Time.deltaTime * changeRateValue);

            return inputValue;
        }
        else
        {
            return targetvalue;
        }
    }

    public override void OnStateEnter()
    {
        targetSpeed = characterMove.walkSpeed;
        currentSpeed = 0;
        characterMove.moveVelocity = Vector3.zero;
        animationBlendMultiply = targetSpeed / 2;
    }

    public override void OnStateExit()
    {
        isSprint = false;
    }

    void OnPause(bool pause)
    {
        isPause = pause;
        characterMove.StopAllActions();
    }
}
