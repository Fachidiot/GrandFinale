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

    #region Settings & Data

    [Header("Category Capacity Settings")]
    [SerializeField] private int weaponCapacity = 15;
    [SerializeField] private int equipmentCapacity = 10;
    [SerializeField] private int accessoryCapacity = 10;
    [SerializeField] private int relicCapacity = 10;
    [SerializeField] private int etcCapacity = 15;

    public int TotalCapacity => weaponCapacity + equipmentCapacity + accessoryCapacity + relicCapacity + etcCapacity;

    [Header("Data Storage")]
    public List<InventoryItem> allItems = new List<InventoryItem>();
    public List<InventoryItem> allTabDisplayList = new List<InventoryItem>();

    public InventoryFilterType currentFilter { get; private set; } = InventoryFilterType.All;

    public static event Action OnInventoryChanged;
    public static event Action<bool> OnInventoryToggle;

    #endregion

    #region UI References & Components

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

    #endregion

    #region Initialization

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (characterMove != null) playerAnimator = characterMove.GetComponent<Animator>();

        InitCanvasGroup(fullInventoryUI, ref fullCanvasGroup);
        InitCanvasGroup(smallInventoryUI, ref smallCanvasGroup);

        InitializeInventorySlots();
        CleanUpGhostItems();
    }

    private void InitializeInventorySlots()
    {
        int total = TotalCapacity;
        while (allItems.Count < total) allItems.Add(null);
        while (allTabDisplayList.Count < total) allTabDisplayList.Add(null);
        Debug.Log($"[Inventory Init] Dual List Created (Total: {total})");
    }

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
        if (cleanCount > 0) Debug.Log($"[System] Ghost Items Cleaned: {cleanCount}");
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

    #endregion

    #region Input & Update

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

    #endregion

    #region Item Management (Add / Remove / Drop)

    public bool AddItem(RelicData newItem, int amount = 1)
    {
        if (newItem == null) return false;

        InventoryFilterType type = GetFilterFromItem(newItem);
        var (startIndex, count) = GetCategoryRange(type);

        // 1. Stacking Check
        if (newItem.maxStack > 1)
        {
            for (int i = startIndex; i < startIndex + count; i++)
            {
                if (i >= allItems.Count) break;
                if (allItems[i] != null && allItems[i].item == newItem && allItems[i].quantity < newItem.maxStack)
                {
                    allItems[i].quantity += amount;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // 2. Find Empty Slot in Storage
        int realIndex = -1;
        for (int i = startIndex; i < startIndex + count; i++)
        {
            if (i >= allItems.Count) break;
            if (allItems[i] == null || allItems[i].item == null)
            {
                realIndex = i;
                break;
            }
        }

        if (realIndex == -1)
        {
            Debug.LogWarning($"[Inventory Full] {type} Capacity Reached.");
            return false;
        }

        // 3. Find Empty Slot in Display List
        int displayIndex = -1;
        for (int i = 0; i < allTabDisplayList.Count; i++)
        {
            if (allTabDisplayList[i] == null || allTabDisplayList[i].item == null)
            {
                displayIndex = i;
                break;
            }
        }

        if (displayIndex == -1) displayIndex = realIndex;

        // 4. Create & Assign
        InventoryItem newInvItem = new InventoryItem(newItem, amount);
        allItems[realIndex] = newInvItem;
        allTabDisplayList[displayIndex] = newInvItem;

        Debug.Log($"[Item Added] {newItem.itemName} (Slot {realIndex})");
        OnInventoryChanged?.Invoke();
        return true;
    }

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

    public void DropItem(InventoryItem itemToDrop)
    {
        if (itemToDrop == null || genericLootPrefab == null) return;

        Transform playerTr = GameObject.FindWithTag("Player")?.transform ?? Camera.main.transform;
        Vector3 dropPos = playerTr.position + playerTr.forward * 1.5f;
        GameObject orb = Instantiate(genericLootPrefab, dropPos, Quaternion.identity);

        if (orb.TryGetComponent(out ItemPickup pickup)) pickup.itemData = itemToDrop.item;
        if (orb.TryGetComponent(out LootOrbVisuals visuals)) visuals.Initialize(itemToDrop.item.grade);

        RemoveItem(itemToDrop, 1);
    }

    #endregion

    #region Item Movement (Move / Swap)

    public void MoveItemToEmptySlot(InventoryItem itemToMove, int targetLocalIndex)
    {
        if (itemToMove == null) return;

        // Case A: All Tab (Modify Display List Only)
        if (currentFilter == InventoryFilterType.All)
        {
            if (targetLocalIndex < 0 || targetLocalIndex >= allTabDisplayList.Count) return;

            int currentIndex = allTabDisplayList.IndexOf(itemToMove);
            if (currentIndex == -1 || currentIndex == targetLocalIndex) return;

            bool isTargetOccupied = (allTabDisplayList[targetLocalIndex] != null && allTabDisplayList[targetLocalIndex].item != null);

            if (isTargetOccupied)
            {
                InventoryItem targetItem = allTabDisplayList[targetLocalIndex];
                allTabDisplayList[targetLocalIndex] = itemToMove;
                allTabDisplayList[currentIndex] = targetItem;
            }
            else
            {
                allTabDisplayList[targetLocalIndex] = itemToMove;
                allTabDisplayList[currentIndex] = null;
            }
        }
        // Case B: Category Tab (Modify Storage List)
        else
        {
            var (offset, count) = GetCategoryRange(currentFilter);
            int targetGlobalIndex = offset + targetLocalIndex;

            if (targetLocalIndex < 0 || targetLocalIndex >= count) return;

            int currentGlobalIndex = allItems.IndexOf(itemToMove);
            if (currentGlobalIndex == -1 || currentGlobalIndex == targetGlobalIndex) return;

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

    #endregion

    #region Data Retrieval & Helpers

    public List<InventoryItem> GetFilteredItems()
    {
        if (currentFilter == InventoryFilterType.All) return allTabDisplayList;

        var (offset, count) = GetCategoryRange(currentFilter);
        if (offset + count > allItems.Count) return new List<InventoryItem>();
        return allItems.GetRange(offset, count);
    }

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

    public void SetFilter(InventoryFilterType newFilter)
    {
        currentFilter = newFilter;
        OnInventoryChanged?.Invoke();
    }

    public void NotifyInventoryChanged() => OnInventoryChanged?.Invoke();
    public void SetSearchQuery(string query) => OnInventoryChanged?.Invoke();

    #endregion

    #region UI Toggle Logic

    public void ToggleFullInventory()
    {
        if (!fullInventoryUI) return;
        if (smallInventoryUI.activeSelf) ToggleSmallInventory();

        bool isOpen = !fullInventoryUI.activeSelf;
        SetUIState(fullInventoryUI, fullCanvasGroup, isOpen);
    }

    public void ToggleSmallInventory()
    {
        if (!smallInventoryUI) return;
        if (fullInventoryUI.activeSelf) ToggleFullInventory();

        bool isOpen = !smallInventoryUI.activeSelf;
        SetUIState(smallInventoryUI, smallCanvasGroup, isOpen);
    }

    private void SetUIState(GameObject ui, CanvasGroup cg, bool isOpen)
    {
        if (isOpen)
        {
            PlaySFX("Open");
            ui.SetActive(true);
            if (cg != null) { cg.alpha = 0f; cg.blocksRaycasts = true; cg.interactable = true; cg.DOFade(1f, fadeDuration); }
            SetFocusState(true);
        }
        else
        {
            PlaySFX("Close");
            if (cg != null)
            {
                cg.blocksRaycasts = false; cg.interactable = false;
                cg.DOFade(0f, fadeDuration).OnComplete(() => ui.SetActive(false));
            }
            SetFocusState(false);
        }
    }

    public void CloseAllInventories()
    {
        if (fullInventoryUI && fullInventoryUI.activeSelf) ToggleFullInventory();
        else if (smallInventoryUI && smallInventoryUI.activeSelf) ToggleSmallInventory();
    }

    public void ToggleInventory() => ToggleFullInventory();

    public void SetExternalInteractionActive(bool isActive)
    {
        this.isExternalInteractionActive = isActive;
        if (!isActive) CloseAllInventories();
    }

    public void OpenSmallInventory()
    {
        if (smallInventoryUI != null && !smallInventoryUI.activeSelf) ToggleSmallInventory();
    }

    #endregion

    #region Audio & Animation

    private void PlaySFX(string type)
    {
        if (AudioManager.Instance)
        {
            if (type == "Open") AudioManager.Instance.PlaySFX(AudioManager.Instance.uiOpenClip);
            else AudioManager.Instance.PlaySFX(AudioManager.Instance.uiCloseClip);
        }
    }

    private void SetFocusState(bool isFocused)
    {
        if (this.IsFocused == isFocused) return;
        this.IsFocused = isFocused;
        if (GameManager.Instance != null) GameManager.Instance.SetPause(isFocused);

        if (isFocused) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; StopPlayerAnimation(); }
        else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; ResumePlayerAnimation(); }

        OnInventoryToggle?.Invoke(isFocused);
    }

    private void StopPlayerAnimation()
    {
        if (playerAnimator) playerAnimator.speed = 0f;
        if (characterMove) characterMove.StopAllActions();
        if (cameraSwitcher) cameraSwitcher.StopAiming();
    }

    private void ResumePlayerAnimation()
    {
        if (playerAnimator) playerAnimator.speed = 1f;
    }

    #endregion
}