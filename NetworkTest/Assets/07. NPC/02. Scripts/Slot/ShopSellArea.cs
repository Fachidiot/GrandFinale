using UnityEngine;
using UnityEngine.EventSystems;

public class ShopSellArea : MonoBehaviour, IDropHandler
{
    [SerializeField] private ShopModule shopModule;

    public void OnDrop(PointerEventData eventData)
    {
        // 드래그된 오브젝트가 인벤토리 슬롯인지 확인
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        Slot_UI slot = draggedObject.GetComponent<Slot_UI>();
        if (slot != null && slot.currentSlot != null && slot.currentSlot.item != null)
        {
            // 판매 로직 실행
            if (shopModule != null)
            {
                shopModule.SellItem(slot.currentSlot.item, slot.currentSlot.slotIndex);
            }
        }
    }
}