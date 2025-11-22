using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용
using Cinemachine; // 시네머신 사용
using System.Collections.Generic;
using System.Linq; // 리스트 조작용

public class UpgradeModule : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("업그레이드용 가상 카메라")]
    [SerializeField] private CinemachineVirtualCamera upgradeCamera;
    [SerializeField] private int activePriority = 20;

    [Header("UI References")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button strengthenButton; // 강화 버튼

    [Header("Slots")]
    [Tooltip("강화 대상 장비가 놓일 슬롯")]
    [SerializeField] private UpgradeSlotUI equipmentSlot;

    [Tooltip("강화 재료 슬롯들")]
    [SerializeField] private List<MaterialSlotUI> materialSlots;

    [Header("Gauge UI")]
    [SerializeField] private Image gaugeImage;         // 강화 확률 게이지
    [SerializeField] private TMP_Text gaugeText;       // 재료 목록 텍스트

    [Header("Settings")]
    [SerializeField] private float autoCloseDistance = 5f;

    public bool IsOpen { get; private set; }
    private Transform _opener;
    private const float MAX_CHANCE = 1.0f; // 100%

    // --- 데이터 상태 관리 ---
    private RelicData currentEquipment; // 현재 올려진 장비
    // 각 슬롯별로 어떤 아이템이 들어있는지 추적 (Slot UI -> Item Data 매핑)
    private Dictionary<MaterialSlotUI, RelicData> materialSlotData = new Dictionary<MaterialSlotUI, RelicData>();

    private void Start()
    {
        if (upgradePanel) upgradePanel.SetActive(false);
        if (upgradeCamera) upgradeCamera.Priority = 0;

        // 버튼 리스너 연결
        if (strengthenButton)
        {
            strengthenButton.onClick.RemoveAllListeners();
            strengthenButton.onClick.AddListener(StrengthenEquipment);
        }

        // 딕셔너리 초기화
        foreach (var slot in materialSlots)
        {
            if (!materialSlotData.ContainsKey(slot))
                materialSlotData.Add(slot, null);
        }
    }

    // NPCBrain에서 호출
    public void ToggleUpgrade(Transform player)
    {
        if (IsOpen) CloseUpgrade();
        else OpenUpgrade(player);
    }


    public void OpenUpgrade(Transform player)
    {
        if (IsOpen) return;

        IsOpen = true;
        _opener = player;

        UIEvents.FireInteractState(null);

        if (upgradeCamera) upgradeCamera.Priority = activePriority;
        ClearAllSlots();
        if (upgradePanel) upgradePanel.SetActive(true);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetExternalInteractionActive(true);
            InventoryManager.Instance.OpenSmallInventory();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetExternalInteractionActive(false);
            InventoryManager.Instance.CloseAllInventories();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }


    public bool HandleEquipmentDrop(RelicData item, int sourceSlotIndex)
    {
        // 타입 검사 (장비, 무기 등만 가능)
        if (item.itemTypeEnum != ItemType.Equipment &&
            item.itemTypeEnum != ItemType.Weapon &&
            item.itemTypeEnum != ItemType.Artifact &&
            item.itemTypeEnum != ItemType.Accessory)
        {
            Debug.LogWarning("[Upgrade] 강화 가능한 장비 타입이 아닙니다.");
            return false;
        }

        // 스위치 로직: 이미 장비가 있다면 인벤토리로 반환
        if (currentEquipment != null)
        {
            InventoryManager.Instance.AddItem(currentEquipment);
        }

        // 새 장비 등록
        currentEquipment = item;
        InventoryManager.Instance.RemoveItemFromSlot(sourceSlotIndex, 1);

        CheckMaterialCompatibility();

        UpdateUIState();
        return true;
    }

    // 2. 재료 슬롯 드롭 처리
    public bool HandleMaterialDrop(RelicData item, int sourceSlotIndex, MaterialSlotUI targetSlot)
    {
        if (currentEquipment == null)
        {
            Debug.LogWarning("[Upgrade] 먼저 강화할 장비를 등록해주세요.");
            return false;
        }

        // 조건: 강화 대상과 동일한 아이템이어야 함
        if (item.itemID != currentEquipment.itemID)
        {
            Debug.LogWarning("[Upgrade] 강화 대상과 동일한 아이템만 재료로 사용할 수 있습니다.");
            return false;
        }

        // 스위치 로직: 해당 슬롯에 이미 재료가 있다면 반환
        if (materialSlotData.ContainsKey(targetSlot) && materialSlotData[targetSlot] != null)
        {
            InventoryManager.Instance.AddItem(materialSlotData[targetSlot]);
        }

        // 재료 등록
        materialSlotData[targetSlot] = item;
        targetSlot.UpdateUI(item); // 슬롯 UI 갱신
        InventoryManager.Instance.RemoveItemFromSlot(sourceSlotIndex, 1); // 인벤토리에서 제거

        UpdateUIState();
        return true;
    }


    private void CheckMaterialCompatibility()
    {
        foreach (var slot in materialSlots)
        {
            RelicData material = materialSlotData[slot];
            if (material != null && currentEquipment != null)
            {
                // 장비가 바뀌었는데 재료랑 다르면 재료 방출
                if (material.itemID != currentEquipment.itemID)
                {
                    InventoryManager.Instance.AddItem(material);
                    materialSlotData[slot] = null;
                    slot.ClearSlot();
                }
            }
        }
    }

    // UI (게이지, 텍스트) 업데이트
    private void UpdateUIState()
    {
        int materialCount = materialSlotData.Values.Count(x => x != null);
        float chance = (currentEquipment != null && materialCount > 0) ? MAX_CHANCE : 0f;

        // 게이지 업데이트
        if (gaugeImage != null)
            gaugeImage.fillAmount = chance;

        // 텍스트 업데이트: "강화 재료: {아이템1}, {아이템2}"
        if (gaugeText != null)
        {
            if (materialCount > 0)
            {
                List<string> names = new List<string>();
                foreach (var mat in materialSlotData.Values)
                {
                    if (mat != null) names.Add(mat.itemName);
                }
                gaugeText.text = $"강화 재료: {string.Join(", ", names)}";
            }
            else
            {
                gaugeText.text = "강화 재료: 없음";
            }
        }
    }

    // 강화 실행
    public void StrengthenEquipment()
    {
        if (currentEquipment == null) return;

        // 재료가 있는지 확인
        int materialCount = materialSlotData.Values.Count(x => x != null);
        if (materialCount == 0)
        {
            Debug.LogWarning("[Upgrade] 재료가 부족합니다.");
            return;
        }

        // 강화 성공 (100%)
        Debug.Log($"[Upgrade] {currentEquipment.itemName} 강화 성공!");

        foreach (var slot in materialSlots)
        {
            materialSlotData[slot] = null;
            slot.ClearSlot();
        }


        ReturnItemsToInventory();
        CloseUpgrade();
    }

    // 모든 아이템 반환 (닫을 때, 강화 완료 시 등)
    private void ReturnItemsToInventory()
    {
        if (InventoryManager.Instance == null) return;

        // 1. 장비 반환
        if (currentEquipment != null)
        {
            InventoryManager.Instance.AddItem(currentEquipment);
            currentEquipment = null;
        }

        // 2. 재료 반환
        foreach (var key in materialSlotData.Keys.ToList())
        {
            if (materialSlotData[key] != null)
            {
                InventoryManager.Instance.AddItem(materialSlotData[key]);
                materialSlotData[key] = null;
            }
            key.ClearSlot(); // UI 초기화
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

    private void Update()
    {
        if (!IsOpen) return;

        // ESC 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseUpgrade();
        }

        // 거리 멀어지면 닫기
        if (_opener != null && Vector3.Distance(transform.position, _opener.position) > autoCloseDistance)
        {
            CloseUpgrade();
        }
    }
}