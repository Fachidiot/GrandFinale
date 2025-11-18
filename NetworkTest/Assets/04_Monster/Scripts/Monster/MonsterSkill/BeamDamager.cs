using UnityEngine;
using System.Collections.Generic; // Dictionary를 사용하기 위해 추가

public class BeamDamager : MonoBehaviour
{
    [Header("공격 설정")]
    [Tooltip("초당 입히는 데미지")]
    public float damagePerSecond = 10f;

    [Tooltip("데미지가 실제로 적용되는 주기 (초) (0.25 = 초당 4번)")]
    public float damageTickRate = 0.25f;

    // 피해를 입힌 플레이어와 다음 피해 시간 저장용
    private Dictionary<Collider, float> targetsHit = new Dictionary<Collider, float>();

    /// <summary>
    /// 빔의 콜라이더 안에 누군가 '머무르는 동안' 매 프레임 호출됩니다.
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        // 1. 플레이어 태그인지 확인
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // 2. 이 플레이어를 이미 때렸는지, 쿨다운이 지났는지 확인
        if (targetsHit.ContainsKey(other))
        {
            if (Time.time < targetsHit[other])
            {
                return;
            }
        }

        if (other.TryGetComponent<PlayerStats>(out var playerStats))
        {
            // 4. 데미지 계산
            float damage = damagePerSecond * damageTickRate;

            // 5. (수정 완료) PlayerStats의 TakeDamage 함수 호출
            playerStats.TakeDamage(damage);

            // 6. 다음 데미지 시간 기록 (지금 시간 + 쿨다운)
            targetsHit[other] = Time.time + damageTickRate;
        }
    }

    /// <summary>
    /// 플레이어가 빔 범위에서 나갔을 때 호출됩니다.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // 7. 쿨다운 목록에서 제거 (다음에 들어오면 즉시 맞도록)
        if (targetsHit.ContainsKey(other))
        {
            targetsHit.Remove(other);
        }
    }
}