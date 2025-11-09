using UnityEngine;



[RequireComponent(typeof(Animator))]

public class NetworkAnimatorSync : MonoBehaviour

{

    public bool isMine = false;



    private Animator animator;



    // --- Receive and interpolate data ---

    private float targetX = 0f;

    private float targetY = 0f;

    private float smoothingFactor = 10.0f;



    // Animator parameter hashes for efficiency

    private readonly int xHash = Animator.StringToHash("x");

    private readonly int yHash = Animator.StringToHash("y");

    private readonly int walkHash = Animator.StringToHash("walk");

    private readonly int sprintHash = Animator.StringToHash("sprint");

    private readonly int rollHash = Animator.StringToHash("roll");

    private readonly int isGroundedHash = Animator.StringToHash("isGrounded");

    private readonly int crouchHash = Animator.StringToHash("crouch");



    void Awake()

    {

        animator = GetComponent<Animator>();

    }



    public void Initialize(string id, bool isOwner)

    {

        this.isMine = isOwner;

    }



    void Update()

    {

        if (!isMine)

        {

            // For remote clients, smoothly update float parameters

            float currentX = animator.GetFloat(xHash);

            float currentY = animator.GetFloat(yHash);

            animator.SetFloat(xHash, Mathf.Lerp(currentX, targetX, Time.deltaTime * smoothingFactor));

            animator.SetFloat(yHash, Mathf.Lerp(currentY, targetY, Time.deltaTime * smoothingFactor));

        }

    }



    public void OnAnimationDataReceived(float x, float y, bool walk, bool sprint, bool roll, bool isGrounded, bool crouch)

    {

        if (!isMine)

        {

            // Store target float values for interpolation

            targetX = x;

            targetY = y;



            // Set bool values directly

            animator.SetBool(walkHash, walk);

            animator.SetBool(sprintHash, sprint);

            animator.SetBool(crouchHash, crouch);

            animator.SetBool(isGroundedHash, isGrounded);



            // Handle triggers like roll directly

            if (roll)

            {

                animator.SetTrigger(rollHash);

            }

        }

    }



    // --- Methods for the host to read local state ---

    public float GetHorizontal()

    {

        return animator.GetFloat(xHash);

    }



    public float GetVertical()

    {

        return animator.GetFloat(yHash);

    }



    public byte GetAnimationMask()

    {

        byte mask = 0;

        if (animator.GetBool(walkHash)) mask |= AnimationBitmask.Walk;

        if (animator.GetBool(sprintHash)) mask |= AnimationBitmask.Sprint;

        if (animator.GetBool(rollHash)) mask |= AnimationBitmask.Roll; // Note: Roll is a trigger, this might not work as expected.

        if (animator.GetBool(isGroundedHash)) mask |= AnimationBitmask.IsGrounded;

        if (animator.GetBool(crouchHash)) mask |= AnimationBitmask.Crouch;

        return mask;

    }

}


