using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class NavMeshMovement : MonoBehaviour, IMonsterMovement
{
    private NavMeshAgent agent;
    private Animator animator;

    // AI가 요청한 '목표 속도'를 저장하는 변수
    private float targetSpeed;

    [Header("--- 가속/감속 설정 ---")]
    [Tooltip("속도가 0에서 최대로 오르는 데 걸리는 시간 (예: 5 = 초당 5의 속도만큼 가속)")]
    [SerializeField] private float accelerationRate = 5f;
    [Tooltip("속도가 최대에서 0으로 떨어지는 데 걸리는 시간 (가속 비율보다 높음)")]
    [SerializeField] private float decelerationRate = 10f;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.updateRotation = true;
    }

    private void Update()
    {
        // 1. 현재 속도를 목표 속도로 조절합니다.
        float currentSpeed = agent.speed;

        // 2. 목표 속도에 도달하기 위해 현재 가속도에 기반한 가/감속도를 구합니다.
        // (목표가 더 크면 가속, 작으면 감속)
        float rate = (currentSpeed < targetSpeed) ? accelerationRate : decelerationRate;

        // 3. 현재 속도를 목표 속도로 향해 부드럽게 이동시킵니다.
        // MoveTowards는 Lerp과 달리 일정한 속도로 변화합니다.
        agent.speed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * rate);


        // 에이전트의 '실제' 속도를 애니메이터에 전달합니다.
        // (0 -> 1.5 -> 3.2 -> 5.0 처럼 부드럽게 변하는 값이 들어감)
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    // AI로부터 이동 명령을 받으면 NavMeshAgent에 전달
    public void Move(Vector3 destination, float speed)
    {
        // agent.speed를 직접 바꾸는 대신, 'targetSpeed' 변수를 사용합니다.
        this.targetSpeed = speed;

        agent.SetDestination(destination);
    }

    public void TurnTowards(Vector3 worldTargetPosition, float turnSpeed)
    {
        // (제자리 회전이므로 이동 X)
        agent.SetDestination(transform.position);
        transform.LookAt(new Vector3(worldTargetPosition.x, transform.position.y, worldTargetPosition.z));
    }

    public void Stop()
    {
        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        // 멈추라는 명령은 'targetSpeed'를 0으로 설정합니다.
        // Update() 함수가 알아서 속도를 0으로 부드럽게 낮춰줄 겁니다.
        this.targetSpeed = 0f;
    }
}