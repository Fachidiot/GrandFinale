using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using TMPro;
using DG.Tweening;

[DisallowMultipleComponent]
public class ShopModule : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private MasterDatabase masterDatabase;

    [Header("Camera")]
    [SerializeField] private CinemachineVirtualCamera shopCamera;
    [SerializeField] private int activePriority = 20;

    [Header("Shop Items")]
    [SerializeField] private List<RelicData> saleItems = new List<RelicData>();

    [Header("UI")]
    [SerializeField] public GameObject shopPanel;
    [SerializeField] private Button buyConfirmButton;
    [SerializeField] private ShopSellArea sellArea;
    [SerializeField] private Transform slotContainer;

    [Header("UI - Money & Message")]
    [SerializeField] private TextMeshProUGUI systemMessageText;

    [Header("Refresh")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private TextMeshProUGUI refreshCountText;
    [SerializeField] private int maxRefreshCount = 1;

    [Header("Settings")]
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private float autoCloseDistance = 5f;
    [SerializeField] private float restockIntervalMinutes = 30f;

    [Header("Message Animation")]
    [SerializeField] private float msgMoveDist = 50f; // 위로 올라갈 거리
    [SerializeField] private float msgDuration = 1.5f; // 떠있는 시간

    private Dictionary<RelicData, int> purchaseCart = new Dictionary<RelicData, int>();
    private List<ShopSlotUI> selectedSlots = new List<ShopSlotUI>();

    public bool IsOpen { get; private set; }

    private Transform _opener;

    private double _nextRestockTime;
    private int _currentRefreshCount;
    private Vector2 _originalMsgPos;

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

        if (refreshButton)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }

        if (systemMessageText != null)
        {
            _originalMsgPos = systemMessageText.rectTransform.anchoredPosition;
            systemMessageText.gameObject.SetActive(false);
        }

        RestockShop(true);
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup >= _nextRestockTime)
        {
            RestockShop(true);
        }

        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape)) CloseShop();
        if (_opener != null && Vector3.Distance(transform.position, _opener.position) > autoCloseDistance)
            CloseShop();
    }

    private void RestockShop(bool restoreRefreshCount)
    {
        if (masterDatabase == null || masterDatabase.allRelics.Count == 0) return;

        saleItems.Clear();
        purchaseCart.Clear();
        selectedSlots.Clear();

        float countRoll = Random.value;
        int itemCount = 8;
        if (countRoll > 0.95f) itemCount = 24;
        else if (countRoll > 0.75f) itemCount = 16;

        for (int i = 0; i < itemCount; i++)
        {
            RelicData pickedItem = GetRandomItemByRarity();
            if (pickedItem != null) saleItems.Add(pickedItem);
        }

        _nextRestockTime = Time.realtimeSinceStartup + (restockIntervalMinutes * 60f);

        if (restoreRefreshCount) _currentRefreshCount = maxRefreshCount;

        if (IsOpen)
        {
            PopulateShop();
            UpdateRefreshUI();
        }
    }

    // 버튼 클릭 시 UI 강제 갱신 추가
    private void OnRefreshButtonClicked()
    {
        if (_currentRefreshCount > 0)
        {
            _currentRefreshCount--;

            RestockShop(false);

            // RestockShop이 DB오류 등으로 중간에 멈추더라도
            // 횟수는 차감되었으니 UI는 무조건 갱신해줍니다.
            UpdateRefreshUI();
        }
    }

    private void UpdateRefreshUI()
    {
        if (refreshCountText != null)
        {
            // text: "0 / 1"
            refreshCountText.text = $"{_currentRefreshCount} / {maxRefreshCount}";
        }

        if (refreshButton != null)
        {
            // 0이면 버튼 비활성화 (클릭 불가)
            refreshButton.interactable = (_currentRefreshCount > 0);
        }
    }

    private RelicData GetRandomItemByRarity()
    {
        float rarityRoll = Random.value;
        string targetGrade = "Normal";

        if (rarityRoll > 0.90f) targetGrade = "Epic";
        else if (rarityRoll > 0.65f) targetGrade = "Rare";

        var candidateItems = masterDatabase.allRelics.Where(x => x.grade.Equals(targetGrade, System.StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidateItems.Count == 0) candidateItems = masterDatabase.allRelics;

        if (candidateItems.Count > 0)
            return candidateItems[Random.Range(0, candidateItems.Count)];

        return null;
    }

    public void BuySelectedItems()
    {
        if (purchaseCart.Count == 0 || InventoryManager.Instance == null || _currentCustomer == null) return;

        int totalPrice = 0;
        foreach (var pair in purchaseCart) totalPrice += pair.Key.price * pair.Value;

        if (!_currentCustomer.SpendCurrency(totalPrice))
        {
            ShowSystemMessage("소지금이 부족합니다!");
            return;
        }

        foreach (var pair in purchaseCart)
        {
            for (int i = 0; i < pair.Value; i++)
            {
                InventoryManager.Instance.AddItem(pair.Key);
                saleItems.Remove(pair.Key);
            }
        }

        DeselectAll();
        PopulateShop();
    }

    public void SellItem(RelicData item, int slotIndex)
    {
        if (InventoryManager.Instance != null && _currentCustomer != null)
        {
            int sellPrice = Mathf.FloorToInt(item.price * 0.3f);
            if (sellPrice < 1) sellPrice = 1;

            InventoryManager.Instance.RemoveItemFromSlot(slotIndex, 1);
            _currentCustomer.AddCurrency(sellPrice);
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
        if (_currentCustomer == null) return;

        _opener = player;
        DeselectAll();
        UIEvents.FireInteractState(null);

        if (shopCamera) shopCamera.Priority = activePriority;

        PopulateShop();
        UpdateRefreshUI(); // 열릴 때 UI 갱신

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

        _currentCustomer = null;

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

    private void ShowSystemMessage(string message)
    {
        if (systemMessageText == null) return;

        // 1. 기존에 실행 중이던 애니메이션이 있다면 즉시 종료 (중복 방지)
        systemMessageText.DOKill();
        systemMessageText.rectTransform.DOKill();

        // 2. 초기 상태 설정 (위치 복구, 불투명하게, 켜기)
        systemMessageText.text = message;
        systemMessageText.rectTransform.anchoredPosition = _originalMsgPos;
        systemMessageText.alpha = 1f; // 투명도 초기화
        systemMessageText.gameObject.SetActive(true);

        // 3. DOTween 애니메이션 실행
        // 위로 이동
        systemMessageText.rectTransform.DOAnchorPosY(_originalMsgPos.y + msgMoveDist, msgDuration)
            .SetEase(Ease.OutQuad); // 부드럽게 감속

        // 투명해지면서 사라짐
        systemMessageText.DOFade(0f, msgDuration)
            .SetEase(Ease.InQuad) // 가속하며 사라짐
            .OnComplete(() =>
            {
                // 애니메이션이 끝나면 비활성화
                systemMessageText.gameObject.SetActive(false);
            });
    }
}