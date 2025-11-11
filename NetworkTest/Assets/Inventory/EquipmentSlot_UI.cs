using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 드롭 감지!

public class EquipmentSlot_UI : MonoBehaviour, IDropHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    // 1. 이 슬롯이 받을 수 있는 아이템 타입 (RelicData.itemType과 비교할 문자열)
    [Tooltip("이 슬롯이 받을 itemType 문자열 (예: Relic, Artifact, Skill...)")]
    public string requiredType = "Relic"; // (RelicData의 itemType에 맞게 수정하세요)

    [Header("UI (선택 사항)")]
    public Image slotIcon; // 장착된 아이템 아이콘을 표시할 이미지
    public int equipmentSlotIndex; // 이 슬롯이 0,1,2,3 중 몇 번째인지

    public RelicData currentItem { get; private set; } // 이 슬롯이 현재 들고있는 아이템
    public bool dropSuccessful = false;

    void Start()
    {
        // 2. EquipmentManager의 신호를 구독해서 아이콘 업데이트
        EquipmentManager.OnEquipmentChanged += UpdateSlotVisuals;
        UpdateSlotVisuals(); // 시작할 때 한 번 실행
    }

    void OnDestroy()
    {
        EquipmentManager.OnEquipmentChanged -= UpdateSlotVisuals;
    }

    // 3. (Slot_UI와 동일) 매니저를 보고 내 아이콘을 업데이트
    void UpdateSlotVisuals()
    {
        if (EquipmentManager.Instance == null) return;

        // 내 인덱스에 맞는 RelicData를 가져옴
        currentItem = EquipmentManager.Instance.equipmentSlots[equipmentSlotIndex];
        if (currentItem != null && !string.IsNullOrEmpty(currentItem.iconPath))
        {
            Sprite icon = Resources.Load<Sprite>(currentItem.iconPath);
            if (icon != null)
            {
                slotIcon.sprite = icon;
                slotIcon.enabled = true;
            }
            else
            {
                slotIcon.enabled = false;
            }
        }
        else
        {
            slotIcon.sprite = null;
            slotIcon.enabled = false;
        }
    }


    public void OnDrop(PointerEventData eventData)
    {
        // 1. 드래그한 슬롯이 인벤토리 슬롯(Slot_UI)인지 확인
        Slot_UI sourceSlot = eventData.pointerDrag.GetComponent<Slot_UI>();
        if (sourceSlot == null || sourceSlot.currentItem == null)
        {
            return; // 인벤토리 슬롯이 아님
        }

        // 2. (문자열 비교) 아이템의 itemType과 이 슬롯이 요구하는 requiredType이 일치하는지 확인
        if (sourceSlot.currentItem.itemType == this.requiredType)
        {
            // 3. 타입이 일치하면, EquipmentManager에게 장착 요청
            bool success = EquipmentManager.Instance.EquipItem(sourceSlot.currentItem, sourceSlot.slotIndex);

            if (success)
            {
                sourceSlot.dropSuccessful = true;
            }
        }
        else
        {
            Debug.Log($"타입이 맞지 않습니다. 이 슬롯은 '{requiredType}'만 받습니다. (아이템 타입: '{sourceSlot.currentItem.itemType}')");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            dropSuccessful = false; // 플래그 리셋
            Sprite icon = Resources.Load<Sprite>(currentItem.iconPath);
            if (icon != null)
            {
                // NOTE: InventoryUIManager.Instance != null 체크는 편의상 생략했습니다.
                InventoryUIManager.Instance.StartDrag(icon);
                slotIcon.enabled = false;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 유령 아이콘 무조건 끔
        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.EndDrag();

        if (currentItem == null) return;

        // 드롭에 실패했고, UI 밖으로 버린 것도 아니라면 (제자리 복귀)
        if (!dropSuccessful && eventData.pointerEnter != null)
        {
            slotIcon.enabled = true;
        }

        dropSuccessful = false;
    }

    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return; // 슬롯이 비어있으면 아무것도 안 함

        // 1. 좌클릭 (한 번 클릭) -> 상세 정보 표시
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 1)
        {
            // Debug.Log("장비 상세정보 표시");
            if (InventoryUIManager.Instance != null)
                InventoryUIManager.Instance.UpdateDetails(currentItem);
        }

        // 2. 좌 더블클릭 (두 번 클릭) -> 장착 해제 시도
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
        {
            UnequipItemAttempt();
        }

        // ★ L126: 3. 우클릭 -> 장착 해제 시도 (요청 기능 추가)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            UnequipItemAttempt();
        }
    }

    /// <summary>
    /// 장착 해제를 시도합니다. (인벤토리가 꽉 찼는지 확인)
    /// </summary>
    private void UnequipItemAttempt()
    {
        if (currentItem == null) return;

        // (UnequipItem 함수가 알아서 빈 인벤토리 슬롯을 찾고, 스탯을 제거함)
        bool success = EquipmentManager.Instance.UnequipItem(currentItem, this.equipmentSlotIndex);

        if (!success)
        {
            Debug.Log("인벤토리가 꽉 차서 장비를 해제할 수 없습니다.");
        }
    }
}