// FloatingMovement.cs (수정된 최종본)

using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
[RequireComponent(typeof(MonsterAIController))]
public class FloatingMovement : MonoBehaviour, IMonsterMovement
{
    private Rigidbody rb;
    private Animator animator;
    private MonsterAIController controller;

    private Vector3 targetDestination;
    private float targetSpeed;
    private float turnSpeed;
    private float currentSpeed = 0f;

    // ★ 1. (추가) 회전 목표를 독립적으로 저장합니다.
    private Quaternion targetRotation;

    [Header("--- 가속/감속 설정 ---")]
    [SerializeField] private float accelerationRate = 5f;
    [SerializeField] private float decelerationRate = 10f;

    private int animSpeedHash;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        controller = GetComponent<MonsterAIController>();

        rb.useGravity = false;

        // ★ 1. (수정) Rigidbody를 Kinematic으로 변경합니다.
        // 이렇게 하면 외부 물리력(총알)에 밀려나지 않고,
        // rb.MovePosition()이 안정적으로 작동합니다.
        rb.isKinematic = true;

        // ★ 2. (원본 복귀) 회전만 고정합니다.
        // (isKinematic = true이므로 FreezePosition은 더 이상 필요 없습니다.)
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // ★ 3. (수정) 초기 회전값을 현재 방향으로 설정
        targetDestination = transform.position;
        targetRotation = transform.rotation;
        targetSpeed = 0f;
    }
    private void Start()
    {
        if (controller.animConfig != null)
        {
            this.animSpeedHash = controller.hashMoveSpeed;
        }
        else
        {
            Debug.LogError("FloatingMovement: animConfig가 없습니다!");
        }

        // (FSM이 시작하기 전에 초기화)
        if (animator != null && this.animSpeedHash != 0)
        {
            animator.SetFloat(this.animSpeedHash, 0f);
        }
    }

    private void FixedUpdate()
    {
        // --- 1. 속도 계산 ---
        float rate = (currentSpeed < targetSpeed) ? accelerationRate : decelerationRate;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.fixedDeltaTime * rate);

        // --- 2. 이동 처리 (물리) ---
        if (currentSpeed > 0.01f)
        {
            Vector3 direction = (targetDestination - rb.position).normalized;
            rb.MovePosition(rb.position + direction * currentSpeed * Time.fixedDeltaTime);
        }

        // --- 3. 회전 처리 (물리) ★★★
        // (속도와 관계없이 항상 부드럽게 목표 지점을 바라봅니다)
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));

        // --- 4. 애니메이션 처리 ---
        // (속도와 관계없이 항상 현재 속도를 전달합니다)
        if (animator != null && this.animSpeedHash != 0)
        {
            animator.SetFloat(this.animSpeedHash, currentSpeed);
        }
    }

    // Move 함수: 이동 방향으로 targetRotation을 설정
    public void Move(Vector3 destination, float speed)
    {
        this.targetDestination = destination;
        this.targetSpeed = speed;
        this.turnSpeed = controller.config.turnSpeed;

        // ★ 4. (수정) 이동 방향을 새로운 '회전 목표'로 설정
        Vector3 direction = (destination - rb.position).normalized;
        if (direction != Vector3.zero)
        {
            this.targetRotation = Quaternion.LookRotation(direction);
        }
    }

    // TurnTowards 함수: LookAt() 호출 시, 특정 방향으로 targetRotation을 강제 설정
    public void TurnTowards(Vector3 worldTargetPosition, float turnSpeed)
    {
        this.turnSpeed = turnSpeed;

        // ★ 5. (수정) 바라볼 방향을 '회전 목표'로 설정
        Vector3 direction = (worldTargetPosition - rb.position).normalized;
        if (direction != Vector3.zero)
        {
            // (이전 코드의 x=0, z=0 제한을 제거하여 3D로 자유롭게 바라보게 함)
            this.targetRotation = Quaternion.LookRotation(direction);
        }
    }

    // Stop 함수: 속도만 0으로 줄임 (회전 목표는 그대로 둠)
    public void Stop()
    {
        this.targetSpeed = 0f;
    }
}