using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cinemachine;
using System.Collections.Generic;
using System.Linq;

public class UpgradeModule : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("업그레이드용 가상 카메라")]
    [SerializeField] private CinemachineVirtualCamera upgradeCamera;
    [SerializeField] private int activePriority = 20;

    [Header("UI References")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button strengthenButton;

    [Header("Slots")]
    [Tooltip("강화 대상 장비가 놓일 슬롯")]
    [SerializeField] private UpgradeSlotUI equipmentSlot;

    [Tooltip("강화 재료 슬롯들")]
    [SerializeField] private List<MaterialSlotUI> materialSlots;

    [Header("Gauge UI")]
    [SerializeField] private Image gaugeImage;
    [SerializeField] private TMP_Text gaugeText;

    [Header("Settings")]
    [SerializeField] private float autoCloseDistance = 5f;

    public bool IsOpen { get; private set; }
    private GameObject _opener;
    private const float MAX_CHANCE = 1.0f;

    private const float CHANCE_COMMON = 0.20f;
    private const float CHANCE_RARE = 0.40f;
    private const float CHANCE_EPIC = 0.60f;
    private const float CHANCE_LEGENDARY = 0.80f;
    private const float SAME_ITEM_BONUS = 0.5f;

    private readonly string[] upgradePrefixes = new string[]
    {
        "0",
        "+1",
        "+2",
        "+3",
        "+4",
        "+5",
    };

    private readonly HashSet<string> upgradableStats = new HashSet<string>()
    {
        "MaxHealth", "MoveSpeed", "InfiniteAmmo", "AllDamage",
        "PistolBullet", "RifleBullet", "PlasmaPellet", "Laser",
        "Slash", "Stun", "Defense", "CritChance", "Health",
        "SAS_Module", "Bulwark_Armor"
    };

    private RelicData currentEquipment;
    private Dictionary<MaterialSlotUI, RelicData> materialSlotData = new Dictionary<MaterialSlotUI, RelicData>();

    private void Start()
    {
        if (upgradePanel) upgradePanel.SetActive(false);
        if (upgradeCamera) upgradeCamera.Priority = 0;

        if (strengthenButton)
        {
            strengthenButton.onClick.RemoveAllListeners();
            strengthenButton.onClick.AddListener(StrengthenEquipment);
        }

        foreach (var slot in materialSlots)
        {
            if (!materialSlotData.ContainsKey(slot))
                materialSlotData.Add(slot, null);
        }

        if (equipmentSlot != null) equipmentSlot._module = this;
        foreach (var slot in materialSlots) if (slot != null) slot._module = this;
    }

    public void ToggleUpgrade(GameObject player) { if (IsOpen) CloseUpgrade(); else OpenUpgrade(player); }

    public void OpenUpgrade(GameObject player)
    {
        if (IsOpen) return;
        IsOpen = true;
        _opener = player;
        UIEvents.FireInteractState(null);
        if (upgradeCamera) upgradeCamera.Priority = activePriority;
        ClearAllSlots();
        if (upgradePanel) upgradePanel.SetActive(true);
        if (InventoryManager.Instance != null) { InventoryManager.Instance.SetExternalInteractionActive(true); InventoryManager.Instance.OpenSmallInventory(); }
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void CloseUpgrade()
    {
        if (!IsOpen) return;
        StationaryBrain brain = GetComponent<StationaryBrain>();
        if (brain != null) UIEvents.FireInteractState(brain.GetPrompt());
        ReturnItemsToInventory();
        IsOpen = false;
        _opener = null;
        if (upgradeCamera) upgradeCamera.Priority = 0;
        if (upgradePanel) upgradePanel.SetActive(false);
        if (InventoryManager.Instance != null) { InventoryManager.Instance.SetExternalInteractionActive(false); InventoryManager.Instance.CloseAllInventories(); }
        else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    public void StrengthenEquipment()
    {
        if (currentEquipment == null) return;

        // 1. 재료 확인
        int materialCount = materialSlotData.Values.Count(x => x != null);
        if (materialCount == 0)
        {
            Debug.LogWarning("[Upgrade] 재료가 부족합니다.");
            return;
        }

        // 2. 확률 계산
        float currentChance = gaugeImage != null ? gaugeImage.fillAmount : 0f;
        bool isSuccess = (currentChance >= 1.0f) || (Random.value <= currentChance);

        if (isSuccess)
        {
            Debug.Log($"[Upgrade] {currentEquipment.itemName} 강화 성공!");

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeSuccessClip);

            // 3. 원본 데이터 보호를 위해 복제본 생성
            RelicData upgradedItem = Instantiate(currentEquipment);

            // 4. AbilityData 복제 및 스탯 강화
            if (currentEquipment.grantedAbility != null)
            {
                upgradedItem.grantedAbility = Instantiate(currentEquipment.grantedAbility);
                ApplyStatUpgrade(upgradedItem.grantedAbility);
            }

            // 5. 이름 변경 로직 (접두사 시스템 적용)
            string originalName = currentEquipment.itemName;
            int currentLevel = 0;

            for (int i = 1; i < upgradePrefixes.Length; i++)
            {
                string prefix = upgradePrefixes[i];
                if (originalName.StartsWith(prefix))
                {
                    // 접두사를 찾으면 제거하고 레벨 설정
                    originalName = originalName.Substring(prefix.Length);
                    currentLevel = i;
                    break;
                }
            }

            // 레벨 증가 (최대 레벨 제한)
            int nextLevel = Mathf.Min(currentLevel + 1, upgradePrefixes.Length - 1);

            // 새 접두사 붙이기
            string newPrefix = upgradePrefixes[nextLevel];
            upgradedItem.itemName = $"{newPrefix}{originalName}";

            // 6. 강화된 아이템으로 교체
            currentEquipment = upgradedItem;
        }
        else
        {
            Debug.Log($"[Upgrade] {currentEquipment.itemName} 강화 실패...");

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeFailClip);
            // 실패 시 패널티가 있다면 여기에 추가
        }

        // 7. 재료 소모
        foreach (var slot in materialSlots)
        {
            materialSlotData[slot] = null;
            slot.ClearSlot();
        }

        // 8. 아이템 반환 및 종료
        ReturnItemsToInventory();
        CloseUpgrade();
    }

    private void ApplyStatUpgrade(AbilityData ability)
    {
        if (ability == null) return;

        if (!upgradableStats.Contains(ability.param_Key)) return;

        bool upgraded = false;
        if (TryUpgradeValue(ref ability.param_ValueA)) upgraded = true;
        if (TryUpgradeValue(ref ability.param_ValueB)) upgraded = true;
        if (TryUpgradeValue(ref ability.param_ValueC)) upgraded = true;

        if (upgraded) Debug.Log($"[Upgrade] 능력치 상승 완료! ({ability.param_Key})");
    }

    private bool TryUpgradeValue(ref string paramValue)
    {
        if (string.IsNullOrEmpty(paramValue)) return false;

        if (float.TryParse(paramValue, out float val))
        {
            if (val == 0) return false;

            int bonus = Random.Range(1, 6);
            float newVal = val + bonus;

            paramValue = newVal.ToString();
            return true;
        }
        return false;
    }

    public bool HandleEquipmentDrop(RelicData item, int sourceSlotIndex)
    {
        if (item.itemTypeEnum != ItemType.Equipment &&
            item.itemTypeEnum != ItemType.Weapon &&
            item.itemTypeEnum != ItemType.Artifact &&
            item.itemTypeEnum != ItemType.Accessory)
        {
            Debug.LogWarning("[Upgrade] 강화 가능한 장비 타입이 아닙니다.");
            return false;
        }

        if (currentEquipment != null) InventoryManager.Instance.AddItem(currentEquipment);

        currentEquipment = item;
        InventoryManager.Instance.RemoveItemFromSlot(sourceSlotIndex, 1);

        UpdateUIState();
        return true;
    }

    public bool HandleMaterialDrop(RelicData item, int sourceSlotIndex, MaterialSlotUI targetSlot)
    {
        if (currentEquipment == null)
        {
            Debug.LogWarning("[Upgrade] 먼저 강화할 장비를 등록해주세요.");
            return false;
        }

        if (materialSlotData.ContainsKey(targetSlot) && materialSlotData[targetSlot] != null)
        {
            InventoryManager.Instance.AddItem(materialSlotData[targetSlot]);
        }

        materialSlotData[targetSlot] = item;
        targetSlot.UpdateUI(item);
        InventoryManager.Instance.RemoveItemFromSlot(sourceSlotIndex, 1);

        UpdateUIState();
        return true;
    }


    private void UpdateUIState()
    {
        float totalChance = 0f;

        if (currentEquipment != null)
        {
            foreach (var material in materialSlotData.Values)
            {
                if (material == null) continue;

                if (material.itemID == currentEquipment.itemID)
                    totalChance += SAME_ITEM_BONUS;
                else
                    totalChance += GetChanceByGrade(material.grade);
            }
        }

        totalChance = Mathf.Clamp01(totalChance);

        if (gaugeImage != null) gaugeImage.fillAmount = totalChance;

        if (gaugeText != null)
        {
            List<string> names = materialSlotData.Values.Where(x => x != null).Select(x => x.itemName).ToList();
            string materialListStr = names.Count > 0 ? string.Join(", ", names) : "없음";
            string chanceColor = totalChance >= 1.0f ? "#00FF00" : "#FFFFFF";
            gaugeText.text = $"재료: {materialListStr}\n성공 확률: <color={chanceColor}>{(totalChance * 100):F0}%</color>";
        }
    }

    private float GetChanceByGrade(string grade)
    {
        switch (grade.ToLower())
        {
            case "common": case "normal": return CHANCE_COMMON;
            case "rare": return CHANCE_RARE;
            case "epic": return CHANCE_EPIC;
            case "legendary": return CHANCE_LEGENDARY;
            default: return 0.05f;
        }
    }

    private void ReturnItemsToInventory()
    {
        if (InventoryManager.Instance == null) return;

        if (currentEquipment != null)
        {
            InventoryManager.Instance.AddItem(currentEquipment);
            currentEquipment = null;
        }

        foreach (var key in materialSlotData.Keys.ToList())
        {
            if (materialSlotData[key] != null)
            {
                InventoryManager.Instance.AddItem(materialSlotData[key]);
                materialSlotData[key] = null;
            }
            key.ClearSlot();
        }

        if (equipmentSlot) equipmentSlot.ClearSlot();
        UpdateUIState();
    }

    private void ClearAllSlots()
    {
        if (equipmentSlot) equipmentSlot.ClearSlot();
        foreach (var slot in materialSlots) slot.ClearSlot();
        currentEquipment = null;
        foreach (var key in materialSlotData.Keys.ToList()) materialSlotData[key] = null;
        UpdateUIState();
    }

    public void ReturnEquipmentToInventory()
    {
        if (currentEquipment != null)
        {
            InventoryManager.Instance.AddItem(currentEquipment);
            currentEquipment = null;
            equipmentSlot.ClearSlot();
            UpdateUIState();
        }
    }

    public void ReturnMaterialToInventory(MaterialSlotUI slotUI)
    {
        if (materialSlotData.ContainsKey(slotUI) && materialSlotData[slotUI] != null)
        {
            InventoryManager.Instance.AddItem(materialSlotData[slotUI]);
            materialSlotData[slotUI] = null;
            slotUI.ClearSlot();
            UpdateUIState();
        }
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) CloseUpgrade();
        if (_opener != null && Vector3.Distance(transform.position, _opener.transform.position) > autoCloseDistance) CloseUpgrade();
    }
}