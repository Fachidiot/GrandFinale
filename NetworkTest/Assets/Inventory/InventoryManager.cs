using UnityEngine;
using System.Collections.Generic;
using System;
using Cinemachine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    private int slotCapacity = 8;
    public List<RelicData> inventorySlots;

    public static event Action OnInventoryChanged;
    public static event Action<bool> OnInventoryToggle;

    [Header("UI Reference")]
    // [SerializeField] private GameObject inventoryUI; // L18: ���� UI �ʵ� ����
    [SerializeField] private GameObject smallInventoryUI; // L19: ���� �κ��丮 UI (I Ű)
    [SerializeField] private GameObject fullInventoryUI;  // L20: ��ü �κ��丮 UI (O Ű)

    [Header("Loot Settings")]
    [SerializeField] private GameObject genericLootPrefab;

    [Header("Player Control References")]
    [SerializeField] private PlayerInputs playerInput;    // PlayerInputs.cs
    [SerializeField] private CharacterMove characterMove;  // CharacterMove.cs (�̵� ����) - NOTE: ���� ������ InputHandler�� �̵���
    [SerializeField] private CameraController cameraController; // ī�޶� ȸ�� ���� - NOTE: ���� ������ InputHandler�� �̵���
    [SerializeField] private WeaponController weaponController; // ���� �߻� ���� - NOTE: ���� ������ InputHandler�� �̵���

    // ���� �κ��丮/UI�� �����ִ��� Ȯ��
    public bool IsUIOpen => (smallInventoryUI != null && smallInventoryUI.activeSelf) ||
                            (fullInventoryUI != null && fullInventoryUI.activeSelf);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 2. RelicData ����Ʈ�� �ʱ�ȭ
        inventorySlots = new List<RelicData>();
        for (int i = 0; i < slotCapacity; i++)
        {
            inventorySlots.Add(null);
        }
    }

    void Update()
    {
        if (playerInput == null)
            return;

        // L61: I Ű �Է� ���� (���� �κ��丮)
        if (playerInput.GetInventory())
        {
            ToggleSmallInventory();
        }

        // L66: O Ű �Է� ���� (��ü �κ��丮 - InputHandler�� GetFullCharacterToggle()�� �ִٰ� ����)
        if (playerInput.GetFullInventory())
        {
            ToggleFullInventory();
        }

        // L71: ESC Ű �Է� ���� (��� �κ��丮 �ݱ�)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllInventories();
        }
    }

    // L77: ���� ToggleInventory�� CloseAllInventories�� ��ü�ϰ�, ToggleSmall/FullInventory�� ����մϴ�.

    /// <summary>
    /// I Ű �Է� ó��: ���� �κ��丮 â(���Ը�)�� ����մϴ�.
    /// </summary>
    public void ToggleSmallInventory()
    {
        if (smallInventoryUI == null) return;

        // 1. ��ü â�� ���� ������ ����
        if (fullInventoryUI != null && fullInventoryUI.activeSelf)
        {
            fullInventoryUI.SetActive(false);
        }

        // 2. ���� â ���
        bool shouldBeActive = !smallInventoryUI.activeSelf;
        smallInventoryUI.SetActive(shouldBeActive);

        // 3. Ŀ�� �� �Է� ���� ����
        SetPlayerInputState(shouldBeActive);
    }

    /// <summary>
    /// O Ű �Է� ó��: ��ü �κ��丮 â(���� + ���/����)�� ����մϴ�.
    /// </summary>
    public void ToggleFullInventory()
    {
        if (fullInventoryUI == null) return;

        // 1. ���� â�� ���� ������ ����
        if (smallInventoryUI != null && smallInventoryUI.activeSelf)
        {
            smallInventoryUI.SetActive(false);
        }

        // 2. ��ü â ���
        bool shouldBeActive = !fullInventoryUI.activeSelf;
        fullInventoryUI.SetActive(shouldBeActive);

        // 3. Ŀ�� �� �Է� ���� ����
        SetPlayerInputState(shouldBeActive);
    }

    /// <summary>
    /// ESC Ű �Է� ó��: ��� �κ��丮 â�� �ݰ� Ŀ���� ��޴ϴ�.
    /// </summary>
    public void CloseAllInventories()
    {
        if (!IsUIOpen) return;

        if (smallInventoryUI != null) smallInventoryUI.SetActive(false);
        if (fullInventoryUI != null) fullInventoryUI.SetActive(false);

        // Ŀ�� �� �Է� ���� ���� (��Ȱ��ȭ ����)
        SetPlayerInputState(false);
    }


    /// <summary>
    /// UI Ȱ��ȭ ���ο� ���� Ŀ�� ���¿� �Է� �̺�Ʈ�� �����մϴ�.
    /// </summary>
    private void SetPlayerInputState(bool uiIsActive)
    {
        if (uiIsActive)
        {
            Cursor.lockState = CursorLockMode.None; // Ŀ�� ��� ����
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // Ŀ�� ��� (���� �Է� ���·� ����)
            Cursor.visible = false;
        }

        // L159: InputHandler���� �Է� ������ ó���ϵ��� �̺�Ʈ ȣ��
        OnInventoryToggle?.Invoke(uiIsActive);

        // NOTE: ���� ToggleInventory�� ��ũ��Ʈ Ȱ��ȭ/��Ȱ��ȭ ������ 
        // InputHandler�� OnInventoryToggle�� �����ϴ� �ٸ� ��ũ��Ʈ�� �̵��ϴ� ���� �����մϴ�.
        // InputHandler�� ���콺 �Է��� �����ϸ�, �̵� ��ũ��Ʈ(CharacterMove, CameraController ��)�� 
        // InventoryManager�� ���� ��ȭ�� ���� ���� Ȱ��ȭ/��Ȱ��ȭ �� ���� �ֽ��ϴ�. 
        // ���⼭�� InputHandler�� �����ϴ� ���� �ϰ����� ���Դϴ�.
    }


    /// <summary>
    /// RelicData �������� �κ��丮�� �߰��մϴ�.
    /// </summary>
    public bool AddItem(RelicData itemToAdd) // 3. ItemData -> RelicData
    {
        int emptySlotIndex = FindNextEmptySlot();

        if (emptySlotIndex == -1)
        {
            Debug.Log("�κ��丮�� �� á���ϴ�.");
            return false;
        }

        inventorySlots[emptySlotIndex] = itemToAdd;
        OnInventoryChanged?.Invoke();
        Debug.Log(itemToAdd.itemName + "��(��) " + (emptySlotIndex + 1) + "�� ���Կ� �߰��߽��ϴ�.");
        return true;
    }

    public int FindNextEmptySlot()
    {
        for (int i = 0; i < slotCapacity; i++)
        {
            if (inventorySlots[i] == null)
            {
                return i;
            }
        }
        return -1;
    }

    // 4. RelicData�� ��ü�ϵ��� ����
    public void SwapItems(int indexA, int indexB)
    {
        RelicData temp = inventorySlots[indexA];
        inventorySlots[indexA] = inventorySlots[indexB];
        inventorySlots[indexB] = temp;
        OnInventoryChanged?.Invoke();
    }

    // L215: RelicData�� �����ϵ��� ���� + bool ��ȯ �߰� (CS0029 ���� ����)
    public bool RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotCapacity)
        {
            Debug.LogError($"[InventoryManager] �߸��� ���� �ε���: {slotIndex}");
            return false;
        }

        inventorySlots[slotIndex] = null;
        OnInventoryChanged?.Invoke();
        return true; // ���� ����
    }


    public void DropItem(int slotIndex)
    {
        RelicData itemToDrop = inventorySlots[slotIndex];
        if (itemToDrop == null) return;

        // 1. (�߿�!) RelicData�� dropPrefab�� �����Ǿ� �ִ��� Ȯ��
        if (genericLootPrefab != null)
        {
            // 2. �÷��̾� ��ġ ã�� (�ӽ÷� "Player" �±� ���)
            GameObject player = GameObject.FindWithTag("Player");
            Vector3 dropPosition;

            if (player != null)
            {
                // �÷��̾� 1���� �տ� ����
                dropPosition = player.transform.position + (player.transform.forward * 1f);
            }
            else
            {
                // �÷��̾ �� ã���� ī�޶� 1���� �տ� ���� (���� ��ġ)
                dropPosition = Camera.main.transform.position + (Camera.main.transform.forward * 1f);
            }

            GameObject orbInstance = Instantiate(genericLootPrefab, dropPosition, Quaternion.identity);

            ItemPickup pickupScript = orbInstance.GetComponent<ItemPickup>();
            if (pickupScript != null)
            {
                pickupScript.itemData = itemToDrop; // [�߿�] �� ��ü�� � ���������� ����
            }
            LootOrbVisuals visualScript = orbInstance.GetComponent<LootOrbVisuals>();
            if (visualScript != null)
            {
                visualScript.Initialize(itemToDrop.grade);
            }

            // 4. �κ��丮���� ������ ����
            RemoveItem(slotIndex); // (�� �Լ��� OnInventoryChanged�� ȣ����)
            Debug.Log(itemToDrop.itemName + "��(��) �ٴڿ� ���Ƚ��ϴ�.");
        }
        else
        {
            Debug.LogWarning("InventoryManager�� genericLootPrefab�� �������� �ʾ� �������� ���� �� �����ϴ�.");
        }
    }

    /// <summary>
    /// �κ��丮 ���� �̺�Ʈ�� �ܺο� �˸��ϴ�. (�̺�Ʈ ���� ȣ�� ������)
    /// </summary>
    public void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}