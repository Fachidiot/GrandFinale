using UnityEngine;
using UnityEngine.EventSystems;

public class SynthesisDropArea : MonoBehaviour, IDropHandler
{
    [SerializeField] private RelicSynthesisManager manager;

    void Start()
    {
        if (manager == null) manager = GetComponentInParent<RelicSynthesisManager>();
        if (manager == null) manager = FindObjectOfType<RelicSynthesisManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (manager == null) return;

        Slot_UI sourceSlot = eventData.pointerDrag.GetComponent<Slot_UI>();

        if (sourceSlot != null && sourceSlot.currentItem != null)
        {
            var type = sourceSlot.currentItem.item.itemTypeEnum;
            if (type != ItemType.Artifact && type != ItemType.Etc) return;

            manager.SpawnNode(sourceSlot.currentItem);
        }
    }
}