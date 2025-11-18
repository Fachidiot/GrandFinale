using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // 'Action' 이벤트를 사용하기 위함

public class PlayerStats : MonoBehaviour
{
    // [★수정★] 스탯이 변경될 때마다 UI 매니저에게 알리기 위한 이벤트
    public event Action OnStatsChanged;

    [Header("기본 능력치 (Base Stats)")]
    public float baseWalkSpeed = 2f;
    public float baseRunSpeed = 3f;
    public float baseSprintSpeed = 5f;
    public float baseCooldownReduction = 0f;
    public float baseMaxHealth = 100f;
    public float baseDamageModifier = 1.0f;
    public float baseDefense = 10f;  // [★신규★] 기본 방어력
    public float basePower = 10f;    // [★신규★] 기본 파워
    public int baseLevel = 1;        // [★신규★] 기본 레벨
    public int baseCurrency = 22222; // [★신규★] 기본 재화

    [Header("현재 상태 (실시간 디버그용)")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentMaxHealth;
    [SerializeField] private float currentShield;
    [SerializeField] private float currentWalkSpeed;
    [SerializeField] private float currentRunSpeed;
    [SerializeField] private float currentSprintSpeed;
    [SerializeField] private float currentDamageModifier;
    [SerializeField] private float currentCooldownReduction;
    [SerializeField] private float currentDefense; // [★신규★]
    [SerializeField] private float currentPower;   // [★신규★]
    [SerializeField] private int currentLevel;     // [★신규★]
    [SerializeField] private int currentCurrency;  // [★신규★]

    // Public Properties (UI 및 다른 스크립트가 접근용)
    public float CurrentHealth { get { return currentHealth; } private set { currentHealth = value; } }
    public float CurrentMaxHealth { get { return currentMaxHealth; } private set { currentMaxHealth = value; } }
    public float CurrentShield { get { return currentShield; } private set { currentShield = value; } }
    public float CurrentWalkSpeed { get { return currentWalkSpeed; } private set { currentWalkSpeed = value; } }
    public float CurrentRunSpeed { get { return currentRunSpeed; } private set { currentRunSpeed = value; } }
    public float CurrentSprintSpeed { get { return currentSprintSpeed; } private set { currentSprintSpeed = value; } }
    public float CurrentDamageModifier { get { return currentDamageModifier; } private set { currentDamageModifier = value; } }
    public float CurrentCooldownReduction { get { return currentCooldownReduction; } private set { currentCooldownReduction = value; } }
    public float CurrentDefense { get { return currentDefense; } private set { currentDefense = value; } } // [★신규★]
    public float CurrentPower { get { return currentPower; } private set { currentPower = value; } }     // [★신규★]
    public int CurrentLevel { get { return currentLevel; } private set { currentLevel = value; } }     // [★신규★]
    public int CurrentCurrency { get { return currentCurrency; } private set { currentCurrency = value; } } // [★신규★]


    void Awake()
    {
        ResetToBaseStats();
        CurrentHealth = CurrentMaxHealth;
        currentCurrency = baseCurrency; // [★신규★] 기본 재화로 시작
    }

    // ====================================================================
    // 1. 데미지 및 회복 처리
    // ====================================================================

    public void TakeDamage(float damage)
    {
        float damageToTake = damage;

        if (CurrentShield > 0)
        {
            if (CurrentShield >= damageToTake)
            {
                CurrentShield -= damageToTake;
                damageToTake = 0;
            }
            else
            {
                damageToTake -= CurrentShield;
                CurrentShield = 0;
            }
        }

        if (damageToTake > 0)
        {
            CurrentHealth -= damageToTake;
        }

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }

        // [★수정★] UI 업데이트 신호 보내기
        OnStatsChanged?.Invoke();
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > CurrentMaxHealth)
        {
            CurrentHealth = CurrentMaxHealth;
        }
        // [★수정★] UI 업데이트 신호 보내기
        OnStatsChanged?.Invoke();
    }

    private void Die()
    {
        Debug.Log("플레이어가 사망했습니다.");
    }

    // ====================================================================
    // 2. [★신규★] 재화 관리
    // ====================================================================

    public void AddCurrency(int amount)
    {
        currentCurrency += amount;
        OnStatsChanged?.Invoke();
    }

    public bool SpendCurrency(int amount)
    {
        if (currentCurrency >= amount)
        {
            currentCurrency -= amount;
            OnStatsChanged?.Invoke();
            return true;
        }
        return false;
    }


    // ====================================================================
    // 3. 스탯 적용 (AbilityManager가 호출)
    // ====================================================================

    public void ResetToBaseStats()
    {
        currentMaxHealth = baseMaxHealth;
        currentDamageModifier = baseDamageModifier;
        currentCooldownReduction = baseCooldownReduction;
        currentWalkSpeed = baseWalkSpeed;
        currentRunSpeed = baseRunSpeed;
        currentSprintSpeed = baseSprintSpeed;
        currentDefense = baseDefense;
        currentPower = basePower;    
        currentLevel = baseLevel;    

        // [★수정★] UI 업데이트 신호 보내기
        OnStatsChanged?.Invoke();
    }

    public void ValidateHealth()
    {
        if (CurrentHealth > CurrentMaxHealth)
        {
            CurrentHealth = CurrentMaxHealth;
        }
        // [★수정★] UI 업데이트 신호 보내기
        OnStatsChanged?.Invoke();
    }

    public void AddStat(string statName, float value)
    {
        switch (statName)
        {
            case "MaxHealth":
                CurrentMaxHealth += value;
                CurrentHealth += value;
                break;
            case "Defense":
                CurrentDefense += value; // [★신규★]
                break;
            case "Power":
                CurrentPower += value;   // [★신규★]
                break;
        }
        // [★수정★] UI 업데이트 신호 보내기
        OnStatsChanged?.Invoke();
    }

    public void AddStatPercent(string statName, float value)
    {
        switch (statName)
        {
            case "MoveSpeed":
                float walkBonus = baseWalkSpeed * (value / 100.0f);
                float runBonus = baseRunSpeed * (value / 100.0f);
                float sprintBonus = baseSprintSpeed * (value / 100.0f);
                CurrentWalkSpeed += walkBonus;
                CurrentRunSpeed += runBonus;
                CurrentSprintSpeed += sprintBonus;
                break;

            case "CooldownReduction":
                CurrentCooldownReduction += value;
                break;
        }
        OnStatsChanged?.Invoke();
    }

    public void AddTemporaryShield(float amount, float duration)
    {
        StartCoroutine(ShieldRoutine(amount, duration));
    }

    private IEnumerator ShieldRoutine(float amount, float duration)
    {
        CurrentShield += amount;
        OnStatsChanged?.Invoke(); // 실드 변경도 UI에 알림 (필요시)

        yield return new WaitForSeconds(duration);

        CurrentShield -= amount;
        if (CurrentShield < 0) CurrentShield = 0;
        OnStatsChanged?.Invoke(); // 실드 변경도 UI에 알림 (필요시)
    }
}