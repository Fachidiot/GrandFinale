using System;
using UnityEngine;

// UnitStats(수치 데이터)를 보유한 유닛의 "체력/피격" 어댑터.
// 공격/스킬은 무조건 IDamageable만 타도록 하고, 내부에서 UnitStats의 HP를 깎는다.
[RequireComponent(typeof(UnitStats))]
[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [Header("Options")]
    [Tooltip("무적 (디버그/보호막 등)")]
    [SerializeField] private bool invincible = false;

    [Tooltip("피격 시, 추가 이펙트/사운드 등을 여기서 호출해도 됨")]
    [SerializeField] private bool debugLogOnHit = false;

    public event Action<float> OnDamaged;     // 받은 피해량
    public event Action<float> OnHealed;      // 회복량
    public event Action OnDied;               // 사망

    private UnitStats stats;

    private void Awake()
    {
        stats = GetComponent<UnitStats>();
        if (stats == null)
            Debug.LogError("[Health] UnitStats가 필요합니다.");
    }

    public bool IsDead => stats != null && stats.IsDead;
    public float CurrentHP => stats != null ? stats.CurrentHP : 0f;
    public float MaxHP => stats != null ? stats.MaxHP : 0f;

    // === IDamageable 구현 ===름/파라미
    public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, GameObject instigator)
    {
        if (stats == null || invincible || stats.IsDead) return;

        float before = stats.CurrentHP;
        stats.TakeDamage(amount); // ← UnitStats 내부 HP 감소
        float taken = Mathf.Clamp(before - stats.CurrentHP, 0f, amount);

        if (debugLogOnHit)
            Debug.Log($"[Health:{name}] -{taken} (by {instigator?.name ?? "unknown"}) HP:{stats.CurrentHP}/{stats.MaxHP}");

        OnDamaged?.Invoke(taken);

        if (stats.IsDead)
        {
            // TODO: 여기서 FSM DeathState 전환 트리거 하거나, Ragdoll/Disable 등
            OnDied?.Invoke();
        }
    }

    // 편의 메서드(외부에서 직접 힐 줄 때)
    public void Heal(float amount)
    {
        if (stats == null || stats.IsDead) return;
        float before = stats.CurrentHP;
        stats.Heal(amount);
        float healed = Mathf.Clamp(stats.CurrentHP - before, 0f, amount);
        OnHealed?.Invoke(healed);
    }
}
