// MonsterSensor.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 몬스터의 시야 감지(FOV)와 관련된 로직을 처리하는 스크립트입니다.
/// </summary>
public class MonsterSensor : MonoBehaviour
{
    [Header("감지 설정")]
    public MonsterConfig config;
    public LayerMask targetMask;
    public LayerMask obstructionMask;

    [Tooltip("레이캐스트를 시작할 '눈' 높이입니다. (지형 높낮이 보정)")]
    public float eyeHeight = 1.5f;

    // 감지 결과를 저장하는 프로퍼티
    public bool CanSeePlayer { get; private set; }
    public Vector3 TargetLastPosition { get; private set; }

    private GameObject player;
    private WaitForSeconds checkDelay = new WaitForSeconds(0.2f);

    // private void Awake()
    // {
    //     player = GameObject.FindGameObjectWithTag("Player");
    // }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        // 스크립트가 활성화되면 감지 루틴을 시작합니다.
        StartCoroutine(CheckFovRoutine());
    }

    private IEnumerator CheckFovRoutine()
    {
        while (true)
        {
            CheckFov();
            yield return checkDelay;
        }
    }
    // MonsterSensor.cs 스크립트의 CheckFov 함수를 아래 코드로 교체하세요.

    private void CheckFov()
    {
        if (player == null) return;

        bool playerDetected = false;

        // 1. 거리 체크 (기존과 동일)
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, config.fovRange, targetMask);

        if (rangeChecks.Length > 0)
        {
            Transform target = rangeChecks[0].transform;

            // 2. '눈' 위치 계산 (기존과 동일)
            Vector3 eyePosition = transform.position + transform.up * eyeHeight;
            Vector3 directionToTarget = (target.position - eyePosition).normalized;

            // (어떻게) 몬스터의 정면 벡터에서 Y(높이) 값을 0으로 만듭니다.
            Vector3 monsterForward_2D = transform.forward;
            monsterForward_2D.y = 0;

            Vector3 directionToTarget_2D = directionToTarget;
            directionToTarget_2D.y = 0;

            // (왜) 3D 각도(Vector3.Angle) 대신, Y값이 제거된 2D 벡터로 계산합니다.
            //     이렇게 해야 몬스터가 3차원 지형의 위나 아래를 볼 때 각도를 정확히 체크합니다.
            float angle = Vector3.Angle(monsterForward_2D, directionToTarget_2D);

            if (angle < config.fovAngle / 2)
            {
                // 4. 장애물 체크 (기존과 동일)
                // (왜) 장애물(벽, 기둥)은 3D로 체크해야 하므로,
                //     원래의 3D 방향(directionToTarget)을 그대로 사용합니다.
                float distanceToTarget = Vector3.Distance(eyePosition, target.position);

                if (Physics.Raycast(eyePosition, directionToTarget, out RaycastHit hit, distanceToTarget, obstructionMask))
                {
                    Debug.LogWarning($"<color=red>3. 장애물 감지:</color> 시야가 '{hit.collider.name}'에 가려졌습니다!");
                }
                else
                {
                    playerDetected = true;
                    TargetLastPosition = target.position;
                    Debug.Log("<color=cyan>캬캬캬 플레이어 발견! 캬캬캬</color>");
                }
            }
        }

        CanSeePlayer = playerDetected;
    }

    /// <summary>
    /// 플레이어를 강제로 감지 상태로 만들고 위치를 저장합니다.
    /// (예: 피격 시)
    /// </summary>
    public void ForceDetection(Vector3 targetPosition)
    {
        CanSeePlayer = true;
        TargetLastPosition = targetPosition;
        Debug.LogWarning("강제 감지 발동!");
    }
    // 기즈모 로직 (AI 컨트롤러와 동일)
    private void OnDrawGizmosSelected()
    {
        if (config == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, config.fovRange);
        Vector3 fovLine1 = Quaternion.AngleAxis(config.fovAngle / 2, transform.up) * transform.forward * config.fovRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-config.fovAngle / 2, transform.up) * transform.forward * config.fovRange;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
    }
}