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
    [Header("Components")]
    public Image slotIcon;
    public TextMeshProUGUI countText;
    [SerializeField] private Image slotBorderImage;
    [SerializeField] private Sprite normalBorderSprite;
    [SerializeField] private Sprite selectedBorderSprite;

    public InventoryItem currentItem { get; private set; }
    public InventoryItem currentSlot => currentItem;
    public bool dropSuccessful = false;
    public int listIndex { get; private set; }

    private ScrollRect parentScrollRect;
    private bool isDragging = false;
    private Coroutine tooltipCoroutine;

    private static readonly string[] EquippableTypes = { "weapon", "artifact", "accessory", "equipment" };

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

    void UpdateSlotVisuals()
    {
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
            if (eventData.clickCount == 2)
            {
                AttemptEquip();
                if (InventoryUIManager.Instance) InventoryUIManager.Instance.ClearDetails();
            }
            else
            {
                if (InventoryUIManager.Instance)
                    InventoryUIManager.Instance.HandleSlotClick(this, currentItem.item);
                SetSelected(true);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;

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
        // [수정] IsDragging 속성이 없어서 오류가 났다면, 간단히 currentItem 체크로 대체
        bool isUIDragging = InventoryUIManager.Instance != null && currentItem != null;
        // 만약 InventoryUIManager에 IsDragging 프로퍼티가 있다면 && InventoryUIManager.Instance.IsDragging 사용

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
        
        // 인벤토리 -> 인벤토리 드롭 처리
if (sourceSlot != null && sourceSlot.currentItem != null)
        {
            // 현재 슬롯(this)이 비어있는 경우
            if (this.currentItem == null)
            {
                if (InventoryManager.Instance != null)
                {
                    Debug.Log("빈 슬롯으로 이동 요청");

                    InventoryManager.Instance.MoveItemToEmptySlot(sourceSlot.currentItem, this.listIndex);
                    sourceSlot.MarkDropSuccessful();
                    this.MarkDropSuccessful();
                }
            }
            // 현재 슬롯에 아이템이 있는 경우 (스왑)
            else
            {
                InventoryManager.Instance.SwapItems(sourceSlot.currentItem, this.currentItem);
                sourceSlot.MarkDropSuccessful();
                this.MarkDropSuccessful();
            }
            return;
        }

        // 장비 슬롯 -> 인벤토리 드롭 처리
        EquipmentSlot_UI equipSlot = eventData.pointerDrag.GetComponent<EquipmentSlot_UI>();
        if (equipSlot != null && equipSlot.currentItem != null)
        {
            EquipmentManager.Instance.UnequipItem(equipSlot.currentItem);
            equipSlot.MarkDropSuccessful();
            this.MarkDropSuccessful();
        }
    }

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

    public void MarkDropSuccessful() => dropSuccessful = true;
    public void OnDropSuccess() => MarkDropSuccessful();
    public void SetSelected(bool selected, bool animate = true)
    {
        if (slotBorderImage != null)
        {
            slotBorderImage.sprite = selected ? selectedBorderSprite : normalBorderSprite;
            if (selected && animate) transform.DOPunchScale(Vector3.one * -0.05f, 0.15f);
        }
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

    public void SetBoundItem(InventoryItem newItem) => BindItem(newItem);
}