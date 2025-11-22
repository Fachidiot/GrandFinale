using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image borderImage; // 선택 표시용 테두리

    [Header("Selection Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    private RelicData _data;
    private ShopModule _shop;
    private bool _isSelected = false;

    public void Setup(RelicData data, ShopModule shop)
    {
        _data = data;
        _shop = shop;

        if (iconImage) iconImage.sprite = Resources.Load<Sprite>(data.iconPath);
        if (nameText) nameText.text = data.itemName;

        SetSelected(false); // 초기화
    }

    public void OnPointerClick(PointerEventData eventData)
    {

        // 1. 데이터 유무 확인 및 무시
        if (_data == null)
        {
            Debug.LogWarning($"[ShopSlotUI] 클릭 무시: {gameObject.name}에 할당된 아이템 데이터가 없습니다.");
            return;
        }

        // 2. ShopModule로 전달 (Ctrl 키 체크 로직 제거 및 시그니처 단순화)
        if (_shop != null)
        {
            _shop.OnSlotClicked(_data, this);
        }
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (borderImage)
        {
            borderImage.color = selected ? selectedColor : normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_data == null) return;

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
}