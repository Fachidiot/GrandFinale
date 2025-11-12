// Assets/Scripts/Player/PlayerStats.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // 'Action' 이벤트를 사용하기 위함

public class PlayerStats : MonoBehaviour
{
    [Header("기본 능력치 (Base Stats)")]
    public float baseWalkSpeed = 2f;
    public float baseRunSpeed = 3f;
    public float baseSprintSpeed = 5f;
    public float baseCooldownReduction = 0f;
    public float baseMaxHealth = 100f;
    public float baseDamageModifier = 1.0f;

    [Header("현재 상태 (실시간 디버그용)")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentMaxHealth;
    [SerializeField] private float currentShield;
    [SerializeField] private float currentWalkSpeed;
    [SerializeField] private float currentRunSpeed;
    [SerializeField] private float currentSprintSpeed;
    [SerializeField] private float currentDamageModifier;
    [SerializeField] private float currentCooldownReduction;


    public float CurrentHealth { get { return currentHealth; } private set { currentHealth = value; } }
    public float CurrentMaxHealth { get { return currentMaxHealth; } private set { currentMaxHealth = value; } }
    public float CurrentShield { get { return currentShield; } private set { currentShield = value; } }
    public float CurrentWalkSpeed { get { return currentWalkSpeed; } private set { currentWalkSpeed = value; } }
    public float CurrentRunSpeed { get { return currentRunSpeed; } private set { currentRunSpeed = value; } }
    public float CurrentSprintSpeed { get { return currentSprintSpeed; } private set { currentSprintSpeed = value; } }
    public float CurrentDamageModifier { get { return currentDamageModifier; } private set { currentDamageModifier = value; } }
    public float CurrentCooldownReduction { get { return currentCooldownReduction; } private set { currentCooldownReduction = value; } }
    // UI 업데이트를 위한 이벤트 (옵션)
    // 예: public event Action<string> OnStatChanged;

    void Awake()
    {
        ResetToBaseStats(); 
        CurrentHealth = CurrentMaxHealth; // 체력 꽉 채움
    }

    // ====================================================================
    // 1. 데미지 및 회복 처리 (기존 코드와 동일)
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

        Debug.Log($"데미지 {damage} 받음. 현재 체력: {CurrentHealth}, 현재 실드: {CurrentShield}"); //

        if (CurrentHealth <= 0) 
        {
            CurrentHealth = 0; 
            Die(); 
        }
    }

    public void Heal(float amount) 
    {
        CurrentHealth += amount; 
        if (CurrentHealth > CurrentMaxHealth) 
        {
            CurrentHealth = CurrentMaxHealth; 
        }
    }

    private void Die() 
    {
        Debug.Log("플레이어가 사망했습니다."); 
    }

    // ====================================================================
    // 2. 스탯 적용 (AbilityManager가 호출)
    // ====================================================================
    /// <summary>
    /// 모든 '현재 스탯'을 '기본 스탯'으로 되돌립니다.
    /// (AbilityManager가 스탯 재계산 전 호출)
    /// </summary>
    public void ResetToBaseStats()
    {
        currentMaxHealth = baseMaxHealth;
        currentDamageModifier = baseDamageModifier;
        currentCooldownReduction = baseCooldownReduction;
        currentWalkSpeed = baseWalkSpeed;
        currentRunSpeed = baseRunSpeed;
        currentSprintSpeed = baseSprintSpeed;
    }

    // ★★★ (신규) 2. 체력 보정 함수 ★★★
    /// <summary>
    /// 스탯 재계산 후, 현재 체력이 최대 체력보다 높으면 최대 체력으로 맞춥니다.
    /// (예: 아이템을 버려서 최대 체력이 150 -> 100이 되었을 때)
    /// </summary>
    public void ValidateHealth()
    {
        if (CurrentHealth > CurrentMaxHealth)
        {
            CurrentHealth = CurrentMaxHealth;
        }
    }

    /// <summary>
    /// (ABIL_001) 합연산 스탯을 적용합니다. (예: 최대 체력 +50)
    /// </summary>
    public void AddStat(string statName, float value) 
    {
        switch (statName)
        {
            case "MaxHealth":
                CurrentMaxHealth += value; 
                CurrentHealth += value; 
                Debug.Log($"MaxHealth 증가: +{value} (총 {CurrentMaxHealth})"); 
                break;
            case "BaseDamage":
                Debug.Log($"BaseDamage 증가: +{value} (구현 필요)"); 
                break;
        }
    }


    /// <summary>
    /// (ABIL_002) 곱연산(%) 스탯을 적용합니다. (예: 이동 속도 +10%)
    /// </summary>
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
                Debug.Log($"모든 MoveSpeed 증가: +{value}% (현재 스프린트: {CurrentSprintSpeed})");
                break;

            case "CooldownReduction":
                CurrentCooldownReduction += value;
                Debug.Log($"CooldownReduction 증가: +{value}% (총 {CurrentCooldownReduction}%)");
                break;
        }
    }

    /// <summary>
    /// (ABIL_006) 지정된 시간 동안 실드를 추가합니다.
    /// </summary>
    public void AddTemporaryShield(float amount, float duration) 
    {
        StartCoroutine(ShieldRoutine(amount, duration)); 
    }

    private IEnumerator ShieldRoutine(float amount, float duration) 
    {
        CurrentShield += amount; 
        Debug.Log($"실드 {amount} 획득! (총 {CurrentShield})"); 

        yield return new WaitForSeconds(duration); 

        CurrentShield -= amount; 
        if (CurrentShield < 0) CurrentShield = 0; 
        Debug.Log($"실드 {amount} 종료. (남은 실드 {CurrentShield})"); 
    }
}