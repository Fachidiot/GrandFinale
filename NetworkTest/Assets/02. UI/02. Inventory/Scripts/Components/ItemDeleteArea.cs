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
        // [수정] 인덱스가 아닌 아이템 객체 자체를 전달
        InventoryManager.Instance.RemoveItem(sourceSlot.currentItem);

        // 삭제는 즉시 처리되므로 성공 표시
        sourceSlot.dropSuccessful = true;
    }
}