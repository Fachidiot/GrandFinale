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

    // [중요] 이제 전체 크기는 이 변수들의 '합계'로 자동 결정됩니다.
    // maxSlotCapacity 변수는 삭제했습니다. (충돌 방지)
    [Header("Category Capacity Settings")]
    [SerializeField] private int weaponCapacity = 15;
    [SerializeField] private int equipmentCapacity = 10;
    [SerializeField] private int accessoryCapacity = 10;
    [SerializeField] private int relicCapacity = 10;
    [SerializeField] private int etcCapacity = 15;

    // 전체 용량 자동 계산 프로퍼티
    public int TotalCapacity => weaponCapacity + equipmentCapacity + accessoryCapacity + relicCapacity + etcCapacity;

    [Header("Data")]
    public List<InventoryItem> allItems = new List<InventoryItem>();
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
        CleanUpGhostItems();
    }

    // [수정] maxSlotCapacity 대신 TotalCapacity(합계) 사용
    private void InitializeInventorySlots()
    {
        allItems.Clear();
        int total = TotalCapacity; // 자동 합계 사용
        for (int i = 0; i < total; i++)
        {
            allItems.Add(null);
        }
        Debug.Log($"[인벤토리 초기화] 총 {total}칸 생성됨 (무기:{weaponCapacity}, 장비:{equipmentCapacity}...)");
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

    // 탭별 시작 위치와 크기를 반환하는 핵심 함수
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

    public void MoveItemToEmptySlot(InventoryItem itemToMove, int targetLocalIndex)
    {
        if (currentFilter == InventoryFilterType.All) return;
        if (itemToMove == null) return;

        var (offset, count) = GetCategoryRange(currentFilter);
        int targetGlobalIndex = offset + targetLocalIndex;

        if (targetLocalIndex < 0 || targetLocalIndex >= count) return;

        int currentGlobalIndex = allItems.IndexOf(itemToMove);
        if (currentGlobalIndex == -1 || currentGlobalIndex == targetGlobalIndex) return;

        // [핵심 수정] 타겟이 null이 아니더라도, 내용물(item)이 비어있으면 빈칸 취급해야 함
        bool isTargetOccupied = (allItems[targetGlobalIndex] != null && allItems[targetGlobalIndex].item != null);

        if (isTargetOccupied)
        {
            // 타겟에 진짜 아이템이 있으면 스왑
            InventoryItem targetItem = allItems[targetGlobalIndex];
            allItems[targetGlobalIndex] = itemToMove;
            allItems[currentGlobalIndex] = targetItem;
        }
        else
        {
            // 타겟이 비어있거나 껍데기만 있으면 -> 덮어쓰고 이동
            allItems[targetGlobalIndex] = itemToMove;
            allItems[currentGlobalIndex] = null; // 원래 자리는 null로
        }

        OnInventoryChanged?.Invoke();
    }

    public bool AddItem(RelicData newItem, int amount = 1)
    {
        // 디버그용 상태 확인 (Ghost Item 확인용으로 조건 수정됨)
        int currentCount = allItems != null ? allItems.Count : 0;
        // [수정] 껍데기만 있는 경우도 빈칸으로 카운트
        int emptyCount = allItems != null ? allItems.Count(x => x == null || x.item == null) : 0;

        Debug.Log($"[상태 확인] 전체 칸 수: {currentCount}, 사용 가능 슬롯: {emptyCount}, 합계 용량: {TotalCapacity}");

        if (newItem == null) return false;

        InventoryFilterType type = GetFilterFromItem(newItem);
        var (startIndex, count) = GetCategoryRange(type);

        Debug.Log($"[아이템 획득 시도] 아이템: {newItem.itemName} / 타입: {type} / 탐색 범위: {startIndex} ~ {startIndex + count - 1}");

        if (count <= 0)
        {
            Debug.LogError($"[오류] {type} 카테고리 용량이 0입니다.");
            return false;
        }

        // 1. 중첩 확인 (Quantity 증가)
        if (newItem.maxStack > 1)
        {
            for (int i = startIndex; i < startIndex + count; i++)
            {
                if (i >= allItems.Count) break;

                // [중요] x != null 체크 추가
                var slot = allItems[i];
                if (slot != null && slot.item == newItem && slot.quantity < newItem.maxStack)
                {
                    slot.quantity += amount;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // 2. 빈 슬롯 찾기 (새 아이템 추가)
        for (int i = startIndex; i < startIndex + count; i++)
        {
            if (i >= allItems.Count) break;

            // [핵심 수정] 슬롯이 null이거나, 슬롯은 있는데 내용물(item)이 null이면 '빈칸'으로 인정!
            if (allItems[i] == null || allItems[i].item == null)
            {
                // 새 아이템으로 덮어쓰기
                allItems[i] = new InventoryItem(newItem, amount);
                Debug.Log($"[획득 성공] {i}번 슬롯에 저장됨.");
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        Debug.LogWarning($"[Inventory Full] {type} 카테고리가 가득 찼습니다.");
        return false;
    }

    public void RemoveItem(InventoryItem itemToRemove, int amount = 1)
    {
        if (itemToRemove == null) return;

        int index = allItems.IndexOf(itemToRemove);
        if (index == -1) return;

        itemToRemove.quantity -= amount;
        if (itemToRemove.quantity <= 0)
        {
            allItems[index] = null;
        }
        OnInventoryChanged?.Invoke();
    }

    public void RemoveItemFromSlot(int listIndex, int amount = 1)
    {
        if (listIndex < 0 || listIndex >= allItems.Count) return;
        if (allItems[listIndex] == null) return;
        RemoveItem(allItems[listIndex], amount);
    }

    public void RemoveItemByData(RelicData data, int amount = 1)
    {
        var target = allItems.Find(x => x != null && x.item == data);
        if (target != null) RemoveItem(target, amount);
    }

    public void SwapItems(InventoryItem itemA, InventoryItem itemB)
    {
        if (itemA == itemB) return;
        int indexA = allItems.IndexOf(itemA);
        int indexB = allItems.IndexOf(itemB);

        if (indexA != -1 && indexB != -1)
        {
            allItems[indexA] = itemB;
            allItems[indexB] = itemA;
            OnInventoryChanged?.Invoke();
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
    private void CleanUpGhostItems()
    {
        for (int i = 0; i < allItems.Count; i++)
        {
            // 객체는 있는데 내용물이 없으면 -> 가차없이 null로 초기화
            if (allItems[i] != null && allItems[i].item == null)
            {
                allItems[i] = null;
            }
        }
        Debug.Log("[시스템] 인벤토리 유령 아이템 청소 완료");
    }

    public List<InventoryItem> GetFilteredItems()
    {
        // 1. [전체(All) 탭] - 뷰어 모드
        if (currentFilter == InventoryFilterType.All)
        {
            // 수정됨: x != null 뿐만 아니라 x.item != null 인 것만 가져옴
            return allItems.Where(x => x != null && x.item != null).ToList();
        }

        // 2. [개별 탭] - 관리 모드
        var (offset, count) = GetCategoryRange(currentFilter);
        if (offset + count > allItems.Count) return new List<InventoryItem>();
        return allItems.GetRange(offset, count);
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
            if (fullCanvasGroup)
            {
                fullCanvasGroup.alpha = 0f;
                fullCanvasGroup.blocksRaycasts = true;
                fullCanvasGroup.interactable = true;
                fullCanvasGroup.DOFade(1f, fadeDuration);
            }
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
            if (smallCanvasGroup)
            {
                smallCanvasGroup.alpha = 0f;
                smallCanvasGroup.blocksRaycasts = true;
                smallCanvasGroup.interactable = true;
                smallCanvasGroup.DOFade(1f, fadeDuration);
            }
            SetFocusState(true);
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

    public void NotifyInventoryChanged() => OnInventoryChanged?.Invoke();
    public void SetSearchQuery(string query) => OnInventoryChanged?.Invoke();
    public void OpenSmallInventory() { if (smallInventoryUI != null && !smallInventoryUI.activeSelf) ToggleSmallInventory(); }

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

    private void StopPlayerAnimation() { if (playerAnimator) playerAnimator.speed = 0f; if (characterMove) characterMove.StopAllActions(); if (cameraSwitcher) cameraSwitcher.StopAiming(); }
    private void ResumePlayerAnimation() { if (playerAnimator) playerAnimator.speed = 1f; }
}