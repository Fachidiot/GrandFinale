using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탭 전환 전담 컨트롤러
/// - Inventory/Artifacts 탭 전환
/// - Fade + Slide 애니메이션
/// - 유물 탭 진입 시 왼쪽 정보창 활성화
/// </summary>
public class TabSwitchController : MonoBehaviour
{
    public enum TabType
    {
        Inventory,
        Artifacts
    }

    #region Serialized Fields

    [Header("Tab Panels")]
    [SerializeField] private GameObject inventoryPanel;       // Mid_Inventory_Info
    [SerializeField] private GameObject artifactsPanel;       // Artifacts_Info

    [Header("Additional Panels")]
    [SerializeField] private GameObject leftInventoryInfoPanel; // Left_Inventory_Info

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup inventoryCanvasGroup;
    [SerializeField] private CanvasGroup artifactsCanvasGroup;

    [Header("Tab Buttons")]
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button artifactsButton;
    [SerializeField] private TMP_Text tabSelectText;

    [Header("Animation Settings")]
    [SerializeField] private float tabSwitchDuration = 0.15f;
    [SerializeField] private float slideDistance = 10f;

    #endregion

    #region Private Fields

    private TabType currentTab = TabType.Inventory;

    #endregion

    #region Initialization

    void Start()
    {
        BindUI();
        InitializeTabState();
        RegisterButtonEvents();
    }

    void OnDestroy()
    {
        UnregisterButtonEvents();
    }

    private void BindUI()
    {
        inventoryPanel = UIHelper.FindObject(transform, "Mid_Inventory_Info");
        artifactsPanel = UIHelper.FindObject(transform, "Artifacts_Info");
        leftInventoryInfoPanel = UIHelper.FindObject(transform, "Left_Inventory_Info");

        if (inventoryPanel) inventoryCanvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
        if (artifactsPanel) artifactsCanvasGroup = artifactsPanel.GetComponent<CanvasGroup>();

        inventoryButton = UIHelper.FindChild<Button>(transform, "Inventory_Button");
        artifactsButton = UIHelper.FindChild<Button>(transform, "Artifacts_Button");
        tabSelectText = UIHelper.FindChild<TMP_Text>(transform, "Button_Sellect_Text");
    }

    private void InitializeTabState()
    {
        // 초기화 시 Inventory 탭 활성화
        SetTabImmediate(TabType.Inventory);
    }

    private void RegisterButtonEvents()
    {
        if (inventoryButton != null) inventoryButton.onClick.AddListener(OnInventoryTabClick);
        if (artifactsButton != null) artifactsButton.onClick.AddListener(OnArtifactsTabClick);
    }

    private void UnregisterButtonEvents()
    {
        if (inventoryButton != null) inventoryButton.onClick.RemoveListener(OnInventoryTabClick);
        if (artifactsButton != null) artifactsButton.onClick.RemoveListener(OnArtifactsTabClick);
    }

    #endregion

    #region Public API

    public void OnInventoryTabClick()
    {
        if (currentTab == TabType.Inventory) return;
        SwitchTab(TabType.Inventory);
    }

    public void OnArtifactsTabClick()
    {
        if (currentTab == TabType.Artifacts) return;
        SwitchTab(TabType.Artifacts);
    }

    public void SetTab(TabType tab, bool animated = false)
    {
        if (animated) SwitchTab(tab);
        else SetTabImmediate(tab);
    }

    #endregion

    #region Private Methods - Tab Switching

    private void SwitchTab(TabType targetTab)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayTabSound();

