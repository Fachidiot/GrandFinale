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

    //  리스트 기반 UI 바인딩
    private void UpdateSlotUIBindings()
    {
        if (!IsValidState()) return;

        List<InventoryItem> displayItems = InventoryManager.Instance.GetFilteredItems();
        int maxSlots = allInventorySlots.Count;
        int displayCount = displayItems.Count;

        // 2. 슬롯을 순회하며 아이템 채우기
        for (int i = 0; i < maxSlots; i++)
        {
            Slot_UI slotUI = allInventorySlots[i];
            if (slotUI == null) continue;

            // [추가] 슬롯의 인덱스를 설정합니다.
            slotUI.SetListIndex(i);

            if (i < displayCount)
            {
                // 데이터가 있으면 바인딩하고 활성화
                slotUI.gameObject.SetActive(true);
                slotUI.BindItem(displayItems[i]);
            }
            else
            {
                // 데이터가 없으면 비우기
                slotUI.ClearSlot();

                // 만약 빈 슬롯을 아예 숨기고 싶다면 아래 주석 해제
                // slotUI.gameObject.SetActive(false); 
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