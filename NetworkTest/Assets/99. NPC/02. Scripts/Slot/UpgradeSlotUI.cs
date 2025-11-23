using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UpgradeSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    [Header("UI Components")]
    [SerializeField] private Image iconImage;

    public UpgradeModule _module;

    public RelicData CurrentItem { get; private set; }

    private void Awake()
    {
        _module = GetComponentInParent<UpgradeModule>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2 && CurrentItem != null)
        {
            if (_module != null) _module.ReturnEquipmentToInventory();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CurrentItem == null || InventoryUIManager.Instance == null) return;

        // 아이콘 이미지로 드래그 시작 (InventoryUIManager 활용)
        if (iconImage != null && iconImage.sprite != null)
        {
            InventoryUIManager.Instance.StartDrag(iconImage.sprite);
            iconImage.color = new Color(1, 1, 1, 0.5f);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (CurrentItem != null && InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance != null) InventoryUIManager.Instance.EndDrag();

        if (iconImage != null) iconImage.color = Color.white;
    }
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[UpgradeSlotUI] OnDrop 발생! 대상: {gameObject.name}");

        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        Slot_UI sourceSlot = draggedObject.GetComponent<Slot_UI>();

        // 유효성 검사
        if (sourceSlot == null || sourceSlot.currentSlot == null || sourceSlot.currentSlot.item == null)
            return;

        // 인벤토리에서 사라지기 전에, 아이템 정보를 미리 복사
        RelicData droppedItem = sourceSlot.currentSlot.item;
        int slotIndex = sourceSlot.currentSlot.slotIndex;

        if (_module != null)
        {
            // 저장해둔 droppedItem을 사용해서 전달
            bool success = _module.HandleEquipmentDrop(droppedItem, slotIndex);

            if (success)
            {
                Debug.Log("[UpgradeSlotUI] 드롭 처리 성공 -> UI 갱신");

                UpdateUI(droppedItem);

                sourceSlot.MarkDropSuccessful();
            }
        }
    }

    public void UpdateUI(RelicData item)
    {
        if (item == null)
        {
            Debug.LogError("[UpgradeSlotUI] UpdateUI에 전달된 아이템이 null입니다!");
            return;
        }

        CurrentItem = item;


        if (iconImage != null)
        {
            if (!string.IsNullOrEmpty(item.iconPath))
            {
                Sprite loadedSprite = Resources.Load<Sprite>(item.iconPath);
                if (loadedSprite != null)
                {
                    iconImage.sprite = loadedSprite;
                    iconImage.enabled = true;
                    iconImage.color = Color.white;
                }
                else
                {
                    Debug.LogError($"[UpgradeSlotUI] 아이콘 로드 실패: {item.iconPath}");
                    iconImage.enabled = false;
                }
            }
            else
            {
                Debug.LogWarning("[UpgradeSlotUI] 아이템의 iconPath가 비어있습니다.");
                iconImage.enabled = false;
            }
        }
        else
        {
            Debug.LogError("[UpgradeSlotUI] iconImage 컴포넌트가 연결되지 않았습니다! 인스펙터를 확인하세요.");
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
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("마우스 감지 : {gameObject.name}");
    }
}