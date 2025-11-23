using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 툴팁 표시 전담 컨트롤러
/// - 가격 표시 및 강화 수치 분리 기능 추가됨
/// </summary>
public class TooltipController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Tooltip Panels")]
    [SerializeField] private GameObject smallTooltipPanel;
    [SerializeField] private GameObject fullTooltipPanel;

    [Header("Small Tooltip Components")]
    [SerializeField] private RectTransform smallTooltipRect;
    [SerializeField] private TextMeshProUGUI smallTooltipTitleText;
    [SerializeField] private TextMeshProUGUI smallTooltipItemKindText;
    [SerializeField] private Image smallTooltipItemImage;
    [SerializeField] private TextMeshProUGUI smallTooltipGradeText;
    [SerializeField] private TextMeshProUGUI smallTooltipDescriptionText;
    [SerializeField] private TextMeshProUGUI smallTooltipUpgradeText; // 강화 수치(+1) 표시용
    [SerializeField] private TextMeshProUGUI smallTooltipPriceText;   

    [Header("Full Tooltip Components")]
    [SerializeField] private RectTransform fullTooltipRect;
    [SerializeField] private TextMeshProUGUI fullTooltipTitleText;
    [SerializeField] private TextMeshProUGUI fullTooltipItemKindText;
    [SerializeField] private Image fullTooltipItemImage;
    [SerializeField] private Image fullTooltipGradeImage;
    [SerializeField] private TextMeshProUGUI fullTooltipGradeText;
    [SerializeField] private TextMeshProUGUI fullTooltipDescriptionText;
    [SerializeField] private TextMeshProUGUI fullTooltipPriceText;   

    [Header("Animation Settings")]
    [SerializeField] private Vector2 tooltipOffset = new Vector2(20f, -50f);
    [SerializeField] private float fadeDuration = 0.2f;

    #endregion

    #region Private Fields

    private CanvasGroup currentTooltipCanvasGroup;
    private float currentTargetAlpha = 0f;
    private Coroutine fadeCoroutine;
    private readonly SpriteCache spriteCache = new SpriteCache();

    private readonly string[] upgradePrefixes = new string[]
    {
        "+5", "+4", "+3", "+2", "+1", "0"
    };

    #endregion

    #region Initialization

    void Awake()
    {
        BindUI();
        InitializeTooltipPanel(smallTooltipPanel);
        InitializeTooltipPanel(fullTooltipPanel);
    }

    private void BindUI()
    {
        // 1. 패널 찾기 (이름은 Hierarchy 구조에 맞게 수정 필요)
        smallTooltipPanel = UIHelper.FindObject(transform, "Small_Tooltip_Bar");
        fullTooltipPanel = UIHelper.FindObject(transform, "fulll_Tooltip_Bar");

        // 2. Small 컴포넌트 연결
        if (smallTooltipPanel != null)
        {
            smallTooltipRect = smallTooltipPanel.GetComponent<RectTransform>();

            smallTooltipTitleText = UIHelper.FindChild<TextMeshProUGUI>(smallTooltipPanel.transform, "Small_Title Text");
            smallTooltipItemKindText = UIHelper.FindChild<TextMeshProUGUI>(smallTooltipPanel.transform, "Small_Item Kind Text");
            smallTooltipItemImage = UIHelper.FindChild<Image>(smallTooltipPanel.transform, "Small_Item _Image");
            smallTooltipGradeText = UIHelper.FindChild<TextMeshProUGUI>(smallTooltipPanel.transform, "Small_Item_Grade_Text");
            smallTooltipDescriptionText = UIHelper.FindChild<TextMeshProUGUI>(smallTooltipPanel.transform, "Smalll_Detail_Text");
            smallTooltipUpgradeText = UIHelper.FindChild<TextMeshProUGUI>(smallTooltipPanel.transform, "Small_Item_Kind_Upgrade_Text");

            smallTooltipPriceText = UIHelper.FindChild<TextMeshProUGUI>(smallTooltipPanel.transform, "Small_Item_Price_Text");
        }
        else
        {
            Debug.LogError("[TooltipController] Small_Tooltip_Bar를 찾을 수 없습니다.");
        }

        // 3. Full 컴포넌트 연결
        if (fullTooltipPanel != null)
        {
            fullTooltipRect = fullTooltipPanel.GetComponent<RectTransform>();

            fullTooltipTitleText = UIHelper.FindChild<TextMeshProUGUI>(fullTooltipPanel.transform, "Title Text");
            fullTooltipItemKindText = UIHelper.FindChild<TextMeshProUGUI>(fullTooltipPanel.transform, "Item Kind Text");
            fullTooltipItemImage = UIHelper.FindChild<Image>(fullTooltipPanel.transform, "Item Kind Image");
            fullTooltipGradeImage = UIHelper.FindChild<Image>(fullTooltipPanel.transform, "Item Grade Info");
            fullTooltipGradeText = UIHelper.FindChild<TextMeshProUGUI>(fullTooltipPanel.transform, "Item Class Text");
            fullTooltipDescriptionText = UIHelper.FindChild<TextMeshProUGUI>(fullTooltipPanel.transform, "Detail_Text");

            fullTooltipPriceText = UIHelper.FindChild<TextMeshProUGUI>(fullTooltipPanel.transform, "Item_Price_Text");
        }
    }

    private void InitializeTooltipPanel(GameObject panel)
    {
        if (panel == null) return;
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
        panel.SetActive(false);
    }

    #endregion

    #region Public API

    public void ShowTooltip(RelicData item, InventoryType inventoryType)
    {
        if (item == null) return;

        TooltipData tooltipData = GetTooltipDataForInventory(inventoryType);
        if (tooltipData.Panel == null) return;

        UpdateTooltipContent(item, tooltipData);
        ShowTooltipPanel(tooltipData.Panel, tooltipData.CanvasGroup, tooltipData.RectTransform);
    }

    public void HideTooltip()
    {
        HideTooltipPanel(smallTooltipPanel);
        HideTooltipPanel(fullTooltipPanel);
    }

    #endregion

    #region Tooltip Data Management

    private TooltipData GetTooltipDataForInventory(InventoryType type)
    {
        // Full 툴팁 데이터 구성
        if (type == InventoryType.Full)
        {
            return new TooltipData
            {
                Panel = fullTooltipPanel,
                RectTransform = fullTooltipRect,
                CanvasGroup = GetOrCreateCanvasGroup(fullTooltipPanel),
                TitleText = fullTooltipTitleText,
                ItemKindText = fullTooltipItemKindText,
                ItemImage = fullTooltipItemImage,
                GradeText = fullTooltipGradeText,
                GradeImage = fullTooltipGradeImage,
                DescriptionText = fullTooltipDescriptionText,
                UpgradeText = null, 
                PriceText = fullTooltipPriceText 
            };
        }
        // Small 툴팁 데이터 구성
        else
        {
            return new TooltipData
            {
                Panel = smallTooltipPanel,
                RectTransform = smallTooltipRect,
                CanvasGroup = GetOrCreateCanvasGroup(smallTooltipPanel),
                TitleText = smallTooltipTitleText,
                ItemKindText = smallTooltipItemKindText,
                ItemImage = smallTooltipItemImage,
                GradeText = smallTooltipGradeText,
                GradeImage = null,
                DescriptionText = smallTooltipDescriptionText,
                UpgradeText = smallTooltipUpgradeText,
                PriceText = smallTooltipPriceText 
            };
        }
    }

    private CanvasGroup GetOrCreateCanvasGroup(GameObject panel)
    {
        if (panel == null) return null;
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    #endregion

    #region Content Update

    private void UpdateTooltipContent(RelicData item, TooltipData data)
    {
        UpdateTitle(data.TitleText, item);
        UpdateItemKind(data.ItemKindText, item);
        UpdateItemIcon(data.ItemImage, item);
        UpdateGrade(data.GradeText, data.GradeImage, item);
        UpdateDescription(data.DescriptionText, item);

        UpdatePrice(data.PriceText, item);          
        UpdateUpgradeLevel(data.UpgradeText, item); 
    }

    private void UpdateTitle(TMP_Text titleText, RelicData item)
    {
        if (titleText == null) return;

        string cleanName = item.itemName;

        
        foreach (string prefix in upgradePrefixes)
        {
            if (cleanName.StartsWith(prefix))
            {
                cleanName = cleanName.Substring(prefix.Length).Trim();
                break;
            }
        }

        string color = GradeColorHelper.GetColor(item.grade);
        titleText.text = $"<color={color}>{cleanName}</color>";
    }

    private void UpdateItemKind(TMP_Text kindText, RelicData item)
    {
        if (kindText == null) return;
        kindText.text = item.itemTypeEnum.ToString();
    }

    private void UpdateItemIcon(Image iconImage, RelicData item)
    {
        if (iconImage == null) return;
        Sprite icon = spriteCache.GetSprite(item.iconPath);
        iconImage.sprite = icon;
        iconImage.enabled = (icon != null);
    }

    private void UpdateGrade(TMP_Text gradeText, Image gradeImage, RelicData item)
    {
        if (gradeText != null)
        {
            gradeText.text = item.grade;
            string colorHex = GradeColorHelper.GetColor(item.grade);
            if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
                gradeText.color = color;
        }
        if (gradeImage != null) gradeImage.enabled = false;
    }

    private void UpdateDescription(TMP_Text descText, RelicData item)
    {
        if (descText == null) return;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(item.description);
        builder.AppendLine();
        if (item.grantedAbility != null) AppendAbilityInfo(builder, item.grantedAbility);
        descText.text = builder.ToString();
    }

    private void UpdateUpgradeLevel(TMP_Text upgradeText, RelicData item)
    {
        if (upgradeText == null) return;

        string foundPrefix = "+0"; 

        foreach (string prefix in upgradePrefixes)
        {
            if (item.itemName.StartsWith(prefix))
            {
                foundPrefix = prefix;
                break;
            }
        }
        upgradeText.text = foundPrefix;
    }

    private void UpdatePrice(TMP_Text priceText, RelicData item)
    {
        if (priceText == null) return;
        priceText.text = $"{item.price:N0} $";
    }

    private void AppendAbilityInfo(StringBuilder builder, AbilityData ability)
    {
        if (ability.abilityLogicID == "Stat_Add")
            builder.AppendLine($"+{ability.param_ValueA} {ability.param_Key}");
        else
            builder.AppendLine($"능력: {ability.abilityName}");
    }

    #endregion

    #region Panel Animation

    private void ShowTooltipPanel(GameObject panel, CanvasGroup canvasGroup, RectTransform rectTransform)
    {
        if (panel == null || canvasGroup == null) return;

        if (currentTargetAlpha == 1f && canvasGroup.alpha > 0f)
        {
            UpdateTooltipPosition(panel, rectTransform);
            return;
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        panel.SetActive(true);
        currentTargetAlpha = 1f;
        currentTooltipCanvasGroup = canvasGroup;

        fadeCoroutine = StartCoroutine(FadeTooltip(canvasGroup, 1f, panel));
        UpdateTooltipPosition(panel, rectTransform);
    }

    private void HideTooltipPanel(GameObject panel)
    {
        if (panel == null || !panel.activeSelf) return;

        CanvasGroup canvasGroup = GetOrCreateCanvasGroup(panel);
        if (canvasGroup == null) return;

        if (currentTargetAlpha == 0f) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        currentTargetAlpha = 0f;
        currentTooltipCanvasGroup = canvasGroup;

        fadeCoroutine = StartCoroutine(FadeTooltip(canvasGroup, 0f, panel));
    }

    private void UpdateTooltipPosition(GameObject panel, RectTransform rectTransform)
    {
        if (rectTransform == null) return;

        RectTransform parentCanvas = panel.transform.parent.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas, Input.mousePosition, null, out Vector2 localPointerPosition))
        {
            rectTransform.localPosition = localPointerPosition + tooltipOffset;
        }
    }

    private IEnumerator FadeTooltip(CanvasGroup canvasGroup, float targetAlpha, GameObject panel)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            canvasGroup.alpha = currentAlpha;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        currentTargetAlpha = targetAlpha;

        if (targetAlpha == 0f)
        {
            panel.SetActive(false);
        }

        fadeCoroutine = null;
    }

    #endregion

    #region Nested Classes

    private class TooltipData
    {
        public GameObject Panel;
        public RectTransform RectTransform;
        public CanvasGroup CanvasGroup;
        public TMP_Text TitleText;
        public TMP_Text ItemKindText;
        public Image ItemImage;
        public TMP_Text GradeText;
        public Image GradeImage;
        public TMP_Text DescriptionText;
        public TMP_Text UpgradeText; // 강화 수치
        public TMP_Text PriceText;   // 가격
    }

    #endregion
}

#region Helper Classes & Enums

public static class GradeColorHelper
{
    private const string COLOR_COMMON = "#FFFFFF";
    private const string COLOR_RARE = "#00CCFF";
    private const string COLOR_EPIC = "#9900FF";
    private const string COLOR_DEFAULT = "#FFFFFF";

    public static string GetColor(string grade)
    {
        if (string.IsNullOrEmpty(grade)) return COLOR_DEFAULT;

        return grade.ToLower() switch
        {
            "common" => COLOR_COMMON,
            "rare" => COLOR_RARE,
            "epic" => COLOR_EPIC,
            _ => COLOR_DEFAULT,
        };
    }
}

public class SpriteCache
{
    private readonly System.Collections.Generic.Dictionary<string, Sprite> cache
        = new System.Collections.Generic.Dictionary<string, Sprite>();

    public Sprite GetSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (cache.TryGetValue(path, out Sprite cached)) return cached;
        Sprite loaded = Resources.Load<Sprite>(path);
        if (loaded != null) cache[path] = loaded;
        return loaded;
    }

    public void Clear() { cache.Clear(); }
}

public enum InventoryType
{
    Small,
    Full,
    Shop
}

#endregion