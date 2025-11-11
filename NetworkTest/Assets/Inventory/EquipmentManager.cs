using UnityEngine;
using System;
using System.Collections.Generic; // List 사용

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    // 1. RelicData를 담을 4개의 장비 슬롯
    private int equipmentSlotCapacity = 5;
    public List<RelicData> equipmentSlots;

    // 장비가 변경될 때 UI에 보낼 신호
    public static event Action OnEquipmentChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 2. RelicData 리스트로 4칸 초기화
        equipmentSlots = new List<RelicData>();
        for (int i = 0; i < equipmentSlotCapacity; i++)
        {
            equipmentSlots.Add(null);
        }
    }

    public bool EquipItem(RelicData itemToEquip, int inventorySlotIndex)
    {
        // 1. 인벤토리에서 아이템 제거 (EquipmentManager가 Equip을 시작할 때 인벤토리에서 제거합니다.)
        bool removedFromInventory = InventoryManager.Instance.RemoveItem(inventorySlotIndex);

        if (!removedFromInventory)
        {
            Debug.LogError($"[EquipItem] 인벤토리 슬롯 {inventorySlotIndex}에서 아이템을 제거하는 데 실패했습니다.");
            return false;
        }

        // 2. 장착 가능한 빈 슬롯을 찾거나 0번 인덱스부터 교체
        int equipIndex = FindNextEmptyEquipSlot();
        RelicData oldItem = null;

        if (equipIndex == -1) // 빈 슬롯이 없으면 (꽉 찼다면)
        {
            // 사용자의 요청: 0번 인덱스부터 교체
            equipIndex = 0; // 0번 슬롯으로 지정
            oldItem = equipmentSlots[equipIndex]; // 기존 아이템을 저장

            // 2-1. 기존 아이템(oldItem)을 인벤토리로 되돌림
            bool addBackSuccess = InventoryManager.Instance.AddItem(oldItem);

            if (!addBackSuccess)
            {
                InventoryManager.Instance.inventorySlots[inventorySlotIndex] = itemToEquip;
                InventoryManager.Instance.NotifyInventoryChanged();

                Debug.LogError("[EquipItem] 0번 교체 시도 중, 기존 장비가 인벤토리에 들어갈 공간이 없어 장착 실패! 아이템이 복구되었습니다.");
                return false;
            }

            // 2-2. 기존 아이템의 능력치 해제
            if (oldItem.grantedAbility != null)
            {
                PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
                if (playerAbilities != null)
                {
                    playerAbilities.RemoveRelic(oldItem.itemID);
                }
            }
        }

        // 3. 새 아이템 장착 (빈 슬롯이거나 0번 슬롯)
        equipmentSlots[equipIndex] = itemToEquip;

        // 4. 새 아이템의 능력치 적용
        if (itemToEquip.grantedAbility != null)
        {
            PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
            if (playerAbilities != null)
            {
                playerAbilities.AddRelic(itemToEquip.itemID);
            }
        }

        // 5. 장비 변경 이벤트 알림
        OnEquipmentChanged?.Invoke();

        return true;
    }

    // 빈 장비 슬롯을 0번부터 찾는 함수
    private int FindNextEmptyEquipSlot()
    {
        for (int i = 0; i < equipmentSlotCapacity; i++)
        {
            if (equipmentSlots[i] == null)
            {
                return i;
            }
        }
        return -1; // 꽉 찼음
    }

    public bool UnequipItem(RelicData itemToUnequip, int equipSlotIndex)
    {
        // 1. InventoryManager의 AddItem 함수를 호출합니다.
        // (이 함수가 알아서 빈 슬롯을 찾고, 꽉 찼는지 확인하며, OnInventoryChanged 이벤트도 호출합니다)
        bool success = InventoryManager.Instance.AddItem(itemToUnequip);

        // 2. AddItem이 실패했다면 (인벤토리 꽉 참)
        if (!success)
        {
            Debug.Log("인벤토리가 꽉 차서 장비를 해제할 수 없습니다.");
            return false; // 해제 실패
        }

        // 3. AddItem이 성공했다면, 장비 슬롯에서 이 아이템을 제거
        equipmentSlots[equipSlotIndex] = null;

        // 4. (플레이어 스탯 적용 해제 - PlayerAbilityManager 호출)
        if (itemToUnequip.grantedAbility != null)
        {
            PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
            if (playerAbilities != null)
            {
                // playerAbilities에 "RemoveRelic" 함수가 있다고 가정
                playerAbilities.RemoveRelic(itemToUnequip.itemID);
            }
        }

        // 5. '장비' UI에만 신호를 보냄 (인벤토리 신호는 AddItem이 알아서 보냄)
        OnEquipmentChanged?.Invoke();

        Debug.Log(itemToUnequip.itemName + "을(를) 장착 해제했습니다.");
        return true;
    }
}