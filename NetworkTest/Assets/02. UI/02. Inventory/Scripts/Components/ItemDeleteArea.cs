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
        // InventoryItem 구조에 맞춰 수정됨
        return slot != null &&
               slot.currentItem != null &&
               slot.currentItem.item != null;
    }

    // 아이템 삭제
    private void DeleteItem(Slot_UI sourceSlot)
    {
        InventoryManager.Instance.RemoveItem(sourceSlot.currentItem);
        sourceSlot.dropSuccessful = true;
    }
}