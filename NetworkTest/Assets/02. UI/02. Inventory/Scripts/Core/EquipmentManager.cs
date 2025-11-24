using UnityEngine;
using System;
using System.Collections.Generic;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;
    private PlayerStats playerStats;

    [Header("Settings")]
    [SerializeField] private int equipmentSlotCapacity = 13;

    public List<RelicData> equipmentSlots;

    public static event Action OnEquipmentChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializeEquipmentSlots();
    }

    private void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
    }

    private void InitializeEquipmentSlots()
    {
        equipmentSlots = new List<RelicData>();
        for (int i = 0; i < equipmentSlotCapacity; i++)
        {
            equipmentSlots.Add(null);
        }
    }

    // ==================================================================================
    // 1. 장비 장착 (인벤토리 인덱스 제거됨 -> 오류 해결)
    // ==================================================================================

    public bool EquipItem(RelicData itemToEquip, int targetEquipSlotIndex)
    {
        if (itemToEquip == null) return false;

        // 1. 인벤토리에서 해당 아이템 1개 제거 (데이터 기준 검색)
        InventoryManager.Instance.RemoveItemByData(itemToEquip, 1);

        RelicData oldItem = equipmentSlots[targetEquipSlotIndex];

        // 2. 교체 로직
        if (oldItem != null)
        {
            if (!TrySwapEquipment(itemToEquip, oldItem))
            {
                // 교체 실패 시(인벤 꽉참 등), 방금 뺀 아이템 복구
                InventoryManager.Instance.AddItem(itemToEquip);
                return false;
            }
        }

        // 3. 장비 슬롯 등록
        equipmentSlots[targetEquipSlotIndex] = itemToEquip;
        ApplyItemAbility(itemToEquip, true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEquipSound();

        OnEquipmentChanged?.Invoke();
        return true;
    }

    // [오류 해결] 매개변수 1개(RelicData)만 받도록 명확히 정의
    public bool EquipItemToFirstAvailableSlot(RelicData itemToEquip)
    {
        if (itemToEquip == null) return false;

        EquipmentSlot_UI[] allEquipSlots = FindObjectsOfType<EquipmentSlot_UI>(true);

        int targetEmptySlotIndex = -1;
        int targetFilledSlotIndex = -1;

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

        if (targetEmptySlotIndex != -1) return EquipItem(itemToEquip, targetEmptySlotIndex);
        if (targetFilledSlotIndex != -1) return EquipItem(itemToEquip, targetFilledSlotIndex);

        return false;
    }

    private bool TrySwapEquipment(RelicData newItem, RelicData oldItem)
    {
        bool addBackSuccess = InventoryManager.Instance.AddItem(oldItem);
        if (!addBackSuccess) return false;

        ApplyItemAbility(oldItem, false);
        return true;
    }

    // ==================================================================================
    // 2. 장비 해제 (인벤토리 인덱스 제거됨 -> 오류 해결)
    // ==================================================================================

    // [오류 해결] 매개변수 1개(RelicData)만 받도록 수정
    public bool UnequipItem(RelicData itemData)
    {
        if (itemData == null) return false;

        // 1. 인벤토리로 아이템을 되돌려줌 (자동으로 빈 곳에 들어감)
        bool added = InventoryManager.Instance.AddItem(itemData);

        if (added)
        {
            // 2. 장비 슬롯 리스트에서 해당 아이템 제거
            for (int i = 0; i < equipmentSlots.Count; i++)
            {
                if (equipmentSlots[i] == itemData)
                {
                    equipmentSlots[i] = null;
                    ApplyItemAbility(itemData, false); // 능력치 제거
                    OnEquipmentChanged?.Invoke();
                    break;
                }
            }
            return true;
        }
        return false;
    }

    // ==================================================================================
    // 3. 능력치 적용
    // ==================================================================================

    private void ApplyItemAbility(RelicData item, bool isEquipping)
    {
        if (item == null || item.grantedAbility == null) return;

        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats == null) return;

        string logicID = item.grantedAbility.abilityLogicID;
        string key = item.grantedAbility.param_Key;
        string valStr = item.grantedAbility.param_ValueA;

        if (logicID == "Stat_Add" || logicID == "Stat_Percent")
        {
            if (float.TryParse(valStr, out float value))
            {
                float finalValue = isEquipping ? value : -value;

                switch (key)
                {
                    case "MoveSpeed":
                    case "Speed":
                        playerStats.AddStatPercent("MoveSpeed", finalValue); break;
                    case "AllDamage":
                        playerStats.AddStatPercent("AllDamage", finalValue); break;
                    default:
                        playerStats.AddStat(key, finalValue); break;
                }
            }
        }
        else
        {
            PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
            if (playerAbilities != null)
            {
                if (isEquipping) playerAbilities.AddRelic(item.itemID);
                else playerAbilities.RemoveRelic(item.itemID);
            }
        }
    }
}