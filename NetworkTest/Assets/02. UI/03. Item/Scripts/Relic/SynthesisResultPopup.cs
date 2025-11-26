using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class SynthesisResultPopup : MonoBehaviour
{
    #region Serialized Fields
    [Header("UI Components (Auto-Bind)")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI itemDescText;

    [Header("Settings")]
    [SerializeField] private GameObject popupPanel;

    private PlayerInputs playerInputs;
    #endregion

    private CanvasGroup canvasGroup;

    void Awake()
    {
        // 1. 기본 설정
        if (popupPanel == null) popupPanel = gameObject;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        // 2. 자동 바인딩
        if (itemIcon == null)
            itemIcon = transform.Find("SynthesisResult_Item_Image")?.GetComponent<Image>();

        if (itemNameText == null)
            itemNameText = transform.Find("SynthesisResult_Item_Name")?.GetComponent<TextMeshProUGUI>();

        if (confirmButton == null)
            confirmButton = transform.Find("Close_Button")?.GetComponent<Button>();

        if (itemDescText == null)
            itemDescText = transform.Find("SynthesisResult_Item_Desc")?.GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        if (playerInputs == null)
            playerInputs = FindObjectOfType<PlayerInputs>();

        // 버튼 이벤트 연결
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ClosePopup);
        }
    }

    void OnEnable()
    {
        // [핵심] 켜지자마자 안 보이게 세팅 (애니메이션 준비)
        transform.localScale = Vector3.zero;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (popupPanel.activeSelf && playerInputs != null && playerInputs.GetEscape())
        {
            ClosePopup();
        }
    }

    // 매니저가 호출하는 함수
    public void ShowResult(RelicData item)
    {
        if (item == null) return;


        // 데이터 세팅
        if (itemIcon != null) itemIcon.sprite = Resources.Load<Sprite>(item.iconPath);
        if (itemNameText != null) itemNameText.text = item.itemName;

        if (itemDescText != null)
        {
            itemDescText.text = item.description;
        }

        // 애니메이션 시작
        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        seq.Join(canvasGroup.DOFade(1f, 0.2f));
        seq.Play();
    }

    public void ClosePopup()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.uiCloseClip);

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        seq.Join(canvasGroup.DOFade(0f, 0.2f));
        seq.OnComplete(() => gameObject.SetActive(false)); // 다 끝나면 끈다
        seq.Play();
    }
}