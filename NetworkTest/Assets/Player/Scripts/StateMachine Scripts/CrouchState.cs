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
        // var horizontalInput = Input.GetAxis("Horizontal");
        var horizontalInput = characterMove.Inputs.GetAxisHorizontal();
        // var verticalInput = Input.GetAxis("Vertical");
        var verticalInput = characterMove.Inputs.GetAxisVertical();

        Quaternion moveForward = Quaternion.Euler(0, characterMove.directionOrienter.rotation.eulerAngles.y, 0);

        characterMove.moveVelocity = Vector3.ClampMagnitude(moveForward * Vector3.forward * verticalInput + moveForward * Vector3.right * horizontalInput, 1) * characterMove.crouchSpeed;

        // if (Input.GetKeyDown(KeyCode.C))
        if (characterMove.Inputs.GetCrouch())
        {
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

        characterController.Move(characterMove.moveVelocity * Time.deltaTime);

        characterMove.bodyTurnHandler.momentaryTurn = horizontalInput + verticalInput > 0;

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
