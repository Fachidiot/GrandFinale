using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MonsterSensor : MonoBehaviour
{
    [Header("감지 설정")]
    public MonsterConfig config;
    public LayerMask targetMask; // Should be the Player layer
    public LayerMask obstructionMask;

    [Tooltip("레이캐스트를 시작할 '눈' 높이")]
    public float eyeHeight = 1.5f;

    [Tooltip("이 거리 안에서는 시야각 무시하고 360도 감지 (근접/청각 감지)")]
    public float proximityRange = 3.0f;

    [Tooltip("강제 감지(피격 등) 시 유지되는 기억 시간")]
    public float memoryDuration = 3.0f;

    // --- 감지 결과 프로퍼티 ---
    public bool CanSeePlayer { get; private set; }
    public Vector3 TargetLastPosition { get; private set; }
    public GameObject Target { get; private set; }

    // --- 내부 참조 ---
    private MonsterHealth health;
    private WaitForSeconds checkDelay = new WaitForSeconds(0.2f);
    private float currentMemoryTime = 0f;
    private bool isHost;

    private void Start()
    {
        isHost = NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.Host;
        health = GetComponent<MonsterHealth>();

        // 센서 로직은 오직 호스트에서만 실행됩니다.
        if (!isHost)
        {
            this.enabled = false;
            return;
        }

        StartCoroutine(CheckFovRoutine());
    }

    private void Update()
    {
        // 호스트에서만 기억 시간을 관리합니다.
        if (!isHost) return;

        if (currentMemoryTime > 0)
        {
            currentMemoryTime -= Time.deltaTime;
        }
    }

    private IEnumerator CheckFovRoutine()
    {
        while (true)
        {
            if (health != null && health.IsDead)
            {
                // 몬스터가 죽으면 타겟을 초기화하고 루틴을 종료합니다.
                ClearTarget();
                yield break;
            }

            FindBestTarget();
            yield return checkDelay;
        }
    }

    private void FindBestTarget()
    {
        // PlayerManager에 접근할 수 없으면 탐색을 중단합니다.
        if (PlayerManager.Instance == null) return;

        // PlayerManager.Instance.Players는 Dictionary 형태일 가능성이 높으므로, .Values로 접근합니다.
        var players = PlayerManager.Instance.Players.Values;
        if (players == null || players.Count() == 0)
        {
            ClearTarget();
            return;
        }

        GameObject bestTarget = null;
        float minDistanceSqr = float.MaxValue;

        // 모든 플레이어를 순회하며 최고의 타겟을 찾습니다.
        foreach (var player in players)
        {
            // IPlayerControllable 인터페이스는 gameObject 프로퍼티를 가집니다.
            if (player.gameObject == null) continue;

            Vector3 playerPosition = player.gameObject.transform.position;
            float distSqr = (transform.position - playerPosition).sqrMagnitude;

            // 이미 더 가까운 타겟이 있다면 건너뜁니다.
            if (distSqr > minDistanceSqr) continue;

            // 시야 검사를 통과하는지 확인합니다.
            if (IsPlayerInSight(player.gameObject))
            {
                minDistanceSqr = distSqr;
                bestTarget = player.gameObject;
            }
        }

        // 결과 처리
        if (bestTarget != null)
        {
            // 새로운 타겟을 감지했습니다.
            currentMemoryTime = 0f; // 실제로 봤으므로 기억 시간 리셋
            SetTarget(bestTarget);
        }
        else
        {
            // 물리적으로 보이는 타겟이 없습니다.
            // 기억 시간이 남아있고, 기존 타겟이 유효하다면 타겟을 유지합니다.
            if (currentMemoryTime > 0 && Target != null)
            {
                // 타겟이 비활성화되거나 파괴되었는지 확인합니다.
                if (Target.activeInHierarchy)
                {
                    // 기억에 의존하여 타겟 위치를 계속 업데이트합니다.
                    TargetLastPosition = Target.transform.position;
                }
                else
                {
                    // 기억에 의존하던 타겟이 사라졌으므로 초기화합니다.
                    ClearTarget();
                }
            }
            else
            {
                // 기억 시간도 없고, 보이는 플레이어도 없으므로 타겟을 완전히 잃습니다.
                ClearTarget();
            }
        }
    }

    private bool IsPlayerInSight(GameObject player)
    {
        if (player == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 playerPos = player.transform.position;
        float distToPlayer = Vector3.Distance(transform.position, playerPos);

        // 1. 근접 감지 (360도)
        if (distToPlayer <= proximityRange)
        {
            // 근접 범위에서는 장애물만 체크합니다.
            if (!Physics.Raycast(eyePos, (playerPos - eyePos).normalized, distToPlayer, obstructionMask))
            {
                return true;
            }
        }

        // 2. 시야각 감지 (FOV)
        if (distToPlayer <= config.fovRange)
        {
            Vector3 dirToTarget = (playerPos - eyePos).normalized;

            // y축을 무시한 2D 각도 계산
            Vector3 forward2D = transform.forward;
            forward2D.y = 0;
            Vector3 targetDir2D = dirToTarget;
            targetDir2D.y = 0;

            if (Vector3.Angle(forward2D.normalized, targetDir2D.normalized) < config.fovAngle / 2)
            {
                // 시야각 내에 있고, 장애물이 없는지 최종 확인합니다.
                if (!Physics.Raycast(eyePos, dirToTarget, distToPlayer, obstructionMask))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 피격 시 외부에서 호출되어 타겟을 강제로 설정하고 기억 시간을 부여합니다.
    /// </summary>
    public void ForceDetection(GameObject attacker)
    {
        if (!isHost || attacker == null) return;

        // 공격자가 유효한 플레이어인지 확인합니다. (옵션)
        if (attacker.TryGetComponent<IPlayerControllable>(out _))
        {
            SetTarget(attacker);
            currentMemoryTime = memoryDuration; // 기억 시간 충전
        }
    }
    
    private void SetTarget(GameObject newTarget)
    {
        Target = newTarget;
        CanSeePlayer = true;
        if(Target != null)
        {
            TargetLastPosition = Target.transform.position;
        }
    }

    private void ClearTarget()
    {
        Target = null;
        CanSeePlayer = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (config == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, config.fovRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, proximityRange);

        Vector3 fovLine1 = Quaternion.AngleAxis(config.fovAngle / 2, transform.up) * transform.forward * config.fovRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-config.fovAngle / 2, transform.up) * transform.forward * config.fovRange;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        if (Target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, Target.transform.position);
        }
    }
}