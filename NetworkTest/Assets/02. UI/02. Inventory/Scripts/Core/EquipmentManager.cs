using UnityEngine;
using System;
using System.Collections.Generic;

public class EquipmentManager : MonoBehaviour
{
    /*
    0 : Head
    1 : Face
    2 : Necklace
    3 : Armor
    4 : Pants
    5 : Shoes
    6 : Slot1 (Rifle)
    7 : Slot2 (Rifle)
    8 : Slot3 (SMG)
    9 : Slot4 (Pistol)
    10 : 유물
    11 : 유물
    12 : 유물
    */
    public static EquipmentManager Instance;
    private PlayerStats playerStats;

    #region Settings & Data

    [Header("Settings")]
    [SerializeField] private int equipmentSlotCapacity = 13;

    public List<RelicData> equipmentSlots;

    public static event Action OnEquipmentChanged;

    #endregion

    #region Initialization

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

    #endregion

    #region Equip Logic

    public bool EquipItem(RelicData itemToEquip, int targetEquipSlotIndex)
    {
        if (itemToEquip == null) return false;

        // 1. 인벤토리에서 제거
        InventoryManager.Instance.RemoveItemByData(itemToEquip, 1);

        RelicData oldItem = equipmentSlots[targetEquipSlotIndex];

        // 2. 교체 (기존 장비 있으면 인벤토리로)
        if (oldItem != null)
        {
            if (!TrySwapEquipment(itemToEquip, oldItem))
            {
                InventoryManager.Instance.AddItem(itemToEquip); // 실패 시 복구
                return false;
            }
        }

        // 3. 장착
        equipmentSlots[targetEquipSlotIndex] = itemToEquip;
        ApplyItemAbility(itemToEquip, true);

        // TODO : weaponcontroller의 slotmanager에 넣어줘야함.
        // 6 : Slot1 (Rifle)
        // 7 : Slot2 (Rifle)
        // 8 : Slot3 (SMG)
        // 9 : Slot4 (Pistol)
        if (itemToEquip.itemTypeEnum == ItemType.Weapon)
            OnSlotEquip.Invoke(targetEquipSlotIndex - 5, itemToEquip);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayEquipSound();

        OnEquipmentChanged?.Invoke();
        return true;
    }

    public event Action<int, RelicData> OnSlotEquip;
    public event Action<int> OnSlotUnequip;

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

    #endregion

    #region Unequip Logic

    public bool UnequipItem(RelicData itemData)
    {
        if (itemData == null) return false;

        // 인벤토리로 반환
        bool added = InventoryManager.Instance.AddItem(itemData);

        if (added)
        {
            for (int i = 0; i < equipmentSlots.Count; i++)
            {
                if (equipmentSlots[i] == itemData)
                {
                    equipmentSlots[i] = null;
                    ApplyItemAbility(itemData, false);
                    OnEquipmentChanged?.Invoke();
                    if (itemData.itemTypeEnum == ItemType.Weapon)
                        OnSlotUnequip.Invoke(i - 5);
                    break;
                }
            }
            return true;
        }
        return false;
    }

    #endregion

    #region Ability Application

    private void ApplyItemAbility(RelicData item, bool isEquipping)
    {
        if (item == null || item.grantedAbility == null) return;

        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats == null) return;

        string logicID = item.grantedAbility.abilityLogicID;
        string key = item.grantedAbility.param_Key;
        string valStr = item.grantedAbility.param_ValueA;

        // 스탯 적용
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
        // 특수 능력 적용
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

    #endregion
}