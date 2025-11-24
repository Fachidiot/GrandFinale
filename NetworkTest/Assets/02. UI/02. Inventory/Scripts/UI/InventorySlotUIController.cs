using UnityEngine;
using System.Collections.Generic;

public class InventorySlotUIController : MonoBehaviour
{
    [Header("인벤토리 슬롯 UI")]
    [SerializeField] private List<Slot_UI> allInventorySlots;

    [Header("자동 설정")]
    [SerializeField] private bool autoFindSlots = true;

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

    private void AutoFindSlots()
    {
        // 비활성화된 슬롯까지 포함하여 자식 컴포넌트 찾기
        Slot_UI[] foundSlots = GetComponentsInChildren<Slot_UI>(true);
        allInventorySlots = new List<Slot_UI>(foundSlots);
    }

    // [핵심 수정] 리스트 개수에 맞춰 슬롯 껐다 켜기
    private void UpdateSlotUIBindings()
    {
        if (!IsValidState()) return;

        // 1. 현재 탭에 해당하는 리스트 가져오기
        // (무기 탭이면 15개, 장비 탭이면 10개, 전체 탭이면 60개가 옴)
        List<InventoryItem> displayItems = InventoryManager.Instance.GetFilteredItems();

        int totalUISlots = allInventorySlots.Count; // 화면에 깔린 슬롯 전체 (60개)
        int targetCapacity = displayItems.Count;    // 현재 보여줘야 할 슬롯 개수 (15개 등)

        for (int i = 0; i < totalUISlots; i++)
        {
            Slot_UI slotUI = allInventorySlots[i];
            if (slotUI == null) continue;

            // [변경] 현재 탭의 용량(targetCapacity) 안쪽이면 켜고, 넘치면 끕니다.
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
                slotUI.ClearSlot(); // 혹시 모르니 데이터 비움
            }
        }
    }

    private bool IsValidState()
    {
        return InventoryManager.Instance != null &&
               allInventorySlots != null &&
               allInventorySlots.Count > 0;
    }
}