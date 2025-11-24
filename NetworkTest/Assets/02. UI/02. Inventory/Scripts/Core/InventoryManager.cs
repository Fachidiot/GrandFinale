using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public InventoryFilterType currentFilter { get; private set; } = InventoryFilterType.All;

    private int slotCapacity = 60;

    private string currentSearchQuery = string.Empty;

    private bool isExternalInteractionActive = false;

    //private PlayerInputs playerInputs;
    public List<InventorySlot> inventorySlots;


    public static event Action OnInventoryChanged;
    public static event Action<bool> OnInventoryToggle;
    public bool IsFocused { get; private set; } = false;

    [Header("UI Reference")]
    [SerializeField] private GameObject smallInventoryUI;
    [SerializeField] private GameObject fullInventoryUI;

    [Header("Loot Settings")]
    [SerializeField] private GameObject genericLootPrefab;

    [Header("Player Control References")]
    [SerializeField] private CharacterMove characterMove;
    [SerializeField] private CameraSwitcher cameraSwitcher;

    [Header("DOTween References")]
    [SerializeField] private CanvasGroup fullCanvasGroup;
    [SerializeField] private CanvasGroup smallCanvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Animation References")]
    [SerializeField] private InventoryAnimation smallInventoryAnim;
    [SerializeField] private InventoryAnimation fullInventoryAnim;

    private Animator playerAnimator;

    public bool IsUIOpen => (smallInventoryUI != null && smallInventoryUI.activeSelf) ||
                            (fullInventoryUI != null && fullInventoryUI.activeSelf);
    public bool IsExternalInteractionActive => isExternalInteractionActive;

    void Awake()
    {
        //PlayerInputs = GetComponent<PlayerInputs>();
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        inventorySlots = new List<InventorySlot>();
        for (int i = 0; i < slotCapacity; i++)
        {
            inventorySlots.Add(new InventorySlot() { slotIndex = i });
        }

        // 플레이어 애니메이터 찾기
        if (characterMove != null)
        {
            playerAnimator = characterMove.GetComponent<Animator>();
        }

        if (fullInventoryUI != null)
        {
            fullCanvasGroup = fullInventoryUI.GetComponent<CanvasGroup>();
            if (fullCanvasGroup == null) fullCanvasGroup = fullInventoryUI.AddComponent<CanvasGroup>();

            fullCanvasGroup.alpha = 0f;
            fullCanvasGroup.blocksRaycasts = false;
            fullInventoryUI.SetActive(false);
        }

        if (smallInventoryUI != null)
        {
            smallCanvasGroup = smallInventoryUI.GetComponent<CanvasGroup>();
            if (smallCanvasGroup == null) smallCanvasGroup = smallInventoryUI.AddComponent<CanvasGroup>();

            smallCanvasGroup.alpha = 0f;
            smallCanvasGroup.blocksRaycasts = false;
            smallInventoryUI.SetActive(false);
        }
    }

    void Update()
    {
        // if (playerInput != null && playerInput.GetInventory())
        // {
        //     Debug.Log("인벤토리 키 입력 감지 (by PlayerInputs)!");
        //     // ToggleSmallInventory(); // (이전에 'O'키에 연결된 ToggleInventory() 대신)
        // }

        // if (playerInput != null && playerInput.GetFullInventoryToggle())
        // {
        //     Debug.Log("Full 인벤토리 키 입력 감지 (by PlayerInputs)!");
        //     ToggleInventory(); // (기존 'O'키에 연결된 함수)
        // }


        if (!isExternalInteractionActive && Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleSmallInventory();
        }

        // O 키: Full 인벤토리 토글
        if (Input.GetKeyDown(KeyCode.O))
        {
            ToggleFullInventory();
        }

        // ESC 키: 인벤토리가 열려있으면 닫기
        if (IsFocused && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllInventories();
        }

        if (IsFocused && !isExternalInteractionActive && Input.GetMouseButtonDown(0))
        {
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (!isOverUI)
            {
                // 빈 공간을 클릭했으므로 포커스 해제 및 모든 인벤토리 닫기
                SetFocusState(false);
                CloseAllInventories();
            }
        }
    }

    #region NPC 전용

    public void ToggleInventory()
    {
        ToggleFullInventory();
    }

    public void OpenSmallInventory()
    {
        // 이미 열려있다면 포커스만 맞추고 종료
        if (smallInventoryUI != null && smallInventoryUI.activeSelf)
        {
            SetFocusState(true);
            return;
        }

        // Full 인벤토리가 켜져 있다면 끄기
        if (fullInventoryUI != null && fullInventoryUI.activeSelf)
        {
            fullInventoryUI.SetActive(false);
        }

        // Small 인벤토리 열기
        if (smallInventoryUI != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.uiOpenClip);

            smallInventoryUI.SetActive(true);

            if (smallInventoryAnim != null)
            {
                smallInventoryAnim.OpenInventory();
            }
            else
            {
                if (smallCanvasGroup != null)
                {
                    smallCanvasGroup.alpha = 0f;
                    smallCanvasGroup.blocksRaycasts = true;
                    smallCanvasGroup.DOFade(1f, fadeDuration);
                }
            }

            SetFocusState(true);
        }
    }

    public void CloseSmallInventory()
    {
        if (smallInventoryUI == null || !smallInventoryUI.activeSelf) return;
        if (smallInventoryAnim != null)
        {
            smallInventoryAnim.CloseInventory(() =>
            {
                smallInventoryUI.SetActive(false);
                if (fullInventoryUI == null || !fullInventoryUI.activeSelf)
                {
                    SetFocusState(false);
                }
            });
        }
        else
        {
            smallInventoryUI.SetActive(false);

            if (fullInventoryUI == null || !fullInventoryUI.activeSelf)
            {
                SetFocusState(false);
            }
        }
    }
    #endregion

    public void SetExternalInteractionActive(bool isActive)
    {
        this.isExternalInteractionActive = isActive;
    }

    public void ToggleSmallInventory()
    {
        if (smallInventoryUI == null) return;

        if (fullInventoryUI != null && fullInventoryUI.activeSelf)
        {
            fullInventoryUI.SetActive(false);
            if (IsFocused) SetFocusState(false);
        }

        bool currentActive = smallInventoryUI.activeSelf;

        if (currentActive && IsFocused) 
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.uiCloseClip);

            smallCanvasGroup.blocksRaycasts = false;
            smallCanvasGroup.DOFade(0f, fadeDuration)
                .OnComplete(() =>
                {
                    smallInventoryUI.SetActive(false);
                    SetFocusState(false);
                });
        }
        else if (!currentActive)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.uiOpenClip);

            smallInventoryUI.SetActive(true);
            smallCanvasGroup.alpha = 0f;
            smallCanvasGroup.blocksRaycasts = true;
            smallCanvasGroup.DOFade(1f, fadeDuration);
            SetFocusState(true);
        }
        else
        {
            SetFocusState(true);
        }
    }

    public void ToggleFullInventory()
    {
        if (fullInventoryUI == null) return;

        if (smallInventoryUI != null && smallInventoryUI.activeSelf)
        {
            smallInventoryUI.SetActive(false);
            if (IsFocused) SetFocusState(false);
        }

        bool currentActive = fullInventoryUI.activeSelf;

        if (currentActive && IsFocused) // 닫기
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.uiCloseClip);

            InventoryAnimation_Close();
            fullInventoryUI.SetActive(false);
            SetFocusState(false);
        }
        else if (!currentActive) // 열기
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.uiOpenClip);

            InventoryAnimation_Open();
            fullInventoryUI.SetActive(true);
            SetFocusState(true);
        }
        else
        {
            SetFocusState(true);
        }
    }

    public void CloseAllInventories()
    {
        if (!IsUIOpen) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.uiCloseClip);

        CloseSmallInventory();
        if (fullInventoryUI != null && fullInventoryUI.activeSelf)
        {
            InventoryAnimation_Close();
        }
        SetFocusState(false);
    }

    private void SetFocusState(bool isFocused)
    {
        if (this.IsFocused == isFocused) return;

        this.IsFocused = isFocused;

        // Use the centralized pause system
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPause(isFocused);
        }

        // 커서 제어
        if (isFocused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 애니메이션 제어
        if (isFocused)
        {
            StopPlayerAnimation();
        }
        else
        {
            ResumePlayerAnimation();
        }

        OnInventoryToggle?.Invoke(isFocused);
    }

    private void StopPlayerAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.speed = 0f;
        }

        if (characterMove != null)
        {
            characterMove.StopAllActions();
        }

        // 조준 중이면 강제로 해제
        if (cameraSwitcher != null)
        {
            cameraSwitcher.StopAiming();
        }
    }

    private void ResumePlayerAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.speed = 1f;
        }
    }

    public bool AddItem(RelicData itemToAdd)
    {
        // 1. 스택 가능한 아이템인지 확인 (maxStack > 1)
        if (itemToAdd.maxStack > 1)
        {
            // 2. 인벤토리를 순회하며 같은 아이템이 있고, 스택이 가득 차지 않았는지 확인
            for (int i = 0; i < slotCapacity; i++)
            {
                InventorySlot slot = inventorySlots[i];
                if (slot.item != null &&
                    slot.item.itemID == itemToAdd.itemID &&
                    slot.quantity < slot.item.maxStack)
                {
                    // 3. (스택 성공) 수량을 1 증가시키고 알림
                    slot.AddQuantity(1);
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            // 4. (실패) 같은 아이템을 찾지 못했거나 모두 가득 찼음. -> 다음 단계(빈 슬롯 찾기)로 이동
        }

        // 5. (스택 불가 또는 스택 실패) 빈 슬롯을 찾습니다.
        int emptySlotIndex = FindNextEmptySlot();

        if (emptySlotIndex == -1)
        {
            Debug.Log("인벤토리가 꽉 찼습니다.");
            return false;
        }

        // 6. 빈 슬롯에 아이템 정보와 수량 1을 설정합니다.
        inventorySlots[emptySlotIndex].item = itemToAdd;
        inventorySlots[emptySlotIndex].quantity = 1;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int FindNextEmptySlot()
    {
        for (int i = 0; i < slotCapacity; i++)
        {
            if (inventorySlots[i].item == null)
            {
                return i;
            }
        }
        return -1;
    }

    public void RemoveItemFromSlot(int slotIndex, int amountToRemove = 1)
    {
        if (slotIndex < 0 || slotIndex >= slotCapacity || inventorySlots[slotIndex].item == null)
        {
            return;
        }

        InventorySlot slot = inventorySlots[slotIndex];
        slot.quantity -= amountToRemove;

        if (slot.quantity <= 0)
        {
            slot.ClearSlot();
        }

        OnInventoryChanged?.Invoke();
    }

    public void SwapItems(int indexA, int indexB)
    {
        InventorySlot temp = inventorySlots[indexA];
        inventorySlots[indexA] = inventorySlots[indexB];
        inventorySlots[indexB] = temp;

        inventorySlots[indexA].slotIndex = indexA;
        inventorySlots[indexB].slotIndex = indexB;

        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotCapacity)
        {
            Debug.LogError($"[InventoryManager] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }
        inventorySlots[slotIndex].ClearSlot();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void DropItem(int slotIndex)
    {
        InventorySlot slotToDrop = inventorySlots[slotIndex];
        if (slotToDrop.item == null) return;

        RelicData itemToDrop = slotToDrop.item;

        if (genericLootPrefab != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            Vector3 dropPosition;

            if (player != null)
            {
                dropPosition = player.transform.position + (player.transform.forward * 1f);
            }
            else
            {
                dropPosition = Camera.main.transform.position + (Camera.main.transform.forward * 1f);
            }

            GameObject orbInstance = Instantiate(genericLootPrefab, dropPosition, Quaternion.identity);

            ItemPickup pickupScript = orbInstance.GetComponent<ItemPickup>();
            if (pickupScript != null)
            {
                pickupScript.itemData = itemToDrop;
            }
            LootOrbVisuals visualScript = orbInstance.GetComponent<LootOrbVisuals>();
            if (visualScript != null)
            {
                visualScript.Initialize(itemToDrop.grade);
            }

            RemoveItemFromSlot(slotIndex, 1);

            Debug.Log($"{itemToDrop.itemName}을(를) 바닥에 1개 버렸습니다. (남은 수량: {slotToDrop.quantity})");
        }
        else
        {
            Debug.LogWarning("InventoryManager에 genericLootPrefab이 설정되지 않아 아이템을 버릴 수 없습니다.");
        }
    }

    public void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public void SetFilter(InventoryFilterType newFilter)
    {
        if (currentFilter != newFilter)
        {
            currentFilter = newFilter;
            Debug.Log($"[InventoryManager] 필터 변경: {currentFilter}");
            OnInventoryChanged?.Invoke();
        }
    }

    public bool IsCombatInputBlockedByUI()
    {
        return IsFocused;
    }

    public void SortInventory()
    {
        // 1. 아이템이 있는 슬롯과 비어있는 슬롯을 분리
        List<InventorySlot> filledSlots = inventorySlots
            .Where(slot => slot.item != null)
            .ToList();

        List<InventorySlot> emptySlots = inventorySlots
            .Where(slot => slot.item == null)
            .ToList();

        // 2. 아이템이 있는 슬롯을 정렬
        filledSlots = filledSlots
            .OrderBy(slot => slot.item.itemName)
            .ThenByDescending(slot => slot.quantity)
            .ToList();

        // 3. 정렬된 슬롯과 빈 슬롯을 다시 합치기
        inventorySlots.Clear();
        inventorySlots.AddRange(filledSlots);
        inventorySlots.AddRange(emptySlots);

        // 4. 슬롯 인덱스 재설정
        for (int i = 0; i < slotCapacity; i++)
        {
            inventorySlots[i].slotIndex = i;
        }

        // 5. UI 업데이트 이벤트 호출
        OnInventoryChanged?.Invoke();
        Debug.Log("[InventoryManager] 인벤토리 정렬 완료.");
    }

    public void SetSearchQuery(string query)
    {
        // 소문자로 변환하여 저장 (대소문자 구분 없이 검색하기 위함)
        string newQuery = query.Trim().ToLower();

        if (currentSearchQuery != newQuery)
        {
            currentSearchQuery = newQuery;
            Debug.Log($"[InventoryManager] 검색어 변경: {currentSearchQuery}");
            // 검색어가 변경되면 UI를 업데이트해야 함
            OnInventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// Full Inventory UI를 열 때의 애니메이션 (페이드 인)
    /// </summary>
    private void InventoryAnimation_Open()
    {
        if (fullCanvasGroup == null || fullInventoryUI == null) return;

        // 1. 초기 설정
        fullInventoryUI.SetActive(true);
        fullCanvasGroup.alpha = 0f;
        fullCanvasGroup.blocksRaycasts = true;

        // 2. DOTween 애니메이션 실행 (페이드 인)
        fullCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutSine);

        // (테스트용 다른 애니메이션 버전: 스케일 확대)
        /*
        fullInventoryUI.transform.localScale = Vector3.one * 0.8f;
        fullInventoryUI.transform.DOScale(1f, fadeDuration).SetEase(Ease.OutBack);
        */
    }

    /// <summary>
    /// Full Inventory UI를 닫을 때의 애니메이션 (페이드 아웃)
    /// </summary>
    private void InventoryAnimation_Close()
    {
        if (fullCanvasGroup == null || fullInventoryUI == null) return;

        // 1. DOTween 애니메이션 실행 (페이드 아웃)
        fullCanvasGroup.blocksRaycasts = false;

        fullCanvasGroup.DOFade(0f, fadeDuration)
            .SetEase(Ease.InSine)
            .OnComplete(() =>
            {
                // 2. 애니메이션 완료 후 비활성화 및 포커스 해제
                fullInventoryUI.SetActive(false);
                SetFocusState(false);
            });
    }

    public List<InventorySlot> GetFilteredInventory()
    {
        int capacity = slotCapacity;

        if (currentFilter == InventoryFilterType.All)
        {
            return new List<InventorySlot>(inventorySlots);
        }

        List<InventorySlot> filteredList = new List<InventorySlot>();

        foreach (InventorySlot slot in inventorySlots)
        {
            RelicData item = slot.item;
            if (item == null) continue;

            ItemType itemType = item.itemTypeEnum;

            bool match = currentFilter switch
            {
                InventoryFilterType.Weapon => itemType == ItemType.Weapon,
                InventoryFilterType.Equipment => itemType == ItemType.Equipment,
                InventoryFilterType.Accessory => itemType == ItemType.Accessory,
                InventoryFilterType.Relic => itemType == ItemType.Artifact,
                InventoryFilterType.Etc => itemType != ItemType.Weapon &&
                                           itemType != ItemType.Equipment &&
                                           itemType != ItemType.Accessory &&
                                           itemType != ItemType.Artifact,
                _ => false,
            };

            if (match)
            {
                filteredList.Add(slot);
            }
        }

        while (filteredList.Count < capacity)
        {
            filteredList.Add(new InventorySlot() { slotIndex = -1 });
        }

        return filteredList;
    }
}