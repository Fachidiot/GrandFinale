using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image borderImage;

    [Header("Selection Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    private RelicData _data;
    private ShopModule _shop;
    private bool _isSelected = false;

    private bool HasItem => _data != null;

    public void Setup(RelicData data, ShopModule shop)
    {
        _data = data;
        _shop = shop;

        // 데이터가 없으면(빈 슬롯) 초기화 후 종료
        if (!HasItem)
        {
            if (iconImage)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            if (nameText) nameText.text = "";
            SetSelected(false);
            return;
        }

        // 데이터가 있을 때만 아이콘/텍스트 설정
        if (iconImage)
        {
            if (!string.IsNullOrEmpty(data.iconPath))
            {
                iconImage.sprite = Resources.Load<Sprite>(data.iconPath);
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        if (nameText) nameText.text = data.itemName;
        SetSelected(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasItem) return;

        // 2. 소리 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSound();
        }

        // 3. 상점 모듈에 알림
        if (_shop != null)
        {
            _shop.OnSlotClicked(_data, this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. 아이템 없으면 즉시 종료
        if (!HasItem) return;

        // 2. 호버 소리 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.uiHoverClip);
        }

        // 3. 툴팁 표시
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.ShowTooltip(_data, InventoryType.Shop);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.HideTooltip();
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (borderImage)
        {
            borderImage.color = selected ? selectedColor : normalColor;
        }
    }
}