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

    [Tooltip("이 거리 안에서는 시야각 무시하고 360도 감지 (근접 감지)")]
    public float proximityRange = 3.0f;

    public bool CanSeePlayer { get; private set; }
    public Vector3 TargetLastPosition { get; private set; }

    private GameObject player;
    private MonsterHealth health;
    private WaitForSeconds checkDelay = new WaitForSeconds(0.2f);

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        health = GetComponent<MonsterHealth>();
        StartCoroutine(CheckFovRoutine());
    }

    private IEnumerator CheckFovRoutine()
    {
        while (true)
        {
            // 죽었으면 센서 끄기
            if (health != null && health.IsDead) yield break;

            CheckFov();
            yield return checkDelay;
        }
    }

    private void CheckFov()
    {
        if (player == null) return;

        bool isDetected = false;
        float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // 1. 근접 감지 (가까우면 등 뒤에 있어도 감지)
        if (distToPlayer <= proximityRange)
        {
            isDetected = true;
            TargetLastPosition = player.transform.position;
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
                // 장애물 체크
                if (!Physics.Raycast(eyePos, dirToTarget, distToPlayer, obstructionMask))
                {
                    isDetected = true;
                    TargetLastPosition = player.transform.position;
                }
            }
        }

        // 감지 성공 시 로그 출력 및 데이터 갱신
        if (isDetected)
        {
            Debug.Log("<color=cyan>캬캬캬 플레이어 발견! 캬캬캬</color>");
        }

        CanSeePlayer = isDetected;
    }

    public void ForceDetection(Vector3 targetPos)
    {
        CanSeePlayer = true;
        TargetLastPosition = targetPos;
        Debug.LogWarning("강제 감지 발동! (캬캬캬)");
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
    }
}