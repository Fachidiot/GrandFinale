using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

public class Slot_UI : MonoBehaviour, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Components")]
    public Image slotIcon;
    public TextMeshProUGUI countText;

    [Header("Slot Visuals (Click Effect)")]
    [Tooltip("슬롯의 배경/테두리 이미지")]
    [SerializeField] private Image slotBorderImage;
    [Tooltip("기본 상태의 슬롯 테두리 스프라이트")]
    [SerializeField] private Sprite normalBorderSprite;
    [Tooltip("선택됐을 때(눌렀을 때)의 슬롯 테두리 스프라이트")]
    [SerializeField] private Sprite selectedBorderSprite;


    [Tooltip("선택 시 색상이 변경될 배경/테두리 이미지")]
    public Image slotBackground;
    public Color defaultColor = Color.white;
    public Color selectedColor = Color.yellow;

    public bool dropSuccessful = false;

    private Coroutine hideTooltipCoroutine;
    private Coroutine tooltipCoroutine;
    private const float TooltipDelay = 0.5f;

    public InventorySlot currentSlot { get; private set; }
    private Coroutine singleClickCoroutine;

    private ScrollRect parentScrollRect;
    private bool isScrolling = false;
    private bool isSelected = false;

    private static readonly string[] EquippableTypes =
    {
        ItemType.Weapon.ToString().ToLower(),
        ItemType.Artifact.ToString().ToLower(),
        ItemType.Accessory.ToString().ToLower(),
        ItemType.Equipment.ToString().ToLower()
    };

    void Start()
    {
        parentScrollRect = GetComponentInParent<ScrollRect>();
        SetSelected(false, false); // 초기 테두리 설정 (애니메이션 없이)
    }

    /// <summary>
    /// InventoryUIManager에 의해 호출되어 슬롯의 선택 상태를 설정합니다.
    /// </summary>
    public void SetSelected(bool selected, bool playAnimation = true)
    {
        isSelected = selected;
        if (slotBorderImage == null) return;

        if (isSelected)
        {
            slotBorderImage.sprite = selectedBorderSprite;
            if (playAnimation)
            {
                // 클릭 시 펀치 효과
                transform.DOPunchScale(new Vector3(-0.05f, -0.05f, 0), 0.15f, 1, 0.5f);
            }
        }
        else
        {
            slotBorderImage.sprite = normalBorderSprite;
        }
    }

    // ▼▼▼ [수정] OnPointerClick 구문 오류 수정 및 로직 정리 ▼▼▼
    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. 우클릭 처리 (장착 시도)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (HasValidItem())
            {
                AttemptEquip();
            }
            return; // 우클릭 시 좌클릭 로직(선택)은 실행하지 않음
        }

        // 2. 좌클릭 처리
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 스크롤 중에는 클릭 무시
            if (isScrolling) return;

            if (eventData.clickCount == 1)
            {
                // 싱글 클릭 (코루틴 시작)
                if (singleClickCoroutine != null)
                    StopCoroutine(singleClickCoroutine);

                if (IsMainInventoryView())
                    singleClickCoroutine = StartCoroutine(HandleSingleClick());
            }
            else if (eventData.clickCount == 2)
            {
                // 더블 클릭 (장착 시도)
                if (singleClickCoroutine != null)
                {
                    StopCoroutine(singleClickCoroutine);
                    singleClickCoroutine = null;
                }

                AttemptEquip();

                if (InventoryUIManager.Instance != null)
                    InventoryUIManager.Instance.ClearDetails();
            }
        }
    }
    // ▲▲▲ [수정 완료] ▲▲▲

    // ▼▼▼ [수정] HandleSingleClick이 UIManager의 새 함수를 호출하도록 변경 ▼▼▼
    private IEnumerator HandleSingleClick()
    {
        yield return new WaitForSeconds(0.2f); // 0.2초 대기 (더블클릭 구분)

        if (InventoryUIManager.Instance != null)
        {
            // UIManager의 새 클릭 핸들러를 호출 (선택 상태와 정보창을 모두 관리)
            // 아이템이 없으면 null을 전달
            InventoryUIManager.Instance.HandleSlotClick(this, HasValidItem() ? currentSlot.item : null);
        }

        singleClickCoroutine = null;
    }
    // ▲▲▲ [수정 완료] ▲▲▲

    // (참고: OnPointerClick에서 사용하던 HandleLeftClick, HandleDoubleClick 등 불필요한 메서드 제거)

    public void SetBoundItem(InventorySlot newSlot)
    {
        currentSlot = newSlot;
        UpdateSlotVisuals();
    }

    void UpdateSlotVisuals()
    {
        if (slotIcon == null) return;

        if (HasValidItem())
        {
            DisplayItemIcon();
            DisplayItemCount();
        }
        else
        {
            HideSlot();
        }

        // UIManager의 SetSelected 로직이 스프라이트를 제어하므로
        // 이 로직은 주석 처리하거나, UIManager의 로직과 통일해야 합니다.
        // if (!isSelected && slotBorderImage != null)
        // {
        //     slotBorderImage.sprite = normalBorderSprite;
        // }

        // UIManager의 SetSelected와 이 색상 로직이 충돌할 수 있습니다.
        // UIManager가 관리하는 `currentSelectedSlot`을 사용하도록 변경합니다.
        if (slotBackground != null && InventoryUIManager.Instance != null)
        {
            // InventoryUIManager.Instance.GetSelectedSlotIndex() 같은 함수가 필요하지만,
            // 현재 UIManager에는 없으므로, UIManager의 SetSelected가 이 로직을 대신해야 합니다.
            // 여기서는 일단 기존 로직을 유지하되, UIManager의 SetSelected가
            // slotBorderImage.sprite와 slotBackground.color를 모두 제어하는 것을 권장합니다.

            // bool isSelected = (currentSlot != null && ...);
            // slotBackground.color = isSelected ? selectedColor : defaultColor;
        }
    }

    private bool HasValidItem()
    {
        return currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1;
    }

    private void DisplayItemIcon()
    {
        Sprite icon = Resources.Load<Sprite>(currentSlot.item.iconPath);

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

    private void DisplayItemCount()
    {
        if (countText != null)
        {
            countText.text = currentSlot.quantity.ToString();
            countText.gameObject.SetActive(true);
            countText.fontSize = 10;
        }
    }

    private void HideSlot()
    {
        slotIcon.sprite = null;
        slotIcon.enabled = false;

        if (countText != null)
        {
            countText.gameObject.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentScrollRect == null) return;

        if (!HasValidItem())
        {
            isScrolling = true;
            parentScrollRect.OnBeginDrag(eventData);
            return;
        }

        // 아이템이 있으면 무조건 드래그 (스크롤 금지)
        isScrolling = false;

        if (InventoryUIManager.Instance == null) return;

        dropSuccessful = false;
        Sprite icon = Resources.Load<Sprite>(currentSlot.item.iconPath);

        if (icon != null)
        {
            InventoryUIManager.Instance.StartDrag(icon);
            slotIcon.enabled = false;
            if (countText != null) countText.gameObject.SetActive(false);
        }

        // 드래그 시작 시 선택 해제
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.ClearDetails();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.slotIndex == -1) return;

        Slot_UI sourceSlot = FindSlotComponent(eventData.pointerDrag);

        if (sourceSlot != null)
        {
            HandleInventorySlotDrop(sourceSlot);
            return;
        }

        EquipmentSlot_UI sourceEquipSlot = FindEquipmentSlotComponent(eventData.pointerDrag);

        if (sourceEquipSlot != null && sourceEquipSlot.currentItem != null)
        {
            HandleEquipmentSlotDrop(sourceEquipSlot);
        }
    }

    private void HandleInventorySlotDrop(Slot_UI sourceSlot)
    {
        if (sourceSlot.currentSlot == null || sourceSlot.currentSlot.slotIndex == -1) return;
        if (sourceSlot == this) return;

        InventoryManager.Instance.SwapItems(sourceSlot.currentSlot.slotIndex, currentSlot.slotIndex);
        sourceSlot.dropSuccessful = true;
        dropSuccessful = true;

        // ▼▼▼ [추가] 아이템이 놓였을 때 애니메이션 재생 (요청 사항) ▼▼▼
        StartCoroutine(PlayDropAnimationAfterFrame());
        // ▲▲▲ [추가 완료] ▲▲▲
    }

    // ▼▼▼ [추가] 아이템 드롭 애니메이션 관련 함수 ▼▼▼
    private IEnumerator PlayDropAnimationAfterFrame()
    {
        // OnInventoryChanged 이벤트가 UI를 업데이트할 때까지 한 프레임 대기
        yield return null;

        PlayDropAnimation();
    }

    private void PlayDropAnimation()
    {
        // 아이콘이 활성화되어 있을 때(아이템이 있을 때)만 재생
        if (slotIcon != null && slotIcon.enabled)
        {
            slotIcon.transform.DOKill(); // 기존 애니메이션 중지
            slotIcon.transform.localScale = Vector3.one * 0.8f; // 80% 크기에서 시작
            slotIcon.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack); // 오버슈트 효과
        }
    }
    // ▲▲▲ [추가 완료] ▲▲▲

    private void HandleEquipmentSlotDrop(EquipmentSlot_UI sourceEquipSlot)
    {
        bool success = EquipmentManager.Instance.UnequipItem(sourceEquipSlot.currentItem, sourceEquipSlot.equipmentSlotIndex);
        if (success)
        {
            sourceEquipSlot.MarkDropSuccessful();
            dropSuccessful = true;
        }
    }

    private Slot_UI FindSlotComponent(GameObject obj)
    {
        if (obj == null) return null;
        Slot_UI slot = obj.GetComponent<Slot_UI>() ?? obj.GetComponentInParent<Slot_UI>() ?? obj.GetComponentInChildren<Slot_UI>();
        return slot;
    }

    private EquipmentSlot_UI FindEquipmentSlotComponent(GameObject obj)
    {
        if (obj == null) return null;
        EquipmentSlot_UI slot = obj.GetComponent<EquipmentSlot_UI>() ?? obj.GetComponentInParent<EquipmentSlot_UI>() ?? obj.GetComponentInChildren<EquipmentSlot_UI>();
        return slot;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isScrolling)
        {
            if (parentScrollRect != null)
            {
                parentScrollRect.OnEndDrag(eventData);
            }
        }
        else
        {
            if (InventoryUIManager.Instance != null)
                InventoryUIManager.Instance.EndDrag();

            if (HasValidItem())
            {
                if (!dropSuccessful)
                {
                    if (ShouldDropItem(eventData))
                    {
                        DropItemOutsideInventory();
                    }
                    else
                    {
                        UpdateSlotVisuals();
                    }
                }
            }
            else if (!dropSuccessful)
            {
                UpdateSlotVisuals();
            }
        }

        dropSuccessful = false;
        isScrolling = false;
    }

    private bool ShouldDropItem(PointerEventData eventData)
    {
        if (eventData.pointerEnter == null)
        {
            return !IsInsideInventoryArea(eventData);
        }
        return false;
    }

    private bool IsInsideInventoryArea(PointerEventData eventData)
    {
        if (eventData.pointerEnter == null) return false;

        Transform current = eventData.pointerEnter.transform;
        while (current != null)
        {
            if (IsInventoryRelatedObject(current.name))
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    private bool IsInventoryRelatedObject(string objectName)
    {
        return objectName.Contains("Inventory") ||
               objectName.Contains("Slot") ||
               objectName.Contains("ScrollView") ||
               objectName.Contains("Viewport") ||
               objectName == "FullInventory_Inventory_UI" ||
               objectName == "Small_Inventory_UI";
    }

    private void DropItemOutsideInventory()
    {
        if (HasValidItem())
        {
            InventoryManager.Instance.DropItem(currentSlot.slotIndex);
            dropSuccessful = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isScrolling)
        {
            if (parentScrollRect != null)
            {
                parentScrollRect.OnDrag(eventData);
            }
        }
        else
        {
            if (HasValidItem() && InventoryUIManager.Instance != null)
            {
                InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
            }
        }
    }

    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    private void AttemptEquip()
    {
        if (!HasValidItem()) return;

        RelicData itemToEquip = currentSlot.item;
        string currentItemTypeString = itemToEquip.itemTypeEnum.ToString().ToLower();

        if (IsEquippableType(currentItemTypeString))
        {
            EquipmentManager.Instance.EquipItemToFirstAvailableSlot(itemToEquip, currentSlot.slotIndex);
        }
    }

    private bool IsEquippableType(string itemTypeString)
    {
        foreach (var type in EquippableTypes)
        {
            if (itemTypeString == type)
            {
                return true;
            }
        }
        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        if (!HasValidItem()) return;

        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(currentSlot.item));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = null;

        if (hideTooltipCoroutine != null) StopCoroutine(hideTooltipCoroutine);
        hideTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay(0.1f));
    }

    private IEnumerator ShowTooltipAfterDelay(RelicData item)
    {
        yield return new WaitForSeconds(TooltipDelay);
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.ShowTooltip(item, transform.position);
        }
    }

    private IEnumerator HideTooltipAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.HideTooltip();
        }
        hideTooltipCoroutine = null;
    }

    private bool IsMainInventoryView()
    {
        return InventoryManager.Instance.currentFilter != InventoryFilterType.Relic;
    }
}