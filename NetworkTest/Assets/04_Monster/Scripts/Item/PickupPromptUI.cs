// PickupPromptUI.cs

using UnityEngine;
using TMPro; // TextMeshPro를 사용하려면 필요

public class PickupPromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText; // "E : 아이템 줍기" 텍스트
    [SerializeField] private GameObject promptPanel; // 텍스트를 감싸는 패널 (Prompt_Image)

    private ItemPickup currentNearbyItem; // 현재 가까이 있는 아이템 (이름 표시용)

    void Awake()
    {
        // 처음에는 UI를 숨김
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
    }

    void OnEnable()
    {
        // ItemPickup 스크립트에서 발생하는 이벤트 구독
        ItemPickup.OnPlayerNearbyPickup += HandlePlayerNearbyPickup;
    }

    void OnDisable()
    {
        // 스크립트가 비활성화될 때 이벤트 구독 해제 (메모리 누수 방지)
        ItemPickup.OnPlayerNearbyPickup -= HandlePlayerNearbyPickup;
    }

    // ItemPickup에서 호출되는 이벤트 핸들러
    private void HandlePlayerNearbyPickup(bool show, ItemPickup item)
    {
        if (promptPanel == null || promptText == null) return;

        if (show && item != null && item.itemData != null)
        {
            currentNearbyItem = item;
            //promptText.text = $"E : {item.itemData.itemName} 줍기"; // 아이템 이름 표시
            promptText.text = "E : 아이템을 줍는다";
            promptPanel.SetActive(true); // UI 패널 활성화
        }
        else
        {
            currentNearbyItem = null;
            promptPanel.SetActive(false); // UI 패널 비활성화
        }
    }
}