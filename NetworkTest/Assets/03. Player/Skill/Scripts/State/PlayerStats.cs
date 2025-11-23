using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public event Action OnStatsChanged;

    [Header("기본 능력치 (Base Stats)")]
    public float baseWalkSpeed = 2f;
    public float baseRunSpeed = 3f;
    public float baseSprintSpeed = 5f;
    public float baseCooldownReduction = 0f;
    public float baseMaxHealth = 100f;
    public float baseDamageModifier = 1.0f;
    public float baseDefense = 10f;
    public float basePower = 10f;
    public float baseCritChance = 5.0f; // 기본 치명타 확률
    public int baseCurrency = 1500;    // 기본 재화

    [Header("현재 상태 (모니터링)")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentMaxHealth;
    [SerializeField] private float currentShield;
    [SerializeField] private float currentWalkSpeed;
    [SerializeField] private float currentRunSpeed;
    [SerializeField] private float currentSprintSpeed;
    [SerializeField] private float currentDamageModifier;
    [SerializeField] private float currentCooldownReduction;
    [SerializeField] private float currentDefense;
    [SerializeField] private float currentPower;
    [SerializeField] private float currentCritChance;
    [SerializeField] private int currentCurrency;

    // Public Properties
    public float CurrentHealth { get { return currentHealth; } private set { currentHealth = value; } }
    public float CurrentMaxHealth { get { return currentMaxHealth; } private set { currentMaxHealth = value; } }
    public float CurrentShield { get { return currentShield; } private set { currentShield = value; } }
    public float CurrentDefense { get { return currentDefense; } private set { currentDefense = value; } }
    public float CurrentPower { get { return currentPower; } private set { currentPower = value; } }
    public float CurrentCritChance { get { return currentCritChance; } private set { currentCritChance = value; } }
    public float CurrentWalkSpeed { get { return currentWalkSpeed; } private set { currentWalkSpeed = value; } }
    public float CurrentRunSpeed { get { return currentRunSpeed; } private set { currentRunSpeed = value; } }
    public float CurrentSprintSpeed { get { return currentSprintSpeed; } private set { currentSprintSpeed = value; } }
    public float CurrentDamageModifier { get { return currentDamageModifier; } private set { currentDamageModifier = value; } }
    public float CurrentCooldownReduction { get { return currentCooldownReduction; } private set { currentCooldownReduction = value; } }
    public int CurrentCurrency { get { return currentCurrency; } private set { currentCurrency = value; } }

    void Awake()
    {
        ResetToBaseStats();
        CurrentHealth = CurrentMaxHealth;
        currentCurrency = baseCurrency;
    }

    public void TakeDamage(float damage)
    {
        // 방어력 적용 공식 (예시: 방어력의 절반만큼 데미지 감소, 최소 1)
        float reducedDamage = Mathf.Max(1f, damage - (currentDefense * 0.5f));
        float damageToTake = reducedDamage;

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
        OnStatsChanged?.Invoke();
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > CurrentMaxHealth) CurrentHealth = CurrentMaxHealth;
        OnStatsChanged?.Invoke();
    }

    public void ValidateHealth()
    {
        if (CurrentHealth > CurrentMaxHealth)
        {
            CurrentHealth = CurrentMaxHealth;
        }
        OnStatsChanged?.Invoke();
    }

    private void Die()
    {
        Debug.Log("플레이어 사망");
        // 사망 처리 로직 추가 가능
    }


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
        currentCritChance = baseCritChance;

        OnStatsChanged?.Invoke();
    }

    // 장비 장착 해제 시 호출될 함수
    public void AddStat(string statName, float value)
    {
        switch (statName)
        {
            case "MaxHealth":
                CurrentMaxHealth += value;
                CurrentHealth += value; // 최대 체력 증가분만큼 현재 체력도 증가
                break;
            case "Defense":
                CurrentDefense += value;
                break;
            case "Power":
                CurrentPower += value;
                break;
            case "CritChance":
                CurrentCritChance += value;
                break;
            case "AllDamage":
                CurrentDamageModifier += (value / 100f);
                break;
        }
        // 스탯 변경 후 체력 검증 한 번 실행
        ValidateHealth();
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
        OnStatsChanged?.Invoke();
        yield return new WaitForSeconds(duration);
        CurrentShield -= amount;
        if (CurrentShield < 0) CurrentShield = 0;
        OnStatsChanged?.Invoke();
    }
}