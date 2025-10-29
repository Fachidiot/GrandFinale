using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollState : StateMachineBase
{
    private float rollTime = 1.5f;
    private float currentTime = 0;
    private Vector3 startVelocity;
    public RollState(CharacterMove characterMove) : base(characterMove)
    {
    }

    public override void Tick()
    {
        characterMove.bodyTurnHandler.momentaryTurn = true;
        characterController.Move(characterMove.rollVelocity * Time.deltaTime);

        if (currentTime < rollTime && characterMove.rollVelocity != Vector3.zero)
        {
            characterMove.rollVelocity = Vector3.Lerp(startVelocity, Vector3.zero, currentTime / rollTime);
            currentTime += Time.deltaTime;
        }
        else
        {
            characterMove.SetState(characterMove.crouchState);
        }
    }

    public override void OnStateEnter()
    {
        currentTime = 0;
        characterMove.animator.SetBool(characterMove.rollID, true);
        characterMove.rollVelocity = characterMove.moveVelocity;
        startVelocity = characterMove.rollVelocity;
        characterMove.moveVelocity = Vector3.zero;
        if (characterMove.rollVelocity == Vector3.zero)
        {
            characterMove.SetState(characterMove.crouchState);
        }
    }

    public override void OnStateExit()
    {
        characterMove.rollVelocity = Vector3.zero;
        characterMove.animator.SetBool(characterMove.rollID, false);
    }
}
