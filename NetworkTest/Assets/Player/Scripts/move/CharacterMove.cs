using System.Collections;
using UnityEngine;

public class CharacterMove : MonoBehaviour
{
    [Header("Components")]
    public CharacterController characterController;
    public BodyTurnHandler bodyTurnHandler;
    public Animator animator;
    public Transform directionOrienter;

    [Header("Colider values")]
    public float crouchColliderHeight = 1f;
    public float normalColliderHeight { get; private set; }
    public float grounCheckDistance;

    private bool _isGrounded;
    public bool isGrounded
    {
        get => _isGrounded;
        set
        {
            if (value == _isGrounded) return;

            _isGrounded = value;

            if (!_isGrounded && currentState != inAirState)
                SetState(inAirState);

            animator.SetBool("isGrounded", value);

            OnGroundedValueChange.Invoke(value);
        }
    }
    public LayerMask groundCheckMask;
    public delegate void isGroundedChange(bool changed);
    public event isGroundedChange OnGroundedValueChange;
    public float edgeFallMoveForce = 1f;
    public float noSlipDistance = .1f;


    [Header("Move values")]
    public float gravity = -9.81f;
    public float walkSpeed = 2;
    public float runSpeed = 3;
    public float sprintSpeed = 5;
    public float crouchSpeed = 1;

    public float jumpHeight = 1f;

    [Header("Velocity values")]
    public Vector3 moveVelocity;
    public Vector3 velocity;
    public Vector3 rollVelocity;
    public Vector3 edgeSlipVelocity;

    public StateMachineBase previousState;
    public StateMachineBase currentState;
    public MoveState standState { get; private set; }
    public CrouchState crouchState { get; private set; }
    public RollState rollState { get; private set; }
    public JumpState jumpState { get; private set; }
    public InAirState inAirState { get; private set; }

    // animator ids
    public int horizontalInputID { get; private set; }
    public int verticalInputID { get; private set; }
    public int walkID { get; private set; }
    public int crouchID { get; private set; }
    public int isGroundID { get; private set; }
    public int sprintID { get; private set; }
    public int rollID { get; private set; }

    IEnumerator colliderSizeChangeCor;

    private void Awake()
    {
        AssighAnimatorIDs();
        colliderSizeChangeCor = ColliderSizeChangeSmooth(false);
        characterController = GetComponent<CharacterController>();
        normalColliderHeight = characterController.height;


        standState = new MoveState(this);
        crouchState = new CrouchState(this);
        rollState = new RollState(this);
        jumpState = new JumpState(this);
        inAirState = new InAirState(this);
    }

    private void Start()
    {
        currentState = standState;
        SetState(inAirState);
    }

    public void SetState(StateMachineBase state)
    {
        if (currentState != null)
            currentState.OnStateExit();

        previousState = currentState;
        currentState = state;


        bool coliderReduce = currentState == crouchState | currentState == rollState;
        if (colliderSizeChangeCor != null) StopCoroutine(colliderSizeChangeCor);
        colliderSizeChangeCor = ColliderSizeChangeSmooth(coliderReduce);
        StartCoroutine(colliderSizeChangeCor);

        if (currentState != null)
            currentState.OnStateEnter();
    }

    private void Update()
    {
        if (currentState == null) return;

        GroundCheck();

        currentState.Tick();
    }

    void GroundCheck()
    {
        RaycastHit hitInfo;

        if (velocity.y <= 0 && Physics.SphereCast(transform.position + characterController.center, characterController.radius + characterController.skinWidth, Vector3.down, out hitInfo, grounCheckDistance, groundCheckMask, QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
            Vector3 relativeHitPoint = hitInfo.point - (transform.position + Vector3.right * characterController.center.x + Vector3.forward * characterController.center.z);

            Debug.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * 0.3f, Color.red);

            if (characterController.velocity.y < 0 && relativeHitPoint.magnitude > noSlipDistance && !Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f, groundCheckMask))
            {
                Vector3 edgeFallMovement = transform.position - hitInfo.point;
                edgeFallMovement.y = 0;
                edgeSlipVelocity += (edgeFallMovement * Time.deltaTime * edgeFallMoveForce);
            }
            else
            {
                edgeSlipVelocity = Vector3.zero;
            }
        }
        else
        {
            isGrounded = false;
            edgeSlipVelocity = Vector3.zero;
        }

    }

    IEnumerator ColliderSizeChangeSmooth(bool reduce)
    {
        var startSize = characterController.height;
        var finalSize = reduce ? crouchColliderHeight : normalColliderHeight;
        var startCenter = characterController.center.y;
        var finalCener = finalSize / 2f;
        float t = 0;

        while (t < 0.3f)
        {
            characterController.height = Mathf.Lerp(startSize, finalSize, t / 0.3f);
            characterController.center = new Vector3(characterController.center.x, Mathf.Lerp(startCenter, finalCener, t / 0.3f), characterController.center.z);
            t += Time.deltaTime;
            yield return null;
        }
        characterController.height = finalSize;
        yield break;
    }

    private void AssighAnimatorIDs()
    {
        horizontalInputID = Animator.StringToHash("x");
        verticalInputID = Animator.StringToHash("y");
        isGroundID = Animator.StringToHash("isGround");
        sprintID = Animator.StringToHash("sprint");
        rollID = Animator.StringToHash("roll");
        walkID = Animator.StringToHash("walk");
        crouchID = Animator.StringToHash("crouch");
    }

}
