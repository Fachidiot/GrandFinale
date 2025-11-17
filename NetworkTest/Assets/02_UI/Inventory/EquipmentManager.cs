using UnityEngine;
using System;
using System.Collections.Generic;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    private int equipmentSlotCapacity = 13;
    public List<RelicData> equipmentSlots;

    public static event Action OnEquipmentChanged;

    void Awake()
    {
        InitializeSingleton();
        InitializeEquipmentSlots();
    }

    // 싱글톤 초기화
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 장비 슬롯 초기화
    private void InitializeEquipmentSlots()
    {
        equipmentSlots = new List<RelicData>();
        for (int i = 0; i < equipmentSlotCapacity; i++)
        {
            equipmentSlots.Add(null);
        }
    }

    // 장비 착용
    public bool EquipItem(RelicData itemToEquip, int inventorySlotIndex, int targetEquipSlotIndex)
    {
        InventoryManager.Instance.RemoveItemFromSlot(inventorySlotIndex, 1);

        RelicData oldItem = equipmentSlots[targetEquipSlotIndex];

        // 기존 장비가 있으면 교체
        if (oldItem != null)
        {
            if (!TrySwapEquipment(itemToEquip, oldItem))
            {
                RestoreItemToInventory(itemToEquip);
                return false;
            }
        }

        // 새 장비 착용
        equipmentSlots[targetEquipSlotIndex] = itemToEquip;
        ApplyItemAbility(itemToEquip, true);

        OnEquipmentChanged?.Invoke();
        return true;
    }

    // 장비 교체 시도
    private bool TrySwapEquipment(RelicData newItem, RelicData oldItem)
    {
        bool addBackSuccess = InventoryManager.Instance.AddItem(oldItem);

        if (!addBackSuccess)
        {
            return false;
        }

        ApplyItemAbility(oldItem, false);
        return true;
    }

    // 인벤토리에 아이템 복구
    private void RestoreItemToInventory(RelicData item)
    {
        InventoryManager.Instance.AddItem(item);
        InventoryManager.Instance.NotifyInventoryChanged();
    }

    // 사용 가능한 첫 슬롯에 장비 착용
    public bool EquipItemToFirstAvailableSlot(RelicData itemToEquip, int inventorySlotIndex)
    {
        EquipmentSlot_UI[] allEquipSlots = FindObjectsOfType<EquipmentSlot_UI>(true);

        int targetEmptySlotIndex = -1;
        int targetFilledSlotIndex = -1;

        // 빈 슬롯 또는 채워진 슬롯 찾기
        foreach (EquipmentSlot_UI slotUI in allEquipSlots)
        {
            if (slotUI.CanEquipItem(itemToEquip))
            {
                if (slotUI.currentItem == null)
                {
                    targetEmptySlotIndex = slotUI.equipmentSlotIndex;
                    break;
                }
                else if (targetFilledSlotIndex == -1)
                {
                    targetFilledSlotIndex = slotUI.equipmentSlotIndex;
                }
            }
        }

        // 빈 슬롯 우선, 없으면 채워진 슬롯에 교체
        if (targetEmptySlotIndex != -1)
        {
            return EquipItem(itemToEquip, inventorySlotIndex, targetEmptySlotIndex);
        }

        if (targetFilledSlotIndex != -1)
        {
            return EquipItem(itemToEquip, inventorySlotIndex, targetFilledSlotIndex);
        }

        return false;
    }

    // 장비 해제
    public bool UnequipItem(RelicData itemToUnequip, int equipSlotIndex)
    {
        bool success = InventoryManager.Instance.AddItem(itemToUnequip);

        if (!success)
        {
            return false;
        }

        equipmentSlots[equipSlotIndex] = null;
        ApplyItemAbility(itemToUnequip, false);

        OnEquipmentChanged?.Invoke();
        return true;
    }

    // 장비 슬롯 교체
    public bool SwapEquipmentSlots(int slotIndexA, int slotIndexB)
    {
        if (!IsValidSlotIndex(slotIndexA) || !IsValidSlotIndex(slotIndexB))
        {
            return false;
        }

        RelicData temp = equipmentSlots[slotIndexA];
        equipmentSlots[slotIndexA] = equipmentSlots[slotIndexB];
        equipmentSlots[slotIndexB] = temp;

        OnEquipmentChanged?.Invoke();
        return true;
    }

    // 슬롯 인덱스 유효성 검사
    private bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < equipmentSlots.Count;
    }

    // 아이템 능력 적용/제거
    private void ApplyItemAbility(RelicData item, bool isEquipping)
    {
        if (item.grantedAbility == null) return;

        PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
        if (playerAbilities == null) return;

        if (isEquipping)
        {
            playerAbilities.AddRelic(item.itemID);
        }
        else
        {
            playerAbilities.RemoveRelic(item.itemID);
        }
    }
}