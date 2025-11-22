using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UpgradeSlotUI : MonoBehaviour, IDropHandler
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;       // 장비 아이콘
    [SerializeField] private TextMeshProUGUI nameText; // 장비 이름

    public UpgradeModule _module;

    public RelicData CurrentItem { get; private set; }

    private void Awake()
    {
        // 부모 계층에서 UpgradeModule 찾기
        _module = GetComponentInParent<UpgradeModule>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[UpgradeSlotUI] OnDrop 호출됨! 대상: {gameObject.name}");

        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        Slot_UI sourceSlot = draggedObject.GetComponent<Slot_UI>();

        // 유효한 아이템인지 확인
        if (sourceSlot == null || sourceSlot.currentSlot == null || sourceSlot.currentSlot.item == null)
            return;

        if (_module != null)
        {
            // 모듈에게 장비 등록 요청 (HandleEquipmentDrop)
            bool success = _module.HandleEquipmentDrop(sourceSlot.currentSlot.item, sourceSlot.currentSlot.slotIndex);

            if (success)
            {
                // 성공 시 UI 업데이트 및 드롭 완료 처리
                UpdateUI(sourceSlot.currentSlot.item);
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
                iconImage.color = Color.white;
            }
        }

        // 이름 표시
        if (nameText != null)
        {
            nameText.text = item.itemName;
        }
    }

    public void ClearSlot()
    {
        CurrentItem = null;

        // 아이콘 숨기기
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            iconImage.color = new Color(1, 1, 1, 0);
        }

        // 텍스트 초기화
        if (nameText != null)
        {
            nameText.text = "장비 슬롯"; // 기본 텍스트
        }
    }
}