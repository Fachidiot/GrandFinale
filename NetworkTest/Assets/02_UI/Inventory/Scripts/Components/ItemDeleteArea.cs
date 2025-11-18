using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ItemDeleteArea : MonoBehaviour, IDropHandler
{
    // 드롭 이벤트 처리
    public void OnDrop(PointerEventData eventData)
    {
        Slot_UI sourceSlot = eventData.pointerDrag.GetComponent<Slot_UI>();

        if (IsValidSlot(sourceSlot))
        {
            DeleteItem(sourceSlot);
        }
    }

    // 유효한 슬롯인지 확인
    private bool IsValidSlot(Slot_UI slot)
    {
        return slot != null &&
               slot.currentSlot != null &&
               slot.currentSlot.item != null &&
               slot.currentSlot.slotIndex != -1;
    }

    // 아이템 삭제
    private void DeleteItem(Slot_UI sourceSlot)
    {
        bool success = InventoryManager.Instance.RemoveItem(sourceSlot.currentSlot.slotIndex);

        if (success)
        {
            sourceSlot.dropSuccessful = true;
        }
    }
}