using System.Collections;
using UnityEngine;

public class CharacterMove : MonoBehaviour
{
    [Header("Components")]
    public CharacterController characterController;
    public BodyTurnHandler bodyTurnHandler;
    public Animator animator;
    public Transform directionOrienter;
    private PlayerInputs playerInputs;
    public PlayerInputs Inputs { get { return playerInputs; } }

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

            if (!_isGrounded && currentState != inAirState && currentState != flyState)
                SetState(inAirState);

            animator.SetBool("isGrounded", value);

            if (OnGroundedValueChange != null)
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
    public float sprintSpeed = 5;
    public float crouchSpeed = 1;
    public float flySpeed = 10f;

    public float jumpHeight = 1f;

    [Header("Velocity values")]
    public Vector3 moveVelocity;
    public Vector3 velocity;
    public Vector3 rollVelocity;
    public Vector3 edgeSlipVelocity;
    public Vector3 groundNormal;

    public StateMachineBase previousState;
    public StateMachineBase currentState;
    public MoveState moveState { get; private set; }
    public CrouchState crouchState { get; private set; }
    public RollState rollState { get; private set; }
    public JumpState jumpState { get; private set; }
    public InAirState inAirState { get; private set; }
    public FlyState flyState { get; private set; }

    // animator ids
    public int horizontalInputID { get; private set; }
    public int verticalInputID { get; private set; }
    public int walkID { get; private set; }
    public int crouchID { get; private set; }
    public int isGroundID { get; private set; }
    public int sprintID { get; private set; }
    public int rollID { get; private set; }

    IEnumerator colliderSizeChangeCor;

    private PlayerStats playerStats;

    private void InitialCheck()
    {
        if (characterController == null)
            Debug.LogError("CharacterMove: CharacterController is not assigned. Movement will not function.");
        if (bodyTurnHandler == null)
            Debug.LogError("CharacterMove: BodyTurnHandler is not assigned. Body turning will not function.");
        if (animator == null)
            Debug.LogError("CharacterMove: Animator is not assigned. Animations will not function.");
        if (directionOrienter == null)
            Debug.LogError("CharacterMove: DirectionOrienter is not assigned. Movement orientation will be incorrect.");
        if (playerInputs == null)
            Debug.LogError("CharacterMove: PlayerInputs is not found. Input will not be processed.");
    }

    private void Awake()
    {
        AssighAnimatorIDs();
        colliderSizeChangeCor = ColliderSizeChangeSmooth(false);
        characterController = GetComponent<CharacterController>();
        normalColliderHeight = characterController.height;
        GameManager.Instance.TryGetComponent<PlayerInputs>(out playerInputs);
        playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnStatsChanged += UpdateMoveSpeeds;
            UpdateMoveSpeeds(); // 초기 속도 설정
        }

        InitialCheck();

        moveState = new MoveState(this);
        crouchState = new CrouchState(this);
        rollState = new RollState(this);
        jumpState = new JumpState(this);
        inAirState = new InAirState(this);
        flyState = new FlyState(this);
    }

    private void Start()
    {
        currentState = moveState;
        SetState(inAirState);
    }

    void OnDestroy()
    {
        moveState.OnDestroy();
        flyState.OnDestroy();
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
    public void UpdateMoveSpeeds()
    {
        if (playerStats == null) return;

        walkSpeed = playerStats.CurrentWalkSpeed;
        sprintSpeed = playerStats.CurrentSprintSpeed;
        crouchSpeed = playerStats.CurrentCrouchSpeed;

    }

    private void Update()
    {
        if (currentState == null)
            return;

        GroundCheck();

        currentState.Tick();
    }

    void GroundCheck()
    {
        RaycastHit hitInfo;

        if (velocity.y <= 0 && Physics.SphereCast(transform.position + characterController.center, characterController.radius + characterController.skinWidth, Vector3.down, out hitInfo, grounCheckDistance, groundCheckMask, QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
            groundNormal = hitInfo.normal;
            Vector3 relativeHitPoint = hitInfo.point - (transform.position + Vector3.right * characterController.center.x + Vector3.forward * characterController.center.z);

            Debug.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * 0.3f, Color.red);

            if (characterController.velocity.y < 0 && relativeHitPoint.magnitude > noSlipDistance && !Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f, groundCheckMask))
            {
                Vector3 edgeFallMovement = transform.position - hitInfo.point;
                edgeFallMovement.y = 0;
                edgeSlipVelocity += edgeFallMovement * Time.deltaTime * edgeFallMoveForce;
            }
            else
            {
                edgeSlipVelocity = Vector3.zero;
            }
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
            edgeSlipVelocity = Vector3.zero;
        }
    }

    [ContextMenu("Fly Mode")]
    public void Fly()
    {
        EnterFlyMode(true);
    }

    [ContextMenu("UnFly Mode")]
    public void UnFly()
    {
        EnterFlyMode(false);
    }

    public void EnterFlyMode(bool enter)
    {
        if (enter)
        {
            SetState(flyState);
        }
        else
        {
            // Fallback to grounded or in-air state
            SetState(isGrounded ? moveState : inAirState);
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

    public void StopAllActions()
    {
        // 1. 모든 입력 기반 속도를 0으로 만듭니다.
        moveVelocity = Vector3.zero;
        rollVelocity = Vector3.zero;

        // 2. 땅에 있다면 Y축 속도(중력)도 초기화합니다.
        if (isGrounded)
        {
            velocity = new Vector3(0, -2f, 0);
        }
        // (공중에 있다면 Y축 속도는 유지해서 계속 떨어지게 합니다)

        // 3. 애니메이터를 'Idle' 상태로 되돌립니다.
        if (animator != null)
        {
            animator.SetFloat(horizontalInputID, 0f);
            animator.SetFloat(verticalInputID, 0f);
            animator.SetBool(sprintID, false);
            animator.SetBool(rollID, false);
            animator.SetBool(walkID, false);
        }

        // 4. 현재 상태(점프 중, 구르기 중)를 강제로 기본 상태로 되돌립니다.
        if (isGrounded)
        {
            if (currentState != crouchState) // 웅크린 상태가 아니라면
            {
                SetState(moveState); // 기본 이동 상태로
            }
        }
        else
        {
            if (currentState != inAirState) // 공중 상태가 아니라면
            {
                SetState(inAirState); // 공중 상태로
            }
        }

        // 5. 몸 회전(Turn)을 멈춥니다.
        if (bodyTurnHandler != null)
        {
            bodyTurnHandler.momentaryTurn = false;
        }
    }
}

