using UnityEngine;
using UnityEngine.EventSystems;

public class SynthesisDropArea : MonoBehaviour, IDropHandler
{
    [SerializeField] private RelicSynthesisManager manager;

    void Start()
    {
        if (manager == null)
            manager = GetComponentInParent<RelicSynthesisManager>();

        if (manager == null)
            manager = FindObjectOfType<RelicSynthesisManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (manager == null) return;

        Slot_UI sourceSlot = eventData.pointerDrag.GetComponent<Slot_UI>();

        if (sourceSlot != null && sourceSlot.currentItem != null)
        {
            var type = sourceSlot.currentItem.item.itemTypeEnum;
            // 유물 또는 재료만 허용
            if (type != ItemType.Artifact && type != ItemType.Etc) return;

            // 매니저에게 생성 요청 (마우스 위치 전달)
            manager.SpawnNodeAtPosition(sourceSlot.currentItem, eventData.position);
        }
    }
}