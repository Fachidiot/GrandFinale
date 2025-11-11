// PoisonArea.cs (새 파일)
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 바닥에 깔리는 독 장판의 로직을 처리합니다.
/// - 5초 후 자동 파괴
/// - 범위 내의 대상에게 틱 데미지 적용
/// </summary>
public class PoisonArea : MonoBehaviour
{
    private float tickDamage;
    private float poisonDuration;
    private float poisonTickRate;

    // 장판 안에 들어와 있는 대상들 (중복 피해 방지)
    private readonly List<Collider> targetsInArea = new List<Collider>();

    /// <summary>
    /// 투사체로부터 설정값을 받아 로직을 시작합니다.
    /// </summary>
    public void Initialize(float damage, float duration, float tickRate)
    {
        this.tickDamage = damage;
        this.poisonDuration = duration;
        this.poisonTickRate = tickRate;

        // 5초 후 파괴 및 틱 데미지 코루틴 시작
        StartCoroutine(DestroyAfterDuration(poisonDuration));
        StartCoroutine(TickDamageRoutine(poisonTickRate));
    }

    // 독 장판 지속 시간 후 파괴
    private IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        // 독 장판이 사라지는 이펙트가 있다면 여기서 활성화/비활성화

        Destroy(gameObject);
        Debug.Log($"[PoisonArea] 독 장판 {duration}초 후 제거 완료.");
    }

    // 조건 2: 독 장판에 닿아있으면 틱 데미지 주기
    private IEnumerator TickDamageRoutine(float tickRate)
    {
        while (true)
        {
            yield return new WaitForSeconds(tickRate);

            // 장판 안에 있는 모든 대상에게 틱 데미지 적용
            for (int i = targetsInArea.Count - 1; i >= 0; i--)
            {
                Collider target = targetsInArea[i];
                if (target == null)
                {
                    targetsInArea.RemoveAt(i);
                    continue;
                }
                if (target.TryGetComponent<PlayerStats>(out var playerStats))
                {
                    playerStats.TakeDamage(tickDamage);
                    Debug.Log($"[PoisonArea] {target.name}에게 독 틱 피해 {tickDamage} 적용!");
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 태그 확인 및 리스트에 추가
        if (other.CompareTag("Player") && !targetsInArea.Contains(other))
        {
            targetsInArea.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어 태그 확인 및 리스트에서 제거
        if (other.CompareTag("Player") && targetsInArea.Contains(other))
        {
            targetsInArea.Remove(other);
        }
    }
}