using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MaterialSlotUI : MonoBehaviour, IDropHandler
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

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[MaterialSlotUI] OnDrop 호출됨! 대상: {gameObject.name}");

        // 1. 드래그된 객체 확인
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        // 2. 인벤토리 슬롯인지 확인
        Slot_UI sourceSlot = draggedObject.GetComponent<Slot_UI>();

        // 유효한 아이템이 있는 슬롯인지 검사
        if (sourceSlot == null || sourceSlot.currentSlot == null || sourceSlot.currentSlot.item == null)
            return;

        if (_module != null)
        {
            bool success = _module.HandleMaterialDrop(sourceSlot.currentSlot.item, sourceSlot.currentSlot.slotIndex, this);

            if (success)
            {
                sourceSlot.MarkDropSuccessful();
            }
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
}