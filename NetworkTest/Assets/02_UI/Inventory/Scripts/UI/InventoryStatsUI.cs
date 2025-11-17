using TMPro;
using UnityEngine;

/// <summary>
/// 인벤토리 스탯 표시 전담 컨트롤러
/// - 플레이어 스탯 UI 업데이트
/// - 통화 정보 표시
/// - PlayerStats 이벤트 구독
/// </summary>
public class InventoryStatsUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Header Stats Display")]
    [SerializeField] private TMP_Text statsTextMidLeft;
    [SerializeField] private TMP_Text statsTextMidRight;
    [SerializeField] private TMP_Text statsTextMidBottom;
    [SerializeField] private TMP_Text cashText;

    [Header("Left Panel Stats Display")]
    [SerializeField] private TMP_Text statsInfoLeftText;
    [SerializeField] private TMP_Text statsInfoRightText;

    #endregion

    #region Private Fields

    private PlayerStats playerStats;

    #endregion

    #region Initialization

    void Start()
    {
        FindAndSubscribePlayerStats();
    }

    void OnDestroy()
    {
        UnsubscribePlayerStats();
    }

    private void FindAndSubscribePlayerStats()
    {
        playerStats = FindObjectOfType<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.OnStatsChanged += UpdateAllStats;
            UpdateAllStats(); // 초기 표시
        }
        else
        {
            Debug.LogWarning("[InventoryStatsUI] PlayerStats를 찾을 수 없습니다.");
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

    /// <summary>
    /// 플레이어 스탯 UI 업데이트
    /// </summary>
    public void UpdatePlayerStats(StatsData data)
    {
        UpdateHeaderStats(data);
        UpdateLeftPanelStats(data);
    }

    /// <summary>
    /// 통화 정보 업데이트
    /// </summary>
    public void UpdateCurrency(int amount)
    {
        if (cashText != null)
        {
            cashText.text = $"보유 금액 : {amount}";
        }
    }

    #endregion

    #region Private Methods - Stats Update

    private void UpdateAllStats()
    {
        if (playerStats == null) return;

        StatsData data = new StatsData
        {
            CurrentHealth = (int)playerStats.CurrentHealth,
            MaxHealth = (int)playerStats.CurrentMaxHealth,
            Defense = (int)playerStats.CurrentDefense,
            Speed = (int)playerStats.CurrentRunSpeed,
            Power = (int)playerStats.CurrentPower,
            Level = playerStats.CurrentLevel,
            Currency = playerStats.CurrentCurrency
        };

        UpdatePlayerStats(data);
        UpdateCurrency(data.Currency);
    }

    private void UpdateHeaderStats(StatsData data)
    {
        if (statsTextMidLeft != null)
        {
            statsTextMidLeft.text =
                $"HEALTH : {data.CurrentHealth} / {data.MaxHealth}\n" +
                $"DEFENSE : {data.Defense}";
        }

        if (statsTextMidRight != null)
        {
            statsTextMidRight.text =
                $"SPEED : {data.Speed}\n" +
                $"POWER : {data.Power}";
        }

        if (statsTextMidBottom != null)
        {
            statsTextMidBottom.text = $"레벨 : {data.Level}";
        }
    }

    private void UpdateLeftPanelStats(StatsData data)
    {
        if (statsInfoLeftText != null)
        {
            statsInfoLeftText.text =
                $"체력 : {data.MaxHealth}\n" +
                $"방어력 : {data.Defense}\n" +
                $"공격력 : {data.Power}";
        }

        if (statsInfoRightText != null)
        {
            statsInfoRightText.text =
                $"스피드 : {data.Speed}\n" +
                $"보유 능력1 : 없음\n" +
                $"보유 능력2 : 없음";
        }
    }

    #endregion
}

#region Data Structures

/// <summary>
/// 플레이어 스탯 데이터 구조체
/// </summary>
public struct StatsData
{
    public int CurrentHealth;
    public int MaxHealth;
    public int Defense;
    public int Speed;
    public int Power;
    public int Level;
    public int Currency;
}

#endregion