using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradePanelDropHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        Slot_UI sourceSlot = draggedObject.GetComponent<Slot_UI>();

        if (sourceSlot != null)
        {
            // 패널 영역에 드롭됨 = 슬롯에 안착 안 함 = 인벤토리로 유지
            sourceSlot.MarkDropSuccessful();
            Debug.Log("[UpgradePanelDropHandler] 패널 영역 드롭 → 인벤토리 반환");
        }
    }
}