using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlot_UI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region Serialized Fields & Settings

    [Header("Filter Settings")]
    public EquipmentSlot requiredSlotType = EquipmentSlot.None;
    public ItemType requiredItemType = ItemType.Etc;
    public List<WeaponType> allowedWeaponTypes;

    [Header("UI Components")]
    public Image slotIcon;
    public TextMeshProUGUI slotNameText;

    [Header("State")]
    public int equipmentSlotIndex;
    public RelicData currentItem { get; private set; }
    public bool dropSuccessful = false;

    private bool isIgnoringClick = false;
    private Coroutine hideTooltipCoroutine;
    private Coroutine tooltipCoroutine;
    private const float TooltipDelay = 0.5f;

    #endregion

    #region Initialization

    void Start()
    {
        EquipmentManager.OnEquipmentChanged += UpdateSlotVisuals;
        UpdateSlotVisuals();
    }

    void OnDestroy()
    {
        EquipmentManager.OnEquipmentChanged -= UpdateSlotVisuals;
    }

    #endregion

    #region Visual Updates

    void UpdateSlotVisuals()
    {
        if (EquipmentManager.Instance == null) return;

        currentItem = EquipmentManager.Instance.equipmentSlots[equipmentSlotIndex];

        if (HasValidItem()) DisplayItem();
        else HideItem();
    }

    private bool HasValidItem()
    {
        return currentItem != null && !string.IsNullOrEmpty(currentItem.iconPath);
    }

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

    #endregion

    #region Drag & Drop Handlers

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

    private bool IsValidSourceSlot(Slot_UI sourceSlot)
    {
        return sourceSlot != null &&
               sourceSlot.currentSlot != null &&
               sourceSlot.currentSlot.item != null;
    }

    private void TryEquipItem(Slot_UI sourceSlot, RelicData itemToEquip)
    {
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

    private IEnumerator IgnoreNextClick()
    {
        isIgnoringClick = true;
        yield return null;
        isIgnoringClick = false;
    }

    public bool CanEquipItem(RelicData item)
    {
        if (item == null) return false;


        if (requiredItemType != ItemType.Etc)
        {
            if (item.itemTypeEnum != requiredItemType)
            {
                // 디버깅용: 타입이 안 맞음
                return false;
            }
        }


        if (requiredSlotType != EquipmentSlot.None)
        {
            if (item.equipmentSlot != requiredSlotType)
            {
                return false;
            }
        }

        if (item.itemTypeEnum == ItemType.Weapon)
        {
            // 이 슬롯에 허용된 무기 리스트가 있고, 비어있지 않다면 검사
            if (allowedWeaponTypes != null && allowedWeaponTypes.Count > 0)
            {
                // 내 허용 리스트에 이 아이템의 타입이 없다면 차단
                if (!allowedWeaponTypes.Contains(item.weaponType))
                {
                    return false;
                }
            }
        }

        return true;
    }

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

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

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

    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    #endregion

    #region Input Handlers (Click & Hover)

    public void OnPointerClick(PointerEventData eventData)
    {
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

    private void UnequipItemAttempt()
    {
        if (currentItem == null) return;
        EquipmentManager.Instance.UnequipItem(currentItem);
    }

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

    #endregion
}