using DG.Tweening;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 왼쪽 정보 패널 전담 컨트롤러
/// - 장비 정보 / 플레이어 정보 / 아이템 정보 전환
/// - Fade + Scale 애니메이션
/// - 아이템 상세 정보 표시
/// </summary>
public class LeftPanelController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Panel Buttons")]
    [SerializeField] private Button equipmentInfoButton;
    [SerializeField] private Button playerInfoButton;

    [Header("Panel Objects")]
    [SerializeField] private GameObject equipmentInfoPanel;
    [SerializeField] private GameObject statsInfoPanel;
    [SerializeField] private GameObject itemInfoPanel;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup equipmentCanvasGroup;
    [SerializeField] private CanvasGroup statsCanvasGroup;
    [SerializeField] private CanvasGroup itemCanvasGroup;

    [Header("Item Info Components")]
    [SerializeField] private Image itemInfoImage;
    [SerializeField] private TMP_Text itemInfoNameText;
    [SerializeField] private TMP_Text itemInfoDescriptionText;
    [SerializeField] private TMP_Text itemInfoHeadText;

    [Header("Animation Settings")]
    [SerializeField] private float panelSwitchDuration = 0.2f;
    [SerializeField] private float contentFadeDuration = 0.1f;

    #endregion

    #region Private Fields

    private CanvasGroup currentPanelCanvasGroup;
    private RelicData currentDisplayedItem;
    private readonly SpriteCache spriteCache = new SpriteCache();

    #endregion

    #region Initialization

    void Start()
    {
        BindUI();

        InitializePanels();
        RegisterButtonEvents();
        ShowEquipmentPanel();
    }

    private void BindUI()
    {
        equipmentInfoButton = UIHelper.FindChild<Button>(transform, "Equipment_Change_Button");
        playerInfoButton = UIHelper.FindChild<Button>(transform, "State_Change_Button");

        equipmentInfoPanel = UIHelper.FindObject(transform, "Equipment_Info");
        statsInfoPanel = UIHelper.FindObject(transform, "Stats_Info");
        itemInfoPanel = UIHelper.FindObject(transform, "Item_Info");

        if (itemInfoPanel != null)
        {
            itemInfoImage = UIHelper.FindChild<Image>(itemInfoPanel.transform, "Item_Image");
            itemInfoNameText = UIHelper.FindChild<TMP_Text>(itemInfoPanel.transform, "Item_Name_Text");
            itemInfoDescriptionText = UIHelper.FindChild<TMP_Text>(itemInfoPanel.transform, "Item_Text");
            itemInfoHeadText = UIHelper.FindChild<TMP_Text>(itemInfoPanel.transform, "Head_Item_Info_Text");
        }
    }

    void OnDestroy()
    {
        UnregisterButtonEvents();
    }

    private void InitializePanels()
    {
        // 모든 패널 활성화 (CanvasGroup으로 가시성 제어)
        if (equipmentInfoPanel != null) equipmentInfoPanel.SetActive(true);
        if (statsInfoPanel != null) statsInfoPanel.SetActive(true);
        if (itemInfoPanel != null) itemInfoPanel.SetActive(true);

        // CanvasGroup 자동 할당
        if (equipmentCanvasGroup == null && equipmentInfoPanel != null)
        {
            equipmentCanvasGroup = equipmentInfoPanel.GetComponent<CanvasGroup>();
            if (equipmentCanvasGroup == null)
            {
                equipmentCanvasGroup = equipmentInfoPanel.AddComponent<CanvasGroup>();
            }
        }

        if (statsCanvasGroup == null && statsInfoPanel != null)
        {
            statsCanvasGroup = statsInfoPanel.GetComponent<CanvasGroup>();
            if (statsCanvasGroup == null)
            {
                statsCanvasGroup = statsInfoPanel.AddComponent<CanvasGroup>();
            }
        }

        if (itemCanvasGroup == null && itemInfoPanel != null)
        {
            itemCanvasGroup = itemInfoPanel.GetComponent<CanvasGroup>();
            if (itemCanvasGroup == null)
            {
                itemCanvasGroup = itemInfoPanel.AddComponent<CanvasGroup>();
            }
        }

        // 초기 상태: Stats와 Item 패널 숨김
        if (statsCanvasGroup != null)
        {
            statsCanvasGroup.alpha = 0f;
            statsCanvasGroup.blocksRaycasts = false;
        }

        if (itemCanvasGroup != null)
        {
            itemCanvasGroup.alpha = 0f;
            itemCanvasGroup.blocksRaycasts = false;
        }

        // Equipment 패널만 표시
        if (equipmentCanvasGroup != null)
        {
            equipmentCanvasGroup.alpha = 1f;
            equipmentCanvasGroup.blocksRaycasts = true;
            currentPanelCanvasGroup = equipmentCanvasGroup;
        }
    }

    private void RegisterButtonEvents()
    {
        if (equipmentInfoButton != null)
        {
            equipmentInfoButton.onClick.AddListener(OnEquipmentInfoButtonClick);
        }

        if (playerInfoButton != null)
        {
            playerInfoButton.onClick.AddListener(OnPlayerInfoButtonClick);
        }
    }

    private void UnregisterButtonEvents()
    {
        if (equipmentInfoButton != null)
        {
            equipmentInfoButton.onClick.RemoveListener(OnEquipmentInfoButtonClick);
        }

        if (playerInfoButton != null)
        {
            playerInfoButton.onClick.RemoveListener(OnPlayerInfoButtonClick);
        }
    }

    #endregion

    #region Public API - Panel Switching

    /// <summary>
    /// 장비 정보 패널 표시
    /// </summary>
    public void ShowEquipmentPanel()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClickSound();

        SwitchPanel(equipmentCanvasGroup);
        UpdateButtonStates(PanelType.Equipment);
    }

    /// <summary>
    /// 플레이어 스탯 패널 표시
    /// </summary>
    public void ShowPlayerStatsPanel()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClickSound();

        SwitchPanel(statsCanvasGroup);
        UpdateButtonStates(PanelType.PlayerStats);
    }

    /// <summary>
    /// 아이템 정보 패널 표시
    /// </summary>
    public void ShowItemInfoPanel(RelicData item)
    {
        if (item == null) return;

        bool itemChanged = (currentDisplayedItem != item);
        bool panelChanged = (currentPanelCanvasGroup != itemCanvasGroup);

        if (panelChanged)
        {
            // 다른 패널에서 전환
            SwitchPanel(itemCanvasGroup);
            UpdateItemContent(item);
        }
        else if (itemChanged)
        {
            // 같은 패널 내에서 아이템만 변경 (Quick Fade)
            AnimateContentChange(() => UpdateItemContent(item));
        }
        // else: 같은 아이템이면 아무것도 하지 않음

        UpdateButtonStates(PanelType.ItemInfo);
        currentDisplayedItem = item;
    }

    /// <summary>
    /// 아이템 상세 정보 업데이트 (외부 호출용)
    /// </summary>
    public void UpdateDetails(RelicData item)
    {
        if (item != null)
        {
            ShowItemInfoPanel(item);
        }
    }

    /// <summary>
    /// 상세 정보 초기화 (외부 호출용)
    /// </summary>
    public void ClearDetails()
    {
        ShowEquipmentPanel();
    }

    #endregion

    #region Private Methods - Button Handlers

    private void OnEquipmentInfoButtonClick()
    {
        ShowEquipmentPanel();
    }

    private void OnPlayerInfoButtonClick()
    {
        ShowPlayerStatsPanel();
    }

    #endregion

    #region Private Methods - Panel Animation

    private void SwitchPanel(CanvasGroup targetPanel)
    {
        if (targetPanel == null || targetPanel == currentPanelCanvasGroup)
        {
            return;
        }

        CanvasGroup oldPanel = currentPanelCanvasGroup;

        // Fade Out 이전 패널
        if (oldPanel != null)
        {
            oldPanel.blocksRaycasts = false;
            oldPanel.DOFade(0f, panelSwitchDuration).SetEase(Ease.InSine);
            oldPanel.transform.DOScale(0.8f, panelSwitchDuration).SetEase(Ease.InSine);
        }

        // Fade In 새 패널
        targetPanel.alpha = 0f;
        targetPanel.transform.localScale = Vector3.one * 0.8f;
        targetPanel.blocksRaycasts = true;

        targetPanel.DOFade(1f, panelSwitchDuration).SetEase(Ease.OutSine);
        targetPanel.transform.DOScale(1f, panelSwitchDuration).SetEase(Ease.OutBack);

        currentPanelCanvasGroup = targetPanel;
    }

    private void AnimateContentChange(System.Action updateAction)
    {
        if (itemCanvasGroup == null) return;

        // Quick Fade Out
        itemCanvasGroup.DOFade(0.5f, contentFadeDuration)
            .OnComplete(() =>
            {
                // Update Content
                updateAction?.Invoke();

                // Quick Fade In
                itemCanvasGroup.DOFade(1f, contentFadeDuration);
            });
    }

    #endregion

    #region Private Methods - Content Update

    private void UpdateItemContent(RelicData item)
    {
        if (item == null) return;

        UpdateItemHeader();
        UpdateItemIcon(item);
        UpdateItemName(item);
        UpdateItemDescription(item);
    }

    private void UpdateItemHeader()
    {
        if (itemInfoHeadText != null)
        {
            itemInfoHeadText.text = "아이템 정보";
        }
    }

    private void UpdateItemIcon(RelicData item)
    {
        if (itemInfoImage == null) return;

        Sprite icon = spriteCache.GetSprite(item.iconPath);
        itemInfoImage.sprite = icon;
        itemInfoImage.enabled = (icon != null);
    }

    private void UpdateItemName(RelicData item)
    {
        if (itemInfoNameText == null) return;

        string color = GradeColorHelper.GetColor(item.grade);
        itemInfoNameText.text = $"<color={color}>{item.itemName}</color>";
    }

    private void UpdateItemDescription(RelicData item)
    {
        if (itemInfoDescriptionText == null) return;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(item.description);
        builder.AppendLine();

        if (item.grantedAbility != null)
        {
            AppendAbilityInfo(builder, item.grantedAbility);
        }

        itemInfoDescriptionText.text = builder.ToString();
    }

    private void AppendAbilityInfo(StringBuilder builder, AbilityData ability)
    {
        if (ability.abilityLogicID == "Stat_Add")
        {
            builder.AppendLine($"+{ability.param_ValueA} {ability.param_Key}");
        }
        else
        {
            builder.AppendLine($"능력: {ability.abilityName}");
        }
    }

    #endregion

    #region Private Methods - Button State

    private void UpdateButtonStates(PanelType activePanel)
    {
        if (equipmentInfoButton != null)
        {
            equipmentInfoButton.interactable = (activePanel != PanelType.Equipment);
        }

        if (playerInfoButton != null)
        {
            playerInfoButton.interactable = (activePanel != PanelType.PlayerStats);
        }
    }

    #endregion

    #region Nested Enums

    private enum PanelType
    {
        Equipment,
        PlayerStats,
        ItemInfo
    }

    #endregion
}