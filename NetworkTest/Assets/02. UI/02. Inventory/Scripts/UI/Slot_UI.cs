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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasValidItem()) return;

        // 1. 우클릭 처리 (장착 시도)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (HasValidItem())
            {
                AttemptEquip();
            }
            return; 
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

    private IEnumerator HandleSingleClick()
    {
        yield return new WaitForSeconds(0.2f); // 0.2초 대기

        if (InventoryUIManager.Instance != null)
        {
            // UIManager의 새 클릭 핸들러를 호출 (선택 상태와 정보창을 모두 관리)
            InventoryUIManager.Instance.HandleSlotClick(this, HasValidItem() ? currentSlot.item : null);
        }

        singleClickCoroutine = null;
    }


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

        if (slotBackground != null && InventoryUIManager.Instance != null)
        {
        // 어떤 여자가 좋을까
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

        UpgradeSlotUI upgradeSlot = eventData.pointerDrag.GetComponent<UpgradeSlotUI>();
        if (upgradeSlot != null && upgradeSlot.CurrentItem != null)
        {
            if (upgradeSlot._module != null)
            {
                upgradeSlot._module.ReturnEquipmentToInventory();
            }
            return;
        }

        // 강화 재료 슬롯 -> 인벤토리 이동
        MaterialSlotUI materialSlot = eventData.pointerDrag.GetComponent<MaterialSlotUI>();
        if (materialSlot != null && materialSlot.CurrentItem != null)
        {
            if (materialSlot._module != null)
            {
                materialSlot._module.ReturnMaterialToInventory(materialSlot);
            }
            return;
        }
    }

    private void HandleInventorySlotDrop(Slot_UI sourceSlot)
    {
        // 출발지의 데이터가 없으면 리턴
        if (sourceSlot.currentSlot == null || sourceSlot.currentSlot.slotIndex == -1) return;
        if (sourceSlot == this) return;

        if (currentSlot.slotIndex == -1)
        {
            // 필터링된 탭(전체 탭이 아님)이라면, 빈 공간으로의 이동은 불가능하므로 리턴
            if (InventoryManager.Instance.currentFilter != InventoryFilterType.All)
            {
                Debug.Log("필터링된 탭에서는 빈 공간으로 이동할 수 없습니다.");
                return;
            }
            return;
        }

        // 정상적인 아이템 간 교환
        InventoryManager.Instance.SwapItems(sourceSlot.currentSlot.slotIndex, currentSlot.slotIndex);

        sourceSlot.dropSuccessful = true;
        dropSuccessful = true;

        StartCoroutine(PlayDropAnimationAfterFrame());
    }

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

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.uiHoverClip);

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
        //return InventoryManager.Instance.currentFilter != InventoryFilterType.Relic;
        return true;
    }
}