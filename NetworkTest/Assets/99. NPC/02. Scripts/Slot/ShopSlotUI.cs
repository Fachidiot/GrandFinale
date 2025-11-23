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

        //데이터가 null이면 슬롯을 비우고 함수를 종료합니다.
        if (data == null)
        {
            if (iconImage)
            {
                iconImage.sprite = null;
                iconImage.enabled = false; // 아이콘 숨김
            }
            if (nameText) nameText.text = ""; // 이름 지움
            SetSelected(false);
            return;
        }

        // 데이터가 있을 때만 아래 코드가 실행됩니다.
        if (iconImage)
        {
            // 아이콘 경로가 있으면 로드, 없으면 null
            if (!string.IsNullOrEmpty(data.iconPath))
            {
                iconImage.sprite = Resources.Load<Sprite>(data.iconPath);
                iconImage.enabled = true; // 아이콘 보임
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

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

        // 2. ShopModule로 전달
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