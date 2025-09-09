using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : StateMachineBase
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

    public IdleState(CharacterMove characterMove) : base(characterMove)
    {
    }

    public override void Tick()
    {
        var inputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        isSprint = (Input.GetKey(KeyCode.LeftShift) && inputVector.y > 0 && !walk);

        Quaternion moveForward = Quaternion.Euler(0, characterMove.directionOrienter.rotation.eulerAngles.y, 0);

        var isMove = inputVector.magnitude == 0 | characterMove.edgeSlipVelocity.magnitude != 0 ? 0 : targetSpeed; //

        currentSpeed = SpeedValueChange(currentSpeed, isMove, speedChangeRate);

        forwardMoveSpeed = inputVector.y * currentSpeed;
        rightMoveSpeed = inputVector.x * currentSpeed;

        characterMove.moveVelocity = Vector3.ClampMagnitude(moveForward * Vector3.forward * forwardMoveSpeed + moveForward * Vector3.right * rightMoveSpeed, currentSpeed) + characterMove.velocity + characterMove.edgeSlipVelocity;


        if (Input.GetKeyDown(KeyCode.C))
        {
            characterMove.SetState(isSprint ? characterMove.rollState : characterMove.crouchState);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            characterMove.SetState(characterMove.jumpState);
        }

        characterController.Move(characterMove.moveVelocity * Time.deltaTime);

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
        targetSpeed = characterMove.sprintSpeed;
        currentSpeed = 0;
        characterMove.moveVelocity = Vector3.zero;
        animationBlendMultiply = targetSpeed / 2;
    }

    public override void OnStateExit()
    {
        isSprint = false;
    }
}