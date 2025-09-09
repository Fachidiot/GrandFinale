using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InAirState : StateMachineBase
{
    Animator animator;
    public InAirState(CharacterMove characterMove) : base(characterMove)
    {
        animator = characterMove.animator;
    }

    public override void Tick()
    {
        characterMove.bodyTurnHandler.momentaryTurn = true;

        characterMove.velocity.y += characterMove.gravity * Time.deltaTime;

        characterController.Move(characterMove.velocity * Time.deltaTime);

        if (characterMove.IsGrounded) characterMove.SetState(characterMove.standState);
    }

    public override void OnStateEnter()
    {
        characterMove.velocity += characterMove.moveVelocity;
        characterMove.velocity += characterMove.rollVelocity;

        animator.SetBool(characterMove.sprintID, false);
        animator.SetBool(characterMove.crouchID, false);
        animator.SetBool(characterMove.walkID, false);
        animator.SetBool(characterMove.rollID, false);
    }

    public override void OnStateExit()
    {
        characterMove.velocity = new Vector3(0, -2f, 0);
    }
}
