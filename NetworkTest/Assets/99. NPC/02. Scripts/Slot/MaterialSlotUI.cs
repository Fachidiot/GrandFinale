using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MaterialSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;

    public UpgradeModule _module;

    // 현재 이 슬롯에 담긴 아이템 데이터 (읽기 전용)
    public RelicData CurrentItem { get; private set; }

    private void Awake()
    {
        // 부모 계층에 있는 UpgradeModule을 자동으로 찾음
        _module = GetComponentInParent<UpgradeModule>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2 && CurrentItem != null)
        {
            if (_module != null) _module.ReturnMaterialToInventory(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CurrentItem == null || InventoryUIManager.Instance == null) return;

        if (iconImage != null && iconImage.sprite != null)
        {
            InventoryUIManager.Instance.StartDrag(iconImage.sprite);
            iconImage.color = new Color(1, 1, 1, 0.5f);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (CurrentItem != null && InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance != null) InventoryUIManager.Instance.EndDrag();
        if (iconImage != null) iconImage.color = Color.white;

    }
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[MaterialSlotUI] OnDrop 발생! 대상: {gameObject.name}");

        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        Slot_UI sourceSlot = draggedObject.GetComponent<Slot_UI>();

        // 유효성 검사
        if (sourceSlot == null || sourceSlot.currentSlot == null || sourceSlot.currentSlot.item == null)
            return;

        // 인벤토리에서 사라지기 전에, 아이템 정보를 미리 복사
        RelicData droppedItem = sourceSlot.currentSlot.item;
        int slotIndex = sourceSlot.currentSlot.slotIndex;

        if (_module != null)
        {
            // 저장해둔 droppedItem을 사용해서 전달
            bool success = _module.HandleMaterialDrop(droppedItem, slotIndex, this);

            if (success)
            {
                Debug.Log($"[MaterialSlotUI] 재료 드롭 성공: {droppedItem.itemName}");
                sourceSlot.MarkDropSuccessful();
            }
        }
        else
        {
            Debug.LogError("[MaterialSlotUI] 모듈 연결 안됨!");
        }
    }

    public void UpdateUI(RelicData item)
    {
        CurrentItem = item;

        // 아이콘 표시
        if (iconImage != null)
        {
            if (!string.IsNullOrEmpty(item.iconPath))
            {
                iconImage.sprite = Resources.Load<Sprite>(item.iconPath);
                iconImage.enabled = true;
                iconImage.color = Color.white; // 투명도 복구
            }
        }

    }

    public void ClearSlot()
    {
        CurrentItem = null;

        // 아이콘 숨기기
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false; // 이미지를 끄거나 투명하게
            iconImage.color = new Color(1, 1, 1, 0);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[MaterialSlotUI] 마우스 감지됨! : {gameObject.name}");
    }
}