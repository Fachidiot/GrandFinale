using UnityEngine;
using System.Collections;

public class MonsterSensor : MonoBehaviour
{
    [Header("감지 설정")]
    public MonsterConfig config;
    public LayerMask targetMask;
    public LayerMask obstructionMask;

    [Tooltip("레이캐스트를 시작할 '눈' 높이")]
    public float eyeHeight = 1.5f;

    [Tooltip("이 거리 안에서는 시야각 무시하고 360도 감지 (근접/청각 감지)")]
    public float proximityRange = 3.0f;

    [Tooltip("강제 감지(피격 등) 시 유지되는 기억 시간")]
    public float memoryDuration = 3.0f; // 피격당하면 3초간은 안 보여도 쫓아옴

    // 감지 결과 프로퍼티
    public bool CanSeePlayer { get; private set; }
    public Vector3 TargetLastPosition { get; private set; }

    private GameObject player;
    private MonsterHealth health;
    private WaitForSeconds checkDelay = new WaitForSeconds(0.2f);

    // 남은 기억 시간 (피격 시 일정 시간 동안 추적 유지용)
    private float currentMemoryTime = 0f;

    private void Start()
    {
        // 싱글 플레이에선 여기서 찾아지지만, 멀티에선 못 찾을 수도 있음
        player = GameObject.FindGameObjectWithTag("Player");
        health = GetComponent<MonsterHealth>();
        StartCoroutine(CheckFovRoutine());
    }

    private void Update()
    {
        // 기억 시간 감소 (매 프레임)
        if (currentMemoryTime > 0)
        {
            currentMemoryTime -= Time.deltaTime;
        }
    }

    private IEnumerator CheckFovRoutine()
    {
        while (true)
        {
            // 죽었으면 센서 중지
            if (health != null && health.IsDead) yield break;

            CheckFov();
            yield return checkDelay;
        }
    }

    private void CheckFov()
    {
        // [네트워크 안전장치] 플레이어가 처음에 없었다면, 매번 다시 찾아본다.
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return; // 아직도 없으면 이번 턴은 패스
        }

        bool isPhysicallyDetected = false;
        float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // 1. 근접 감지 (등 뒤여도 감지)
        if (distToPlayer <= proximityRange)
        {
            isPhysicallyDetected = true;
        }
        // 2. 시야각 감지 (Config 범위 내)
        else if (distToPlayer <= config.fovRange)
        {
            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
            Vector3 dirToTarget = (player.transform.position - eyePos).normalized;

            Vector3 forward2D = transform.forward; forward2D.y = 0;
            Vector3 targetDir2D = dirToTarget; targetDir2D.y = 0;

            if (Vector3.Angle(forward2D, targetDir2D) < config.fovAngle / 2)
            {
                // 장애물(벽) 체크
                if (!Physics.Raycast(eyePos, dirToTarget, distToPlayer, obstructionMask))
                {
                    isPhysicallyDetected = true;
                }
            }
        }

        // 3. 결과 처리 (물리적 감지 OR 기억력)
        if (isPhysicallyDetected)
        {
            // 실제로 봤으니 기억 시간 리셋 (추적 모드 갱신)
            currentMemoryTime = 0f;

            CanSeePlayer = true;
            TargetLastPosition = player.transform.position;

             Debug.Log("<color=cyan>캬캬캬 플레이어 발견! 캬캬캬</color>");
        }
        else
        {
            // 물리적으로는 안 보이지만, 기억 시간(피격 버프)이 남아있다면 '감지 중'으로 처리
            if (currentMemoryTime > 0)
            {
                CanSeePlayer = true;
                // 기억 중일 때는 위치를 계속 갱신해줘야 벽 뒤로 숨어도 끝까지 쫓아감
                TargetLastPosition = player.transform.position;
            }
            else
            {
                CanSeePlayer = false;
            }
        }
    }

    /// <summary>
    /// 피격 시 호출: 일정 시간 동안 강제로 추적 모드 활성화
    /// </summary>
    public void ForceDetection(Vector3 targetPos)
    {
        // 강제 감지 시에도 플레이어 참조가 필요할 수 있으므로 안전장치
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        CanSeePlayer = true;
        TargetLastPosition = targetPos;
        currentMemoryTime = memoryDuration; // 기억 시간 충전
        Debug.LogWarning($"<color=orange>[Sensor] 피격 감지! {memoryDuration}초간 강제 추적.</color>");
    }

    private void OnDrawGizmosSelected()
    {
        if (config == null) return;

        // 시야각 (파랑)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, config.fovRange);

        // 근접 감지 (빨강)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, proximityRange);

        Vector3 fovLine1 = Quaternion.AngleAxis(config.fovAngle / 2, transform.up) * transform.forward * config.fovRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-config.fovAngle / 2, transform.up) * transform.forward * config.fovRange;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
    }
}