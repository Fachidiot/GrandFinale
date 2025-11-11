using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class NavMeshMovement : MonoBehaviour, IMonsterMovement
{
    private NavMeshAgent agent;
    private Animator animator;

    // AI가 요청한 '목표 속도'를 저장할 변수
    private float targetSpeed;

    [Header("--- 가속/감속 설정 ---")]
    [Tooltip("속도가 0에서 최대로 오르는 데 걸리는 시간 (예: 5 = 초당 5의 속도만큼 증가)")]
    [SerializeField] private float accelerationRate = 5f;
    [Tooltip("속도가 최대에서 0으로 떨어지는 데 걸리는 시간 (보통 가속보다 빠름)")]
    [SerializeField] private float decelerationRate = 10f;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.updateRotation = true;
    }

    private void Update()
    {
        // 1. 현재 속도와 목표 속도를 가져옵니다.
        float currentSpeed = agent.speed;

        // 2. 목표 속도에 도달하기 위해 이번 프레임에 사용할 가/감속도를 정합니다.
        // (목표가 더 크면 가속, 작으면 감속)
        float rate = (currentSpeed < targetSpeed) ? accelerationRate : decelerationRate;

        // 3. 현재 속도를 목표 속도를 향해 부드럽게 이동시킵니다.
        // MoveTowards는 Lerp보다 일정한 속도를 보장해줍니다.
        agent.speed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * rate);


        // 에이전트의 '실제' 속도를 애니메이터에 전달합니다.
        // (0 -> 1.5 -> 3.2 -> 5.0 처럼 부드럽게 변하는 값이 들어감)
        //animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    // AI에게 받은 최종 목적지를 NavMeshAgent에 설정
    public void Move(Vector3 destination, float speed)
    {
        // agent.speed를 직접 설정하는 대신, 'targetSpeed' 변수에 저장합니다.
        this.targetSpeed = speed;

        agent.SetDestination(destination);
    }

    public void TurnTowards(Vector3 worldTargetPosition, float turnSpeed)
    {
        // (제자리 회전용이므로 수정 X)
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
        // Update() 함수가 알아서 속도를 0까지 부드럽게 줄여줄 겁니다.
        this.targetSpeed = 0f;
    }
}