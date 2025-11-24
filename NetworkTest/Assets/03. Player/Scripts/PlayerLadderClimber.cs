using UnityEngine;

public class PlayerLadderClimber : MonoBehaviour
{
    private bool isClimbing = false;
    private Ladder currentLadder;

    private CharacterController characterController;
    private Animator animator;

    [SerializeField] private CameraController cameraController;
    [SerializeField] private BodyTurnHandler bodyTurnHandler;
    [SerializeField] private GameObject model;
    private CharacterMove characterMove; // Assuming CharacterMove is the main movement script
    private PlayerInputs playerInputs;

    private void InitialCheck()
    {
        if (characterController == null)
            Debug.LogError("PlayerLadderClimber: CharacterController is not assigned. Climbing will not function.");
        if (animator == null)
            Debug.LogError("PlayerLadderClimber: Animator is not assigned. Climbing animations will not function.");
        if (cameraController == null)
            Debug.LogError("PlayerLadderClimber: CameraController is not assigned. Camera rotation during climbing will not function.");
        if (bodyTurnHandler == null)
            Debug.LogError("PlayerLadderClimber: BodyTurnHandler is not assigned. Body turn functionality will be affected.");
        if (model == null)
            Debug.LogError("PlayerLadderClimber: Player Model is not assigned. Animator reference might be incorrect.");
        if (characterMove == null)
            Debug.LogError("PlayerLadderClimber: CharacterMove is not assigned. Player movement will not be disabled correctly.");
        if (playerInputs == null)
            Debug.LogError("PlayerLadderClimber: PlayerInputs component not found. Climbing input will not be processed.");
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        characterMove = GetComponent<CharacterMove>();
        animator = model.GetComponent<Animator>();
        // rb = GetComponent<Rigidbody>();

        // Get PlayerInputs from the local player
        if (GameManager.Instance != null)
        {
            playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();
        }
        // No longer logging error here, InitialCheck will handle it.

        InitialCheck(); // Call InitialCheck after all references are attempted to be assigned
    }

    public void StartClimbing(Ladder ladder, Transform startTrans)
    {
        if (isClimbing) return;

        currentLadder = ladder;
        isClimbing = true;

        // Disable normal player movement
        if (characterMove != null)
        {
            characterMove.enabled = false;
        }
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        if (playerInputs != null)
        {
            playerInputs.SetClimbingState(true); // Notify PlayerInputs about climbing state
        }

        // Snap player to the ladder's starting position
        transform.position = startTrans.position;
        bodyTurnHandler.enabled = false;
        model.GetComponent<Transform>().localRotation = startTrans.rotation;

        // TODO: Trigger climbing animation
        animator.SetBool("isClimbing", true);
    }

    public void StopClimbing()
    {
        if (!isClimbing) return;

        isClimbing = false;
        currentLadder = null;

        // Re-enable normal player movement
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        if (characterMove != null)
        {
            characterMove.enabled = true;
        }
        if (playerInputs != null)
        {
            playerInputs.SetClimbingState(false); // Notify PlayerInputs about climbing state
        }


        // TODO: Stop climbing animation
        animator.SetBool("isClimbing", false);
    }

    private void FixedUpdate()
    {
        if (!isClimbing || playerInputs == null) return;

        // Get vertical input from PlayerInputs
        float verticalInput = playerInputs.GetAxisVertical();

        // Move the player along the ladder
        if (verticalInput != 0)
        {
            transform.position += transform.up * verticalInput * Time.deltaTime * 3f; // 3f is climb speed
            animator.SetFloat("y", verticalInput);
        }
        else
            animator.SetFloat("y", 0f);

        // Check for reaching the top or bottom of the ladder
        if (verticalInput > 0 && transform.position.y > currentLadder.TopPoint.position.y)
        {
            transform.position = currentLadder.TopDismountPoint.position;
            // cameraController.RotateCamera180();
            bodyTurnHandler.enabled = true;
            StopClimbing();
            return; // Stop further checks in this frame
        }
        if (verticalInput < 0 && transform.position.y < currentLadder.BottomPoint.position.y)
        {
            bodyTurnHandler.enabled = true;
            StopClimbing();
            return; // Stop further checks in this frame
        }

        // Check for exit conditions (e.g., reaching top/bottom, pressing a key)
        // For example, pressing the interact key again to get off
        if (playerInputs.GetInteract())
        {
            bodyTurnHandler.enabled = true;
            StopClimbing();
        }
    }
}
