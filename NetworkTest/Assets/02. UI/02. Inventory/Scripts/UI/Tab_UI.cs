using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;

public class Tab_UI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    public InventoryFilterType filterType; // 이 버튼이 담당할 필터

    [Header("Visual Targets")]
    [SerializeField] private Image targetBorderImage; // 색깔을 바꿀 타겟 이미지

    [Header("Colors")]
    [SerializeField] private Color activeColor = new Color(1f, 1f, 0f, 1f);   
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Animation")]
    [SerializeField] private float punchScale = 0.1f; 
    [SerializeField] private float duration = 0.2f;  

    public static event Action<InventoryFilterType> OnTabChanged;

    void Awake()
    {
        if (targetBorderImage == null)
        {
            targetBorderImage = GetComponent<Image>();
        }
    }

    void Start()
    {
        OnTabChanged += UpdateVisualState;

        if (InventoryManager.Instance != null)
        {
            UpdateVisualState(InventoryManager.Instance.currentFilter);
        }
    }

    void OnDestroy()
    {
        OnTabChanged -= UpdateVisualState;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetFilter(filterType);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabSound();
        }

        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * punchScale, duration, 10, 1);

        OnTabChanged?.Invoke(filterType);
    }

    private void UpdateVisualState(InventoryFilterType selectedType)
    {
        if (targetBorderImage == null) return;

        bool isSelected = (this.filterType == selectedType);

        targetBorderImage.DOColor(isSelected ? activeColor : inactiveColor, duration);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.currentFilter != filterType)
        {
            transform.DOScale(1.05f, 0.1f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(1f, 0.1f);
    }
}