using UnityEngine;
using System.Collections.Generic;

public class InventorySlotUIController : MonoBehaviour
{
    #region Serialized Fields & Settings

    [Header("UI Components")]
    [SerializeField] private List<Slot_UI> allInventorySlots;

    [Header("Settings")]
    [SerializeField] private bool autoFindSlots = true;

    #endregion

    #region Initialization

    void Start()
    {
        InitializeSlots();
        SubscribeToEvents();

        // 매니저 초기화 후 UI 업데이트를 위해 약간의 지연 실행
        Invoke(nameof(UpdateSlotUIBindings), 0.05f);
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeSlots()
    {
        if (autoFindSlots || allInventorySlots == null || allInventorySlots.Count == 0)
        {
            AutoFindSlots();
        }
    }

    private void AutoFindSlots()
    {
        // 비활성화된 슬롯까지 포함하여 자식 컴포넌트 찾기
        Slot_UI[] foundSlots = GetComponentsInChildren<Slot_UI>(true);
        allInventorySlots = new List<Slot_UI>(foundSlots);
    }

    #endregion

    #region Event Handling

    private void SubscribeToEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged += UpdateSlotUIBindings;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged -= UpdateSlotUIBindings;
        }
    }

    #endregion

    #region UI Updates

    private void UpdateSlotUIBindings()
    {
        if (!IsValidState()) return;

        // 현재 탭에 해당하는 리스트 가져오기 (전체:60개, 무기:15개 등)
        List<InventoryItem> displayItems = InventoryManager.Instance.GetFilteredItems();

        int totalUISlots = allInventorySlots.Count; // 화면에 배치된 슬롯 총개수
        int targetCapacity = displayItems.Count;    // 현재 보여줘야 할 슬롯 개수

        for (int i = 0; i < totalUISlots; i++)
        {
            Slot_UI slotUI = allInventorySlots[i];
            if (slotUI == null) continue;

            // 용량 범위 내의 슬롯만 활성화
            if (i < targetCapacity)
            {
                slotUI.gameObject.SetActive(true);
                slotUI.SetListIndex(i);
                slotUI.BindItem(displayItems[i]);
            }
            else
            {
                // 용량을 초과하는 나머지 슬롯은 숨김 처리
                slotUI.gameObject.SetActive(false);
                slotUI.ClearSlot();
            }
        }
    }

    private bool IsValidState()
    {
        return InventoryManager.Instance != null &&
               allInventorySlots != null &&
               allInventorySlots.Count > 0;
    }

    #endregion
}