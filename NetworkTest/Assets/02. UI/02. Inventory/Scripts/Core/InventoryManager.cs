using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public class InventoryItem
{
    public RelicData item;
    public int quantity;

    public InventoryItem(RelicData data, int qty)
    {
        this.item = data;
        this.quantity = qty;
    }

    public int slotIndex
    {
        get
        {
            if (InventoryManager.Instance == null) return -1;
            return InventoryManager.Instance.allItems.IndexOf(this);
        }
    }
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Category Capacity Settings")]
    [SerializeField] private int weaponCapacity = 15;
    [SerializeField] private int equipmentCapacity = 10;
    [SerializeField] private int accessoryCapacity = 10;
    [SerializeField] private int relicCapacity = 10;
    [SerializeField] private int etcCapacity = 15;

    public int TotalCapacity => weaponCapacity + equipmentCapacity + accessoryCapacity + relicCapacity + etcCapacity;

    [Header("Data")]
    public List<InventoryItem> allItems = new List<InventoryItem>();
    public List<InventoryItem> allTabDisplayList = new List<InventoryItem>();

    public InventoryFilterType currentFilter { get; private set; } = InventoryFilterType.All;

    public static event Action OnInventoryChanged;
    public static event Action<bool> OnInventoryToggle;

    [Header("UI Reference")]
    [SerializeField] private GameObject smallInventoryUI;
    [SerializeField] private GameObject fullInventoryUI;

    [Header("Components")]
    [SerializeField] private CharacterMove characterMove;
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private GameObject genericLootPrefab;

    private CanvasGroup fullCanvasGroup;
    private CanvasGroup smallCanvasGroup;
    private float fadeDuration = 0.2f;
    private Animator playerAnimator;

    public bool IsFocused { get; private set; } = false;
    private bool isExternalInteractionActive = false;
    public bool IsExternalInteractionActive => isExternalInteractionActive;

    public bool IsUIOpen => (smallInventoryUI != null && smallInventoryUI.activeSelf) ||
                            (fullInventoryUI != null && fullInventoryUI.activeSelf);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (characterMove != null) playerAnimator = characterMove.GetComponent<Animator>();

        InitCanvasGroup(fullInventoryUI, ref fullCanvasGroup);
        InitCanvasGroup(smallInventoryUI, ref smallCanvasGroup);

        InitializeInventorySlots();

        // [추가] 시작 시 유령 아이템(껍데기) 청소 실행
        CleanUpGhostItems();
    }

    private void InitializeInventorySlots()
    {
        // 리스트 크기가 부족하면 늘려줌 (기존 데이터 보존 노력)
        int total = TotalCapacity;

        while (allItems.Count < total) allItems.Add(null);
        while (allTabDisplayList.Count < total) allTabDisplayList.Add(null);

        Debug.Log($"[인벤토리 초기화] 듀얼 리스트 구성 완료 (총 {total}칸)");
    }

    // [신규] 유령 아이템 청소 함수
    private void CleanUpGhostItems()
    {
        int cleanCount = 0;
        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i] != null && allItems[i].item == null)
            {
                allItems[i] = null;
                cleanCount++;
            }
        }
        for (int i = 0; i < allTabDisplayList.Count; i++)
        {
            if (allTabDisplayList[i] != null && allTabDisplayList[i].item == null)
            {
                allTabDisplayList[i] = null;
            }
        }
        if (cleanCount > 0) Debug.Log($"[시스템] 빈 껍데기 아이템 {cleanCount}개 정리됨.");
    }

    private void InitCanvasGroup(GameObject obj, ref CanvasGroup cg)
    {
        if (obj == null) return;
        if (cg == null) cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        obj.SetActive(false);
    }

    void Update()
    {
        if (!isExternalInteractionActive && Input.GetKeyDown(KeyCode.Tab)) ToggleSmallInventory();
        if (Input.GetKeyDown(KeyCode.O)) ToggleFullInventory();
        if (IsFocused && Input.GetKeyDown(KeyCode.Escape)) CloseAllInventories();

        if (IsFocused && !isExternalInteractionActive && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject())
                CloseAllInventories();
        }
    }

    // ---------------------------------------------------------
    // 1. 아이템 획득 (유령 아이템 무시 로직 적용)
    // ---------------------------------------------------------
    public bool AddItem(RelicData newItem, int amount = 1)
    {
        if (newItem == null) return false;

        InventoryFilterType type = GetFilterFromItem(newItem);
        var (startIndex, count) = GetCategoryRange(type);

        // A. 중첩 확인
        if (newItem.maxStack > 1)
        {
            for (int i = startIndex; i < startIndex + count; i++)
            {
                if (i >= allItems.Count) break;
                // 내용물이 있는 진짜 아이템만 확인
                if (allItems[i] != null && allItems[i].item == newItem && allItems[i].quantity < newItem.maxStack)
                {
                    allItems[i].quantity += amount;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // B. 빈 슬롯 찾기
        int realIndex = -1;
        for (int i = startIndex; i < startIndex + count; i++)
        {
            if (i >= allItems.Count) break;
            // [핵심 수정] 슬롯이 null이거나, 껍데기(item==null)라면 빈칸으로 인정!
            if (allItems[i] == null || allItems[i].item == null)
            {
                realIndex = i;
                break;
            }
        }

        if (realIndex == -1)
        {
            Debug.LogWarning($"[Inventory Full] {type} 카테고리가 가득 찼습니다.");
            return false;
        }

        // 전체 탭 리스트에서도 빈자리 찾기 (유령 무시)
        int displayIndex = -1;
        for (int i = 0; i < allTabDisplayList.Count; i++)
        {
            if (allTabDisplayList[i] == null || allTabDisplayList[i].item == null)
            {
                displayIndex = i;
                break;
            }
        }

        if (displayIndex == -1) displayIndex = realIndex; // 안전장치

        InventoryItem newInvItem = new InventoryItem(newItem, amount);

        allItems[realIndex] = newInvItem;
        allTabDisplayList[displayIndex] = newInvItem;

        Debug.Log($"[획득] {newItem.itemName} 저장 완료 (Slot {realIndex})");
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ---------------------------------------------------------
    // 2. 아이템 이동
    // ---------------------------------------------------------
    public void MoveItemToEmptySlot(InventoryItem itemToMove, int targetLocalIndex)
    {
        if (itemToMove == null) return;

        // [CASE A] 전체(All) 탭 -> 전체 리스트(allTabDisplayList)만 변경
        if (currentFilter == InventoryFilterType.All)
        {
            if (targetLocalIndex < 0 || targetLocalIndex >= allTabDisplayList.Count) return;

            int currentIndex = allTabDisplayList.IndexOf(itemToMove);
            if (currentIndex == -1 || currentIndex == targetLocalIndex) return;

            // 껍데기 체크
            bool isTargetOccupied = (allTabDisplayList[targetLocalIndex] != null && allTabDisplayList[targetLocalIndex].item != null);

            if (isTargetOccupied)
            {
                // 스왑
                InventoryItem targetItem = allTabDisplayList[targetLocalIndex];
                allTabDisplayList[targetLocalIndex] = itemToMove;
                allTabDisplayList[currentIndex] = targetItem;
            }
            else
            {
                // 이동
                allTabDisplayList[targetLocalIndex] = itemToMove;
                allTabDisplayList[currentIndex] = null;
            }
        }
        // [CASE B] 개별 탭 -> 실제 저장소(allItems) 변경
        else
        {
            var (offset, count) = GetCategoryRange(currentFilter);
            int targetGlobalIndex = offset + targetLocalIndex;

            if (targetLocalIndex < 0 || targetLocalIndex >= count) return;

            int currentGlobalIndex = allItems.IndexOf(itemToMove);
            if (currentGlobalIndex == -1 || currentGlobalIndex == targetGlobalIndex) return;

            // 껍데기 체크
            bool isTargetOccupied = (allItems[targetGlobalIndex] != null && allItems[targetGlobalIndex].item != null);

            if (isTargetOccupied)
            {
                InventoryItem targetItem = allItems[targetGlobalIndex];
                allItems[targetGlobalIndex] = itemToMove;
                allItems[currentGlobalIndex] = targetItem;
            }
            else
            {
                allItems[targetGlobalIndex] = itemToMove;
                allItems[currentGlobalIndex] = null;
            }
        }

        OnInventoryChanged?.Invoke();
    }

    // ---------------------------------------------------------
    // 3. 보여주기
    // ---------------------------------------------------------
    public List<InventoryItem> GetFilteredItems()
    {
        if (currentFilter == InventoryFilterType.All)
        {
            return allTabDisplayList;
        }

        var (offset, count) = GetCategoryRange(currentFilter);
        if (offset + count > allItems.Count) return new List<InventoryItem>();
        return allItems.GetRange(offset, count);
    }

    // ---------------------------------------------------------
    // 4. 제거 (두 리스트 동기화)
    // ---------------------------------------------------------
    public void RemoveItem(InventoryItem itemToRemove, int amount = 1)
    {
        if (itemToRemove == null) return;

        itemToRemove.quantity -= amount;

        if (itemToRemove.quantity <= 0)
        {
            int realIndex = allItems.IndexOf(itemToRemove);
            if (realIndex != -1) allItems[realIndex] = null;

            int displayIndex = allTabDisplayList.IndexOf(itemToRemove);
            if (displayIndex != -1) allTabDisplayList[displayIndex] = null;
        }

        OnInventoryChanged?.Invoke();
    }

    // ... (헬퍼 함수들 유지) ...
    private (int start, int count) GetCategoryRange(InventoryFilterType filter)
    {
        int start = 0;
        if (filter == InventoryFilterType.Weapon) return (0, weaponCapacity);
        start += weaponCapacity;
        if (filter == InventoryFilterType.Equipment) return (start, equipmentCapacity);
        start += equipmentCapacity;
        if (filter == InventoryFilterType.Accessory) return (start, accessoryCapacity);
        start += accessoryCapacity;
        if (filter == InventoryFilterType.Relic) return (start, relicCapacity);
        start += relicCapacity;
        if (filter == InventoryFilterType.Etc) return (start, etcCapacity);
        return (0, 0);
    }

    private InventoryFilterType GetFilterFromItem(RelicData data)
    {
        if (data == null) return InventoryFilterType.All;
        switch (data.itemTypeEnum)
        {
            case ItemType.Weapon: return InventoryFilterType.Weapon;
            case ItemType.Equipment: return InventoryFilterType.Equipment;
            case ItemType.Accessory: return InventoryFilterType.Accessory;
            case ItemType.Artifact: return InventoryFilterType.Relic;
            default: return InventoryFilterType.Etc;
        }
    }

    public void RemoveItemFromSlot(int listIndex, int amount = 1)
    {
        List<InventoryItem> currentList = GetFilteredItems();
        if (listIndex < 0 || listIndex >= currentList.Count) return;
        if (currentList[listIndex] == null) return;
        RemoveItem(currentList[listIndex], amount);
    }

    public void RemoveItemByData(RelicData data, int amount = 1)
    {
        var target = allItems.Find(x => x != null && x.item == data);
        if (target != null) RemoveItem(target, amount);
    }

    public void SwapItems(InventoryItem itemA, InventoryItem itemB)
    {
        if (currentFilter == InventoryFilterType.All)
        {
            int indexA = allTabDisplayList.IndexOf(itemA);
            int indexB = allTabDisplayList.IndexOf(itemB);
            if (indexA != -1 && indexB != -1)
            {
                allTabDisplayList[indexA] = itemB;
                allTabDisplayList[indexB] = itemA;
                OnInventoryChanged?.Invoke();
            }
        }
        else
        {
            int indexA = allItems.IndexOf(itemA);
            int indexB = allItems.IndexOf(itemB);
            if (indexA != -1 && indexB != -1)
            {
                allItems[indexA] = itemB;
                allItems[indexB] = itemA;
                OnInventoryChanged?.Invoke();
            }
        }
    }

    public void DropItem(InventoryItem itemToDrop)
    {
        if (itemToDrop == null) return;
        if (genericLootPrefab != null)
        {
            Transform playerTr = GameObject.FindWithTag("Player")?.transform ?? Camera.main.transform;
            Vector3 dropPos = playerTr.position + playerTr.forward * 1.5f;
            GameObject orb = Instantiate(genericLootPrefab, dropPos, Quaternion.identity);

            if (orb.TryGetComponent(out ItemPickup pickup)) pickup.itemData = itemToDrop.item;
            if (orb.TryGetComponent(out LootOrbVisuals visuals)) visuals.Initialize(itemToDrop.item.grade);

            RemoveItem(itemToDrop, 1);
        }
    }

    public void SetFilter(InventoryFilterType newFilter)
    {
        currentFilter = newFilter;
        OnInventoryChanged?.Invoke();
    }

    public void ToggleFullInventory()
    {
        if (!fullInventoryUI) return;
        if (smallInventoryUI && smallInventoryUI.activeSelf) ToggleSmallInventory();
        bool isActive = fullInventoryUI.activeSelf;
        if (isActive)
        {
            PlaySFX("Close");
            if (fullCanvasGroup) { fullCanvasGroup.blocksRaycasts = false; fullCanvasGroup.interactable = false; }
            SetFocusState(false);
            fullCanvasGroup.DOFade(0f, fadeDuration).OnComplete(() => fullInventoryUI.SetActive(false));
        }
        else
        {
            PlaySFX("Open");
            fullInventoryUI.SetActive(true);
            if (fullCanvasGroup) { fullCanvasGroup.alpha = 0f; fullCanvasGroup.blocksRaycasts = true; fullCanvasGroup.interactable = true; fullCanvasGroup.DOFade(1f, fadeDuration); }
            SetFocusState(true);
        }
    }
    public void ToggleSmallInventory()
    {
        if (!smallInventoryUI) return;
        if (fullInventoryUI && fullInventoryUI.activeSelf) fullInventoryUI.SetActive(false);
        bool isActive = smallInventoryUI.activeSelf;
        if (isActive)
        {
            PlaySFX("Close");
            if (smallCanvasGroup) { smallCanvasGroup.blocksRaycasts = false; smallCanvasGroup.interactable = false; }
            SetFocusState(false);
            smallCanvasGroup.DOFade(0f, fadeDuration).OnComplete(() => smallInventoryUI.SetActive(false));
        }
        else
        {
            PlaySFX("Open");
            smallInventoryUI.SetActive(true);
            if (smallCanvasGroup) { smallCanvasGroup.alpha = 0f; smallCanvasGroup.blocksRaycasts = true; smallCanvasGroup.interactable = true; smallCanvasGroup.DOFade(1f, fadeDuration); }
            SetFocusState(true);
        }
    }
    public void CloseAllInventories() { if (fullInventoryUI && fullInventoryUI.activeSelf) ToggleFullInventory(); else if (smallInventoryUI && smallInventoryUI.activeSelf) ToggleSmallInventory(); }
    public void ToggleInventory() => ToggleFullInventory();
    public void SetExternalInteractionActive(bool isActive) { this.isExternalInteractionActive = isActive; if (!isActive) CloseAllInventories(); }
    public void NotifyInventoryChanged() => OnInventoryChanged?.Invoke();
    public void SetSearchQuery(string query) => OnInventoryChanged?.Invoke();
    public void OpenSmallInventory() { if (smallInventoryUI != null && !smallInventoryUI.activeSelf) ToggleSmallInventory(); }
    private void PlaySFX(string type) { if (AudioManager.Instance) { if (type == "Open") AudioManager.Instance.PlaySFX(AudioManager.Instance.uiOpenClip); else AudioManager.Instance.PlaySFX(AudioManager.Instance.uiCloseClip); } }
    private void SetFocusState(bool isFocused)
    {
        if (this.IsFocused == isFocused) return;
        this.IsFocused = isFocused;
        if (GameManager.Instance != null) GameManager.Instance.SetPause(isFocused);
        if (isFocused) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; StopPlayerAnimation(); }
        else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; ResumePlayerAnimation(); }
        OnInventoryToggle?.Invoke(isFocused);
    }
    private void StopPlayerAnimation() { if (playerAnimator) playerAnimator.speed = 0f; if (characterMove) characterMove.StopAllActions(); if (cameraSwitcher) cameraSwitcher.StopAiming(); }
    private void ResumePlayerAnimation() { if (playerAnimator) playerAnimator.speed = 1f; }
}