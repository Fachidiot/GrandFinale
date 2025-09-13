using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpState : StateMachineBase
{

    public JumpState(CharacterMove characterMove) : base(characterMove)
    {
    }


    public override void Tick()
    {
        characterMove.bodyTurnHandler.momentaryTurn = true;
        if (characterMove.isGrounded)
        {
            //characterMove.SetState(characterMove.runState);
        }

        //characterMove.velocity.y += characterMove.gravity * Time.deltaTime;

        characterController.Move(characterMove.velocity * Time.deltaTime);
    }

    public override void OnStateEnter()
    {
        characterMove.velocity.x = characterMove.moveVelocity.x;
        characterMove.velocity.z = characterMove.moveVelocity.z;
        characterMove.velocity.y = Mathf.Sqrt(characterMove.jumpHeight * -2 * characterMove.gravity);
        characterMove.moveVelocity = Vector3.zero;
    }
    public override void OnStateExit()
    {

    }
}
