using TMPro;
using UnityEngine;

/// <summary>
/// 인벤토리 스탯 표시 전담 컨트롤러
/// - 플레이어 스탯 UI 업데이트 (모든 스탯 포함)
/// </summary>
public class InventoryStatsUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Header Stats Display")]
    [SerializeField] private TMP_Text statsTextMidLeft;
    [SerializeField] private TMP_Text statsTextMidRight;
    [SerializeField] private TMP_Text cashText;

    [Header("Left Panel Stats Display")]
    [SerializeField] private TMP_Text statsInfoLeftText;  // 체력, 방어, 공격
    [SerializeField] private TMP_Text statsInfoRightText; // 스피드, 치명타, 쿨타임 등

    #endregion

    #region Private Fields

    private PlayerStats playerStats;

    #endregion

    #region Initialization

    void Start()
    {
        BindUI();
        FindAndSubscribePlayerStats();
    }

    void OnDestroy()
    {
        UnsubscribePlayerStats();
    }

    private void BindUI()
    {
        statsTextMidLeft = UIHelper.FindChild<TMP_Text>(transform, "Stats_Text_Mid_Left");
        statsTextMidRight = UIHelper.FindChild<TMP_Text>(transform, "Stats_Text_Mid_Right");
        cashText = UIHelper.FindChild<TMP_Text>(transform, "Cash_Text");

        statsInfoLeftText = UIHelper.FindChild<TMP_Text>(transform, "Stats_Right_Text");
        statsInfoRightText = UIHelper.FindChild<TMP_Text>(transform, "Stats_Left_Text");
    }

    private void FindAndSubscribePlayerStats()
    {
        playerStats = FindObjectOfType<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.OnStatsChanged += UpdateAllStats;
            UpdateAllStats(); // 초기 표시
        }
    }

    private void UnsubscribePlayerStats()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= UpdateAllStats;
        }
    }

    #endregion

    #region Public API

    public void UpdatePlayerStats(StatsData data)
    {
        UpdateHeaderStats(data);
        UpdateLeftPanelStats(data);
    }

    public void UpdateCurrency(int amount)
    {
        if (cashText != null)
        {
            cashText.text = $"보유 금액 : {amount:N0}"; 
        }
    }

    #endregion

    #region Private Methods - Stats Update

    private void UpdateAllStats()
    {
        if (playerStats == null) return;

        StatsData data = new StatsData
        {
            CurrentHealth = playerStats.CurrentHealth,
            MaxHealth = playerStats.CurrentMaxHealth,
            Defense = playerStats.CurrentDefense,
            Speed = playerStats.CurrentWalkSpeed,
            Power = playerStats.CurrentPower,
            Currency = playerStats.CurrentCurrency,

            CritChance = playerStats.CurrentCritChance,
            CooldownReduction = playerStats.CurrentCooldownReduction,
            DamageModifier = playerStats.CurrentDamageModifier
        };

        UpdatePlayerStats(data);
        UpdateCurrency(data.Currency);
    }

    private void UpdateHeaderStats(StatsData data)
    {
        if (statsTextMidLeft != null)
        {
            // 소수점 버리고 정수로 깔끔하게 표시
            statsTextMidLeft.text =
                $"HEALTH : {(int)data.CurrentHealth} / {(int)data.MaxHealth}\n" +
                $"DEFENSE : {(int)data.Defense}";
        }

        if (statsTextMidRight != null)
        {
            statsTextMidRight.text =
                $"SPEED : {data.Speed:F1}\n" + // 소수점 1자리
                $"POWER : {(int)data.Power}";
        }
    }

    private void UpdateLeftPanelStats(StatsData data)
    {
        if (statsInfoLeftText != null)
        {
            statsInfoLeftText.text =
                $"체력 : {(int)data.MaxHealth}\n" +
                $"방어력 : {(int)data.Defense}\n" +
                $"공격력 : {(int)data.Power}";
        }
        if (statsInfoRightText != null)
        {
            float dmgBonus = (data.DamageModifier - 1.0f) * 100f;
            statsInfoRightText.text =
                $"이동 속도 : <color=#FFD700>{data.Speed:F1}</color>\n" +
                $"치명타 확률 : {data.CritChance:F1}%\n" +
                $"쿨타임 감소 : {data.CooldownReduction:F0}%" +
                $"\n피해량 증가 : {dmgBonus:F0}%"; 
        }
    }

    #endregion
}

#region Data Structures

/// <summary>
/// UI 전달용 스탯 데이터 구조체 (확장됨)
/// </summary>
public struct StatsData
{
    public float CurrentHealth;
    public float MaxHealth;
    public float Defense;
    public float Speed;
    public float Power;
    public int Currency;

    public float CritChance;       // 치명타 확률
    public float CooldownReduction; // 쿨타임 감소
    public float DamageModifier;    // 데미지 배율
}

#endregion