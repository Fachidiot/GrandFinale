using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class SynthesisMessagePopup : MonoBehaviour
{
    #region Serialized Fields
    [Header("UI Components (Auto-Bind)")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private GameObject popupPanel;

    private PlayerInputs playerInputs;
    #endregion

    private CanvasGroup canvasGroup;

    #region Unity Lifecycle
    void Awake()
    {
        if (popupPanel == null) popupPanel = gameObject;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (popupPanel == null) popupPanel = gameObject;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (messageText == null)
        {
            Transform txtTr = transform.Find("Error_Text");
            if (txtTr != null) messageText = txtTr.GetComponent<TextMeshProUGUI>();
        }

        if (closeButton == null)
        {
            Transform btnTr = transform.Find("Error_Close_Button");
            if (btnTr != null) closeButton = btnTr.GetComponent<Button>();
        }
    }

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePopup);
        }

        ClosePopupImmediate();
    }

    void Update()
    {
        if (popupPanel.activeSelf && playerInputs != null && playerInputs.GetEscape())
        {
            ClosePopup();
        }
    }
    #endregion

    #region Public Methods
    public void ShowMessage(string message)
    {
        if (messageText != null) messageText.text = message;

        popupPanel.SetActive(true);

        transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        seq.Join(canvasGroup.DOFade(1f, 0.2f));
        seq.Play();
    }

    public void ClosePopup()
    {
        if (!popupPanel.activeSelf) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.uiCloseClip);

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        seq.Join(canvasGroup.DOFade(0f, 0.2f));
        seq.OnComplete(() => popupPanel.SetActive(false));
        seq.Play();
    }
    #endregion

    private void ClosePopupImmediate()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }
}