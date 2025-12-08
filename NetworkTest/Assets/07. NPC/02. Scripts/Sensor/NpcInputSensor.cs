using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class NpcInputSensor : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private StationaryBrain brain;

    private bool isActive = false;
    public bool IsActive { get { return isActive; } }

    private void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
        if (!brain) brain = GetComponentInParent<StationaryBrain>();
    }

    // 이름 변경: ToggleShop -> InteractWithNPC
    // 이제 상점뿐만 아니라 대화, 업그레이드 등 모든 상호작용의 입구 역할을 합니다.
    public void InteractWithNPC(GameObject detectedPlayer)
    {
        if (InventoryManager.Instance != null
            && InventoryManager.Instance.IsUIOpen
            && !InventoryManager.Instance.IsExternalInteractionActive)
        {
            InventoryManager.Instance.ToggleInventory();
        }

        // Brain에게 상호작용 신호 전달
        brain.OnInteract(detectedPlayer);
    }

    public void ToggleNPC(GameObject detectedPlayer)
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsUIOpen && !InventoryManager.Instance.IsExternalInteractionActive)
        {
            InventoryManager.Instance.ToggleSmallInventory();
        }

        // Brain에게 상호작용 신호 전달
        if (detectedPlayer)
            brain.OnInteract(detectedPlayer);
        isActive = !isActive;
    }
}