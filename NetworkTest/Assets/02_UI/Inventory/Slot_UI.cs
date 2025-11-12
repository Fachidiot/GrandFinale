using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections; // Coroutine 사용을 위해 필요

public class Slot_UI : MonoBehaviour, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler // L8: IPointerEnterHandler 추가
{
    public int slotIndex;
    public Image slotIcon;
    public bool dropSuccessful = false;

    private Coroutine tooltipCoroutine;
    private const float TooltipDelay = 0.5f;

    public RelicData currentItem { get; private set; }

    void Start()
    {
        InventoryManager.OnInventoryChanged += UpdateSlotVisuals;
        UpdateSlotVisuals();
    }
    void OnDestroy()
    {
        InventoryManager.OnInventoryChanged -= UpdateSlotVisuals;
    }

    void UpdateSlotVisuals()
    {
        currentItem = InventoryManager.Instance.inventorySlots[slotIndex];

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
                Debug.LogWarning($"아이콘을 로드할 수 없습니다: {currentItem.iconPath}");
                slotIcon.enabled = false;
            }
        }
        else
        {
            slotIcon.sprite = null;
            slotIcon.enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return;

        // 1. 좌클릭 (상세 정보만 업데이트)
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 1)
        {
            if (InventoryUIManager.Instance != null)
                InventoryUIManager.Instance.UpdateDetails(currentItem);
        }

        // 2. 좌 더블클릭 (요청 기능: 장착 시도)
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
        {
            AttemptEquip();
        }

        // 3. 우클릭 (요청 기능: 장착 시도)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            AttemptEquip();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null && InventoryUIManager.Instance != null)
        {
            dropSuccessful = false;

            Sprite icon = Resources.Load<Sprite>(currentItem.iconPath);
            if (icon != null)
            {
                InventoryUIManager.Instance.StartDrag(icon);
                slotIcon.enabled = false;
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        Slot_UI targetSlot = this;

        Slot_UI sourceInventorySlot = eventData.pointerDrag.GetComponent<Slot_UI>();
        if (sourceInventorySlot != null)
        {
            // [인벤토리 -> 인벤토리] 아이템 교체
            if (sourceInventorySlot != targetSlot)
            {
                InventoryManager.Instance.SwapItems(sourceInventorySlot.slotIndex, targetSlot.slotIndex);
                sourceInventorySlot.dropSuccessful = true;
            }
            return;
        }

        EquipmentSlot_UI sourceEquipSlot = eventData.pointerDrag.GetComponent<EquipmentSlot_UI>();
        if (sourceEquipSlot != null && sourceEquipSlot.currentItem != null)
        {
            // 장비 해제 시도 (인벤토리로 이동)
            bool success = EquipmentManager.Instance.UnequipItem(sourceEquipSlot.currentItem, sourceEquipSlot.equipmentSlotIndex);

            if (success)
            {
                sourceEquipSlot.MarkDropSuccessful();
                dropSuccessful = true; // 인벤토리 슬롯에도 성공 플래그 설정
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.EndDrag(); // 유령 아이콘은 무조건 끔

        // 1. UI 밖으로 드롭했는지 확인 (pointerEnter == null)
        if (eventData.pointerEnter == null)
        {
            if (currentItem != null)
            {
                // 인게임에 드랍
                InventoryManager.Instance.DropItem(this.slotIndex);
                dropSuccessful = true;
            }
        }

        if (!dropSuccessful)
        {
            // UI 안의 유효하지 않은 곳에 놓았을 경우: 원본 슬롯 아이콘을 즉시 복구합니다.
            slotIcon.enabled = true;
        }

        dropSuccessful = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem != null && InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    private void AttemptEquip()
    {
        string requiredType = "Relic"; // 장착 가능 여부 확인 (RelicData의 itemType과 비교해야 함)

        if (currentItem != null && currentItem.itemType == requiredType)
        {
            // 장착 시도
            bool success = EquipmentManager.Instance.EquipItem(currentItem, this.slotIndex);

            if (!success)
            {
                Debug.Log("장비 슬롯이 꽉 찼습니다.");
            }
        }
        else
        {
            Debug.Log($"이 아이템({currentItem.itemName})은 장착할 수 없습니다. (타입: {currentItem.itemType})");
        }
    }

    // ★ L200: OnPointerEnter 함수
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem == null) return;

        // 마우스가 슬롯에 들어왔을 때, 딜레이 후 툴팁을 띄우는 코루틴 시작
        // 기존 코루틴이 실행 중이면 중복 실행을 막기 위해 멈추는 로직이 있으면 더 안전합니다.
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(currentItem));
    }

    // L209: OnPointerExit 함수
    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스가 슬롯을 벗어났을 때, 툴팁 코루틴을 중지하고 툴팁 숨김
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
        }
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.HideTooltip();
        }
    }

    // L221: ShowTooltipAfterDelay 코루틴
    private IEnumerator ShowTooltipAfterDelay(RelicData item)
    {
        yield return new WaitForSeconds(TooltipDelay);

        // 0.5초 후에도 여전히 마우스가 위에 있다면 툴팁 표시
        if (InventoryUIManager.Instance != null)
        {
            // 툴팁 표시 (현재 슬롯 위치 기준)
            InventoryUIManager.Instance.ShowTooltip(item, transform.position);
        }
    }
}