        if (targetTab == TabType.Inventory)
        {
            SwitchToInventoryWithAnimation();
            if (inventoryButton)
                inventoryButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1);
        }
        else
        {
            SwitchToArtifactsWithAnimation();
            if (artifactsButton)
                artifactsButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1);
        }

        currentTab = targetTab;
        UpdateButtonStates(targetTab);
    }

    private void SetTabImmediate(TabType targetTab)
    {
        if (targetTab == TabType.Inventory)
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            if (artifactsPanel != null) artifactsPanel.SetActive(false);

            if (inventoryCanvasGroup != null)
            {
                inventoryCanvasGroup.alpha = 1f;
                inventoryCanvasGroup.blocksRaycasts = true;
            }
            if (artifactsCanvasGroup != null)
            {
                artifactsCanvasGroup.alpha = 0f;
                artifactsCanvasGroup.blocksRaycasts = false;
            }
        }
        else // Artifacts
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (artifactsPanel != null) artifactsPanel.SetActive(true);

            // [핵심] 유물 탭일 때 왼쪽 패널 켜기
            if (leftInventoryInfoPanel != null) leftInventoryInfoPanel.SetActive(true);

            if (inventoryCanvasGroup != null)
            {
                inventoryCanvasGroup.alpha = 0f;
                inventoryCanvasGroup.blocksRaycasts = false;
            }
            if (artifactsCanvasGroup != null)
            {
                artifactsCanvasGroup.alpha = 1f;
                artifactsCanvasGroup.blocksRaycasts = true;
            }
        }

        currentTab = targetTab;
        UpdateButtonStates(targetTab);
    }

    #endregion

    #region Private Methods - Animations

    private void SwitchToInventoryWithAnimation()
    {
        if (!ShouldAnimateTransition(artifactsPanel, inventoryPanel)) return;

        TabTransitionData fadeOut = CreateFadeOutData(artifactsPanel, artifactsCanvasGroup, -slideDistance);
        TabTransitionData fadeIn = CreateFadeInData(inventoryPanel, inventoryCanvasGroup, slideDistance);

        AnimateTabTransition(fadeOut, fadeIn);
    }

    private void SwitchToArtifactsWithAnimation()
    {
        if (!ShouldAnimateTransition(inventoryPanel, artifactsPanel)) return;

        // [핵심] 애니메이션 전환 시에도 왼쪽 패널 켜기
        if (leftInventoryInfoPanel != null) leftInventoryInfoPanel.SetActive(true);

        TabTransitionData fadeOut = CreateFadeOutData(inventoryPanel, inventoryCanvasGroup, slideDistance);
        TabTransitionData fadeIn = CreateFadeInData(artifactsPanel, artifactsCanvasGroup, -slideDistance);

        AnimateTabTransition(fadeOut, fadeIn);
    }

    private bool ShouldAnimateTransition(GameObject oldPanel, GameObject newPanel)
    {
        return oldPanel != null && oldPanel.activeSelf && newPanel != null;
    }

    private TabTransitionData CreateFadeOutData(GameObject panel, CanvasGroup canvasGroup, float slideDirection)
    {
        return new TabTransitionData
        {
            Panel = panel,
            CanvasGroup = canvasGroup,
            RectTransform = panel.GetComponent<RectTransform>(),
            SlideDirection = slideDirection
        };
    }

    private TabTransitionData CreateFadeInData(GameObject panel, CanvasGroup canvasGroup, float slideDirection)
    {
        return new TabTransitionData
        {
            Panel = panel,
            CanvasGroup = canvasGroup,
            RectTransform = panel.GetComponent<RectTransform>(),
            SlideDirection = slideDirection
        };
    }

    private void AnimateTabTransition(TabTransitionData fadeOut, TabTransitionData fadeIn)
    {
        FadeOutPanel(fadeOut, () => { FadeInPanel(fadeIn); });
    }

    private void FadeOutPanel(TabTransitionData data, System.Action onComplete)
    {
        if (data.CanvasGroup == null || data.RectTransform == null) return;

        data.CanvasGroup.blocksRaycasts = false;
        data.CanvasGroup.DOFade(0f, tabSwitchDuration);

        Vector2 targetPos = data.RectTransform.anchoredPosition + new Vector2(data.SlideDirection, 0);
        data.RectTransform.DOAnchorPos(targetPos, tabSwitchDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                data.Panel.SetActive(false);
                onComplete?.Invoke();
            });
    }

    private void FadeInPanel(TabTransitionData data)
    {
        if (data.CanvasGroup == null || data.RectTransform == null) return;

        data.Panel.SetActive(true);
        data.CanvasGroup.alpha = 0f;

        Vector2 startPos = data.RectTransform.anchoredPosition - new Vector2(data.SlideDirection, 0);
        data.RectTransform.anchoredPosition = startPos;

        data.CanvasGroup.DOFade(1f, tabSwitchDuration);

        Vector2 targetPos = startPos + new Vector2(data.SlideDirection, 0);
        data.RectTransform.DOAnchorPos(targetPos, tabSwitchDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                data.CanvasGroup.blocksRaycasts = true;
            });
    }

    #endregion

    #region Private Methods - UI Update

    private void UpdateButtonStates(TabType activeTab)
    {
        if (inventoryButton != null)
        {
            inventoryButton.interactable = (activeTab != TabType.Inventory);
            if (activeTab == TabType.Inventory) inventoryButton.transform.localScale = Vector3.one;
        }

        if (artifactsButton != null)
        {
            artifactsButton.interactable = (activeTab != TabType.Artifacts);
            if (activeTab == TabType.Artifacts) artifactsButton.transform.localScale = Vector3.one;
        }

        if (tabSelectText != null)
        {
            tabSelectText.text = activeTab == TabType.Inventory ? "인벤토리" : "유물";
        }
    }

    #endregion

    #region Nested Classes

    private class TabTransitionData
    {
        public GameObject Panel;
        public CanvasGroup CanvasGroup;
        public RectTransform RectTransform;
        public float SlideDirection;
    }

    #endregion
}