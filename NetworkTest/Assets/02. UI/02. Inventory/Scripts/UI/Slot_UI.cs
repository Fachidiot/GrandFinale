using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

public class Slot_UI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region UI Components & Settings

    [Header("Components")]
    public Image slotIcon;
    public TextMeshProUGUI countText;
    [SerializeField] private Image slotBorderImage;
    [SerializeField] private Sprite normalBorderSprite;
    [SerializeField] private Sprite selectedBorderSprite;

    private static readonly string[] EquippableTypes = { "weapon", "artifact", "accessory", "equipment" };

    #endregion

    #region Data & State

    public InventoryItem currentItem { get; private set; }
    public InventoryItem currentSlot => currentItem;
    public int listIndex { get; private set; }
    public bool dropSuccessful = false;

    private ScrollRect parentScrollRect;
    private bool isDragging = false;

    // Coroutines
    private Coroutine tooltipCoroutine;
    private Coroutine singleClickCoroutine;

    #endregion

    #region Initialization & Binding

    void Start()
    {
        parentScrollRect = GetComponentInParent<ScrollRect>();
        SetSelected(false, false);
    }

    public void BindItem(InventoryItem item)
    {
        currentItem = item;
        UpdateSlotVisuals();
    }

    public void SetListIndex(int index)
    {
        this.listIndex = index;
    }

    public void ClearSlot()
    {
        currentItem = null;
        UpdateSlotVisuals();
    }

    public void SetBoundItem(InventoryItem newItem) => BindItem(newItem);

    #endregion

    #region Visual Updates

    void UpdateSlotVisuals()
    {
        // 아이템이 있고, 껍데기(IsEmpty)가 아니어야 함
        if (currentItem != null && currentItem.item != null)
        {
            Sprite icon = Resources.Load<Sprite>(currentItem.item.iconPath);
            slotIcon.sprite = icon;
            slotIcon.enabled = (icon != null);

            if (countText != null)
            {
                countText.text = currentItem.quantity > 1 ? currentItem.quantity.ToString() : "";
                countText.gameObject.SetActive(currentItem.quantity > 1);
                countText.fontSize = 14;
            }
        }
        else
        {
            slotIcon.enabled = false;
            slotIcon.sprite = null;
            if (countText != null) countText.gameObject.SetActive(false);
        }
        SetSelected(false, false);
    }

    public void SetSelected(bool selected, bool animate = true)
    {
        if (slotBorderImage != null)
        {
            slotBorderImage.sprite = selected ? selectedBorderSprite : normalBorderSprite;
            if (selected && animate) transform.DOPunchScale(Vector3.one * -0.05f, 0.15f);
        }
    }

    #endregion

    #region Input Handlers (Click)

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;
        if (currentItem == null || currentItem.item == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            AttemptEquip();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.clickCount == 1)
            {
                // 첫 번째 클릭: 대기 (더블클릭 판정 위해)
                if (singleClickCoroutine != null) StopCoroutine(singleClickCoroutine);
                singleClickCoroutine = StartCoroutine(HandleSingleClick());
            }
            else if (eventData.clickCount == 2)
            {
                // 더블 클릭: 대기 취소 후 장착 실행
                if (singleClickCoroutine != null) StopCoroutine(singleClickCoroutine);

                AttemptEquip();
                if (InventoryUIManager.Instance) InventoryUIManager.Instance.ClearDetails();
            }
        }
    }

    private IEnumerator HandleSingleClick()
    {
        yield return new WaitForSeconds(0.25f);

        // 더블클릭이 아니라고 판단되면 정보창 표시
        if (InventoryUIManager.Instance && currentItem != null && currentItem.item != null)
        {
            InventoryUIManager.Instance.HandleSlotClick(this, currentItem.item);
            SetSelected(true);
        }
    }

    #endregion

    #region Input Handlers (Drag & Drop)

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;

        // 스크롤 방향과 드래그 방향 비교하여 스크롤 뷰 제어
        if (parentScrollRect != null && (currentItem == null || Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x)))
        {
            parentScrollRect.OnBeginDrag(eventData);
            return;
        }

        if (currentItem == null) return;

        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.StartDrag(slotIcon.sprite);
            slotIcon.enabled = false;
            if (countText) countText.gameObject.SetActive(false);
            InventoryUIManager.Instance.ClearDetails();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        bool isUIDragging = InventoryUIManager.Instance != null && currentItem != null;

        if (parentScrollRect != null && (currentItem == null || !isUIDragging))
        {
            parentScrollRect.OnDrag(eventData);
        }
        else if (currentItem != null && InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance) InventoryUIManager.Instance.EndDrag();
        if (parentScrollRect != null) parentScrollRect.OnEndDrag(eventData);

        if (currentItem != null)
        {
            slotIcon.enabled = true;
            UpdateSlotVisuals();

            // UI 밖으로 드롭 시 아이템 버리기
            if (!dropSuccessful && !EventSystem.current.IsPointerOverGameObject())
            {
                InventoryManager.Instance.DropItem(currentItem);
            }
        }
        dropSuccessful = false;
        isDragging = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Slot_UI sourceSlot = eventData.pointerDrag.GetComponent<Slot_UI>();

        // 1. 인벤토리 -> 인벤토리 드롭
        if (sourceSlot != null && sourceSlot.currentItem != null)
        {
            if (InventoryManager.Instance != null)
            {
                // Manager가 타겟 슬롯 상태(빈칸/있음)를 확인하여 이동(Move) 또는 교환(Swap) 처리
                InventoryManager.Instance.MoveItemToEmptySlot(sourceSlot.currentItem, this.listIndex);

                sourceSlot.MarkDropSuccessful();
                this.MarkDropSuccessful();
            }
            return;
        }

        // 2. 장비 슬롯 -> 인벤토리 드롭 (장비 해제)
        EquipmentSlot_UI equipSlot = eventData.pointerDrag.GetComponent<EquipmentSlot_UI>();
        if (equipSlot != null && equipSlot.currentItem != null)
        {
            EquipmentManager.Instance.UnequipItem(equipSlot.currentItem);
            equipSlot.MarkDropSuccessful();
            this.MarkDropSuccessful();
        }
    }

    public void MarkDropSuccessful() => dropSuccessful = true;
    public void OnDropSuccess() => MarkDropSuccessful();

    #endregion

    #region Helpers & Tooltip

    private void AttemptEquip()
    {
        if (currentItem == null || currentItem.item == null) return;
        string typeStr = currentItem.item.itemTypeEnum.ToString().ToLower();

        if (IsEquippableType(typeStr))
        {
            EquipmentManager.Instance.EquipItemToFirstAvailableSlot(currentItem.item);
        }
    }

    private bool IsEquippableType(string type)
    {
        foreach (var t in EquippableTypes) if (t == type) return true;
        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem == null) return;
        if (AudioManager.Instance) AudioManager.Instance.PlaySFX(AudioManager.Instance.uiHoverClip);
        tooltipCoroutine = StartCoroutine(ShowTooltipDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        if (InventoryUIManager.Instance) InventoryUIManager.Instance.HideTooltip();
    }

    private IEnumerator ShowTooltipDelay()
    {
        yield return new WaitForSeconds(0.5f);
        if (InventoryUIManager.Instance && currentItem != null)
            InventoryUIManager.Instance.ShowTooltip(currentItem.item, transform.position);
    }

    #endregion
}