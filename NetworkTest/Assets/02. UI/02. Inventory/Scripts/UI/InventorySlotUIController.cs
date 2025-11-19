using UnityEngine;
using System.Collections.Generic;

public class InventorySlotUIController : MonoBehaviour
{
    [Header("인벤토리 슬롯 UI")]
    [SerializeField] private List<Slot_UI> allInventorySlots;

    [Header("자동 설정")]
    [SerializeField] private bool autoFindSlots = false;

    void Start()
    {
        InitializeSlots();
        SubscribeToEvents();
        UpdateSlotUIBindings();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // 슬롯 초기화
    private void InitializeSlots()
    {
        if (autoFindSlots || allInventorySlots == null || allInventorySlots.Count == 0)
        {
            AutoFindSlots();
        }
    }

    // 이벤트 구독
    private void SubscribeToEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged += UpdateSlotUIBindings;
        }
    }

    // 이벤트 구독 해제
    private void UnsubscribeFromEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged -= UpdateSlotUIBindings;
        }
    }

    // 자동으로 슬롯 찾기
    private void AutoFindSlots()
    {
        Slot_UI[] foundSlots = GetComponentsInChildren<Slot_UI>(true);
        allInventorySlots = new List<Slot_UI>(foundSlots);
    }

    // 슬롯 UI 바인딩 업데이트
    private void UpdateSlotUIBindings()
    {
        if (!IsValidState()) return;

        List<InventorySlot> filteredSlots = InventoryManager.Instance.GetFilteredInventory();
        List<InventorySlot> originalSlots = InventoryManager.Instance.inventorySlots;

        for (int i = 0; i < allInventorySlots.Count; i++)
        {
            Slot_UI slotUI = allInventorySlots[i];
            if (slotUI == null) continue;

            InventorySlot slotData = GetSlotData(i, filteredSlots, originalSlots);
            slotUI.SetBoundItem(slotData);
            slotUI.gameObject.SetActive(true);
        }
    }

    // 유효한 상태인지 확인
    private bool IsValidState()
    {
        return InventoryManager.Instance != null &&
               allInventorySlots != null &&
               allInventorySlots.Count > 0;
    }

    // 슬롯 데이터 가져오기
    private InventorySlot GetSlotData(int index, List<InventorySlot> filteredSlots, List<InventorySlot> originalSlots)
    {
        if (index < filteredSlots.Count)
        {
            InventorySlot filteredSlot = filteredSlots[index];

            if (filteredSlot.slotIndex == -1)
            {
                return filteredSlot;
            }
            else
            {
                return originalSlots[filteredSlot.slotIndex];
            }
        }

        return new InventorySlot() { slotIndex = -1 };
    }
}