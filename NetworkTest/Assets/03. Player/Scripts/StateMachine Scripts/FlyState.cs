using UnityEngine;

public class FlyState : StateMachineBase
{
    private float flySpeed = 10f; // Or get from CharacterMove
    private bool isPause = false;

    public FlyState(CharacterMove characterMove) : base(characterMove)
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

        // 1. Get Input
        var inputVector = new Vector2(characterMove.Inputs.GetAxisHorizontal(), characterMove.Inputs.GetAxisVertical());

        // Add vertical movement (Up/Down)
        float verticalMovementInput = 0f;
        if (characterMove.Inputs.GetJump()) // Using Jump for Up
        {
            verticalMovementInput = 1f;
        }
        else if (characterMove.Inputs.GetCrouch()) // Using Crouch for Down
        {
            verticalMovementInput = -1f;
        }


        // 2. Get Camera direction
        // We use the raw camera direction, not projected onto a plane
        Transform cameraTransform = characterMove.directionOrienter;

        // 3. Calculate move direction
        Vector3 moveDirection = (cameraTransform.forward * inputVector.y + cameraTransform.right * inputVector.x);
        
        // Add vertical movement
        moveDirection += Vector3.up * verticalMovementInput;


        // 4. Apply movement
        // No gravity, no extra velocities. Just direct movement.
        characterController.Move(moveDirection.normalized * flySpeed * Time.deltaTime);

        // 5. Animator (optional, for now just set to a generic flying blend tree)
        characterMove.animator.SetFloat(characterMove.horizontalInputID, inputVector.x);
        characterMove.animator.SetFloat(characterMove.verticalInputID, inputVector.y);
        
        // No transitions out for now, user needs to call a method to exit fly mode.
    }

    public override void OnStateEnter()
    {
        // Zero out any existing velocity from other states
        characterMove.velocity = Vector3.zero;
        characterMove.moveVelocity = Vector3.zero;
        
        // Set animator to a flying state if it exists
        // characterMove.animator.SetBool("isFlying", true);
    }

    public override void OnStateExit()
    {
        // characterMove.animator.SetBool("isFlying", false);
    }

    void OnPause(bool pause)
    {
        isPause = pause;
        if (isPause)
        {
             characterMove.animator.SetFloat(characterMove.horizontalInputID, 0f);
             characterMove.animator.SetFloat(characterMove.verticalInputID, 0f);
        }
    }
}
