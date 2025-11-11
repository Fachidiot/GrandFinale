using UnityEngine;

[RequireComponent(typeof(Collider))]
public class UnitStats : MonoBehaviour, ICombatStats
{
    [Header("레벨 및 체력")]
    [SerializeField] private int level = 1;
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;

    [Header("전투 스탯")]
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float attackSpeed = 1f;

    [Header("능력치")]
    [SerializeField] private int strength = 10;
    [SerializeField] private int dexterity = 10;
    [SerializeField] private int intelligence = 10;

   
    // ICombatStats 구현
    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0f;

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Clamp(currentHP - amount, 0f, maxHP);
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);
    }

    public void RestoreFullHP()
    {
        currentHP = maxHP;
    }

    // 유닛 능력치 접근용 프로퍼티
    public int Level => level;
    public float BaseDamage => baseDamage;
    public float AttackSpeed => attackSpeed;

    public int Strength => strength;
    public int Dexterity => dexterity;
    public int Intelligence => intelligence;
}
