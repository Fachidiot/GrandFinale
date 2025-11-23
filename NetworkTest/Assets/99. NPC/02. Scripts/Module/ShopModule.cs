using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;

[DisallowMultipleComponent]
public class ShopModule : MonoBehaviour
{
    [Header("Database Reference")]
    [SerializeField] private MasterDatabase masterDatabase;

    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera shopCamera;
    [SerializeField] private int activePriority = 20;

    [Header("Shop Items (Current)")]
    [SerializeField] private List<RelicData> saleItems = new List<RelicData>();

    [Header("UI References")]
    [SerializeField] public GameObject shopPanel;
    [SerializeField] private UnityEngine.UI.Button buyConfirmButton;
    [SerializeField] private ShopSellArea sellArea;
    [SerializeField] private Transform slotContainer;

    [Header("Settings")]
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private float autoCloseDistance = 5f;
    [SerializeField] private float restockIntervalMinutes = 30f; // 30분마다 리셋

    // 내부 변수
    private Dictionary<RelicData, int> purchaseCart = new Dictionary<RelicData, int>();
    private List<ShopSlotUI> selectedSlots = new List<ShopSlotUI>();

    public bool IsOpen { get; private set; }
    private Transform _opener;
    private double _nextRestockTime;

    private PlayerStats _currentCustomer;

    private void Start()
    {
        if (shopPanel) shopPanel.SetActive(false);
        if (shopCamera) shopCamera.Priority = 0;

        if (buyConfirmButton)
        {
            buyConfirmButton.onClick.RemoveAllListeners();
            buyConfirmButton.onClick.AddListener(BuySelectedItems);
        }

        // 최초 상점 구성
        RestockShop();
    }

    private void Update()
    {
        // 30분 주기 체크
        if (Time.realtimeSinceStartup >= _nextRestockTime)
        {
            RestockShop();
        }

        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape)) CloseShop();
        if (_opener != null && Vector3.Distance(transform.position, _opener.position) > autoCloseDistance)
            CloseShop();
    }

    private void RestockShop()
    {
        if (masterDatabase == null || masterDatabase.allRelics.Count == 0) return;

        saleItems.Clear();
        purchaseCart.Clear();
        selectedSlots.Clear();

        // 1. 아이템 개수 결정 (8: 75%, 16: 20%, 24: 5%)
        float countRoll = Random.value;
        int itemCount = 8;
        if (countRoll > 0.95f) itemCount = 24;      // 상위 5%
        else if (countRoll > 0.75f) itemCount = 16; // 상위 25% (20% 구간)

        // 2. 아이템 뽑기 로직
        for (int i = 0; i < itemCount; i++)
        {
            RelicData pickedItem = GetRandomItemByRarity();
            if (pickedItem != null)
            {
                saleItems.Add(pickedItem);
            }
        }

        // 3. 다음 리셋 시간 설정
        _nextRestockTime = Time.realtimeSinceStartup + (restockIntervalMinutes * 60f);
        Debug.Log($"[Shop] 상점 리셋 완료! (아이템 {itemCount}개)");

        if (IsOpen) PopulateShop();
    }

    private RelicData GetRandomItemByRarity()
    {
        // 등급 확률: Normal 65%, Rare 25%, Epic 10%
        float rarityRoll = Random.value;
        string targetGrade = "Normal";

        if (rarityRoll > 0.90f) targetGrade = "Epic";
        else if (rarityRoll > 0.65f) targetGrade = "Rare";

        // DB에서 해당 등급 필터링
        // (주의: RelicData의 grade 문자열이 대소문자 정확해야 함, 여기선 DB 데이터 신뢰)
        var candidateItems = masterDatabase.allRelics.Where(x => x.grade.Equals(targetGrade, System.StringComparison.OrdinalIgnoreCase)).ToList();

        // 만약 해당 등급이 없으면 전체에서 랜덤
        if (candidateItems.Count == 0) candidateItems = masterDatabase.allRelics;

        if (candidateItems.Count > 0)
        {
            return candidateItems[Random.Range(0, candidateItems.Count)];
        }
        return null;
    }

    public void BuySelectedItems()
    {
        // _currentCustomer가 있어야 거래 가능
        if (purchaseCart.Count == 0 || InventoryManager.Instance == null || _currentCustomer == null) return;

        // 1. 총 가격 계산
        int totalPrice = 0;
        foreach (var pair in purchaseCart)
        {
            totalPrice += pair.Key.price * pair.Value;
        }

        // 2. 돈 확인
        if (!_currentCustomer.SpendCurrency(totalPrice))
        {
            Debug.Log($"[Shop] 돈이 부족합니다! (필요: {totalPrice}, 보유: {_currentCustomer.CurrentCurrency})");
            // 여기에 '돈 부족' 팝업 UI 추가 가능
            return;
        }

        // 3. 아이템 지급 및 상점 리스트에서 제거
        foreach (var pair in purchaseCart)
        {
            RelicData item = pair.Key;
            int count = pair.Value;

            for (int i = 0; i < count; i++)
            {
                InventoryManager.Instance.AddItem(item);
                saleItems.Remove(item);
            }
        }

        Debug.Log($"[Shop] 거래 완료. -{totalPrice}G");

        // 4. UI 재구축 (잔상 해결)
        DeselectAll();
        PopulateShop();
    }

    public void SellItem(RelicData item, int slotIndex)
    {
        if (InventoryManager.Instance != null && _currentCustomer != null)
        {
            // 판매 가격 (30%)
            int sellPrice = Mathf.FloorToInt(item.price * 0.3f);
            if (sellPrice < 1) sellPrice = 1;

            InventoryManager.Instance.RemoveItemFromSlot(slotIndex, 1);
            _currentCustomer.AddCurrency(sellPrice); // 돈 지급

            Debug.Log($"[Shop] {item.itemName} 판매 완료. (+{sellPrice}G)");
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
                slots[i].SetSelected(false);
            }
            else
            {
                slots[i].gameObject.SetActive(false);
                slots[i].Setup(null, null);
            }
        }
    }

    public void ToggleShop(Transform player) { if (IsOpen) CloseShop(); else OpenShop(player); }

    public void OpenShop(Transform player)
    {
        if (IsOpen) return;
        IsOpen = true;

        _currentCustomer = player.GetComponent<PlayerStats>();
        if (_currentCustomer == null) Debug.LogError("[Shop] 손님에게 PlayerStats가 없습니다!");

        _opener = player;
        DeselectAll();
        UIEvents.FireInteractState(null);

        if (shopCamera) shopCamera.Priority = activePriority;

        PopulateShop(); // UI 그리기

        if (shopPanel) shopPanel.SetActive(true);
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetExternalInteractionActive(true);
            InventoryManager.Instance.OpenSmallInventory();
        }
        if (lockCursor) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }

    public void CloseShop()
    {
        if (!IsOpen) return;

        _currentCustomer = null; // 참조 해제

        StationaryBrain brain = GetComponent<StationaryBrain>();
        if (brain != null) UIEvents.FireInteractState(brain.GetPrompt());

        IsOpen = false;
        _opener = null;
        DeselectAll();

        if (shopCamera) shopCamera.Priority = 0;
        if (shopPanel) shopPanel.SetActive(false);

        if (InventoryManager.Instance != null)
        {
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
}