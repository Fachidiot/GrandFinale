using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class EquipmentSlot_UI : MonoBehaviour, IDropHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("필터 설정")]
    public EquipmentSlot requiredSlotType = EquipmentSlot.None;
    public ItemType requiredItemType = ItemType.Etc;

    [Header("UI")]
    public Image slotIcon;
    public TextMeshProUGUI slotNameText;

    public int equipmentSlotIndex;
    public RelicData currentItem { get; private set; }
    public bool dropSuccessful = false;

    private bool isIgnoringClick = false;
    private Coroutine hideTooltipCoroutine;
    private Coroutine tooltipCoroutine;
    private const float TooltipDelay = 0.5f;

    void Start()
    {
        EquipmentManager.OnEquipmentChanged += UpdateSlotVisuals;
        UpdateSlotVisuals();
    }

    void OnDestroy()
    {
        EquipmentManager.OnEquipmentChanged -= UpdateSlotVisuals;
    }

    // 슬롯 비주얼 업데이트
    void UpdateSlotVisuals()
    {
        if (EquipmentManager.Instance == null) return;

        currentItem = EquipmentManager.Instance.equipmentSlots[equipmentSlotIndex];

        if (HasValidItem())
        {
            DisplayItem();
        }
        else
        {
            HideItem();
        }
    }

    // 유효한 아이템이 있는지 확인
    private bool HasValidItem()
    {
        return currentItem != null && !string.IsNullOrEmpty(currentItem.iconPath);
    }

    // 아이템 표시
    private void DisplayItem()
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

        if (slotNameText != null)
        {
            slotNameText.text = currentItem.itemName;
            slotNameText.gameObject.SetActive(true);
        }
    }

    // 아이템 숨기기
    private void HideItem()
    {
        slotIcon.sprite = null;
        slotIcon.enabled = false;

        if (slotNameText != null)
        {
            slotNameText.text = "";
            slotNameText.gameObject.SetActive(false);
        }
    }

    // 드롭 이벤트 처리
    public void OnDrop(PointerEventData eventData)
    {
        Slot_UI sourceSlot = eventData.pointerDrag.GetComponent<Slot_UI>();

        if (!IsValidSourceSlot(sourceSlot)) return;

        RelicData itemToEquip = sourceSlot.currentSlot.item;

        if (CanEquipItem(itemToEquip))
        {
            TryEquipItem(sourceSlot, itemToEquip);
        }
    }

    // 소스 슬롯 유효성 검사
    private bool IsValidSourceSlot(Slot_UI sourceSlot)
    {
        return sourceSlot != null &&
               sourceSlot.currentSlot != null &&
               sourceSlot.currentSlot.item != null;
    }

    // 아이템 장착 시도
    private void TryEquipItem(Slot_UI sourceSlot, RelicData itemToEquip)
    {
        // [수정된 부분] 인자 2개만 전달 (아이템, 장착할 슬롯 인덱스)
        bool success = EquipmentManager.Instance.EquipItem(
            itemToEquip,
            this.equipmentSlotIndex
        );

        if (success)
        {
            sourceSlot.dropSuccessful = true;
            StartCoroutine(IgnoreNextClick());
        }
    }

    // 다음 클릭 무시
    private IEnumerator IgnoreNextClick()
    {
        isIgnoringClick = true;
        yield return null;
        isIgnoringClick = false;
    }

    // 아이템 장착 가능 여부 확인
    public bool CanEquipItem(RelicData item)
    {
        if (item == null) return false;

        if (requiredSlotType != EquipmentSlot.None)
        {
            return item.equipmentSlot == requiredSlotType;
        }

        if (requiredItemType != ItemType.Etc)
        {
            return item.itemTypeEnum == requiredItemType;
        }

        return false;
    }

    // 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;

        dropSuccessful = false;
        Sprite icon = Resources.Load<Sprite>(currentItem.iconPath);

        if (icon != null)
        {
            InventoryUIManager.Instance.StartDrag(icon);
            slotIcon.enabled = false;
        }
    }

    // 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    // 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.EndDrag();

        if (currentItem == null) return;

        if (!dropSuccessful && eventData.pointerEnter != null)
        {
            slotIcon.enabled = true;
        }

        dropSuccessful = false;
    }

    // 드롭 성공 표시
    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    // 클릭 이벤트 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        // 드롭 직후 클릭 무시
        if (isIgnoringClick)
        {
            isIgnoringClick = false;
            return;
        }

        if (currentItem == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            UnequipItemAttempt();
        }
        else if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
        {
            UnequipItemAttempt();
        }
    }

    // 장비 해제 시도
    private void UnequipItemAttempt()
    {
        if (currentItem == null) return;

        // [수정된 부분] 인자 1개만 전달 (아이템 데이터) - 인덱스 제거
        EquipmentManager.Instance.UnequipItem(currentItem);
    }

    // 마우스 진입
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        if (currentItem == null) return;

        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(currentItem));
    }

    // 마우스 이탈
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = null;

        if (hideTooltipCoroutine != null) StopCoroutine(hideTooltipCoroutine);
        hideTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay(0.1f));
    }

    // 툴팁 표시 (딜레이 후)
    private IEnumerator ShowTooltipAfterDelay(RelicData item)
    {
        yield return new WaitForSeconds(TooltipDelay);

        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.ShowTooltip(item, transform.position);
        }
    }

    // 툴팁 숨기기 (딜레이 후)
    private IEnumerator HideTooltipAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.HideTooltip();
        }

        hideTooltipCoroutine = null;
    }
}