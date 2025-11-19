using UnityEngine;
using System.Collections.Generic;

public class PoisonGroundDamager : MonoBehaviour
{
    [Header("독 피해 설정")]
    [Tooltip("총 지속 시간 (이 시간이 지나면 자신을 파괴합니다)")]
    public float duration = 5.0f;
    [Tooltip("데미지가 적용되는 주기")]
    public float damageTickRate = 0.3f;
    [Tooltip("틱당 입히는 데미지")]
    public float damagePerTick = 5.0f;

    // 피해를 입힌 플레이어와 다음 피해 시간 저장용
    private Dictionary<Collider, float> targetsHit =
        new Dictionary<Collider, float>();

    private float destroyTimer = 0f;

    private void Update()
    {
        destroyTimer += Time.deltaTime;
        if (destroyTimer >= duration)
        {
            // 지속 시간이 끝나면 독 바닥 제거
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 1. 플레이어 태그인지 확인
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // 2. 쿨다운 체크
        if (targetsHit.ContainsKey(other))
        {
            if (Time.time < targetsHit[other])
            {
                return;
            }
        }

        if (other.TryGetComponent<PlayerStats>(out var playerStats)) // <-- PlayerStats.cs 참조
        {
            // 4. 데미지 적용
            playerStats.TakeDamage(damagePerTick);

            // 5. 다음 데미지 시간 기록
            targetsHit[other] = Time.time + damageTickRate;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 6. 플레이어가 나가면 쿨다운 목록에서 제거
        if (targetsHit.ContainsKey(other))
        {
            targetsHit.Remove(other);
        }
    }
}