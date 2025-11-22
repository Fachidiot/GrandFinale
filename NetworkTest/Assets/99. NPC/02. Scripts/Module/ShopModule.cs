using UnityEngine;
using System.Collections.Generic;
using Cinemachine;

[DisallowMultipleComponent]
public class ShopModule : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera shopCamera;
    [SerializeField] private int activePriority = 20;

    [Header("Shop Items")]
    [SerializeField] private List<RelicData> saleItems = new List<RelicData>();

    [Header("UI References")]
    [SerializeField] public GameObject shopPanel;
    [SerializeField] private UnityEngine.UI.Button buyConfirmButton;
    [SerializeField] private ShopSellArea sellArea;
    [SerializeField] private Transform slotContainer;

    [Header("Settings")]
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private float autoCloseDistance = 5f;

    // 장바구니
    private Dictionary<RelicData, int> purchaseCart = new Dictionary<RelicData, int>();
    private List<ShopSlotUI> selectedSlots = new List<ShopSlotUI>();

    public bool IsOpen { get; private set; }
    private Transform _opener;

    private void Start()
    {
        if (shopPanel) shopPanel.SetActive(false);
        if (shopCamera) shopCamera.Priority = 0;
        if (buyConfirmButton)
        {
            buyConfirmButton.onClick.RemoveAllListeners();
            buyConfirmButton.onClick.AddListener(BuySelectedItems);
        }
    }

    public void ToggleShop(Transform player)
    {
        if (IsOpen) CloseShop();
        else OpenShop(player);
    }

    public void OpenShop(Transform player)
    {
        if (IsOpen) return;

        IsOpen = true;
        _opener = player;
        DeselectAll();

        // 1. 상호작용 텍스트 끄기
        UIEvents.FireInteractState(null);

        // 2. 카메라 전환
        if (shopCamera) shopCamera.Priority = activePriority;

        // 3. UI 세팅
        PopulateShop();
        if (shopPanel) shopPanel.SetActive(true);

        // 4. 인벤토리 강제 열기 및 외부 상호작용 잠금
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetExternalInteractionActive(true);
            InventoryManager.Instance.OpenSmallInventory();
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void CloseShop()
    {
        if (!IsOpen) return;

        // 상호작용 텍스트 복구 (플레이어가 아직 근처에 있다면)
        StationaryBrain brain = GetComponent<StationaryBrain>();
        if (brain != null) UIEvents.FireInteractState(brain.GetPrompt());

        IsOpen = false;
        _opener = null;
        DeselectAll();

        if (shopCamera) shopCamera.Priority = 0;
        if (shopPanel) shopPanel.SetActive(false);

        if (InventoryManager.Instance != null)
        {
            // 외부 상호작용 잠금 해제 후 닫기
            InventoryManager.Instance.SetExternalInteractionActive(false);
            InventoryManager.Instance.CloseAllInventories();
        }
        else if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OnSlotClicked(RelicData item, ShopSlotUI slotUI)
    {
        if (selectedSlots.Contains(slotUI)) RemoveItemFromCart(item, slotUI);
        else AddItemToCart(item, slotUI);
    }

    private void AddItemToCart(RelicData item, ShopSlotUI slotUI)
    {
        if (purchaseCart.ContainsKey(item)) purchaseCart[item]++;
        else purchaseCart.Add(item, 1);

        if (!selectedSlots.Contains(slotUI)) selectedSlots.Add(slotUI);
        slotUI.SetSelected(true);
    }

    private void RemoveItemFromCart(RelicData item, ShopSlotUI slotUI)
    {
        if (purchaseCart.ContainsKey(item))
        {
            purchaseCart[item]--;
            if (purchaseCart[item] <= 0) purchaseCart.Remove(item);
        }
        if (selectedSlots.Contains(slotUI))
        {
            selectedSlots.Remove(slotUI);
            slotUI.SetSelected(false);
        }
    }

    private void DeselectAll()
    {
        foreach (var slot in selectedSlots) if (slot != null) slot.SetSelected(false);
        purchaseCart.Clear();
        selectedSlots.Clear();
    }

    public void BuySelectedItems()
    {
        if (purchaseCart.Count == 0 || InventoryManager.Instance == null) return;

        int successCount = 0;
        foreach (var pair in purchaseCart)
        {
            for (int i = 0; i < pair.Value; i++)
            {
                if (InventoryManager.Instance.AddItem(pair.Key)) successCount++;
                else break;
            }
        }

        if (successCount > 0)
        {
            Debug.Log($"[Shop] {successCount}개 아이템 구매 완료");
            DeselectAll();
        }
    }

    public void SellItem(RelicData item, int slotIndex)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItemFromSlot(slotIndex, 1);
            Debug.Log($"[Shop] {item.itemName} 판매 완료");
        }
    }

    private void PopulateShop()
    {
        if (!slotContainer) return;
        ShopSlotUI[] slots = slotContainer.GetComponentsInChildren<ShopSlotUI>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < saleItems.Count)
            {
                slots[i].gameObject.SetActive(true);
                slots[i].Setup(saleItems[i], this);
            }
            else slots[i].gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) CloseShop();
        if (_opener != null && Vector3.Distance(transform.position, _opener.position) > autoCloseDistance)
            CloseShop();
    }
}