using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class NpcInputSensor : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private StationaryBrain brain;

    private void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
        if (!brain) brain = GetComponentInParent<StationaryBrain>();
    }

    public void ToggleShop(GameObject detectedPlayer)
    {
        if (InventoryManager.Instance != null
            && InventoryManager.Instance.IsUIOpen
            && !InventoryManager.Instance.IsExternalInteractionActive)
        {
            InventoryManager.Instance.ToggleInventory();
        }

        brain.OnInteract(detectedPlayer);
    }
}