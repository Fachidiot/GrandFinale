using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class NpcInputSensor : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    [Header("Reference")]
    [SerializeField] private StationaryBrain brain;

    private GameObject _detectedPlayer;

    private void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
        if (!brain) brain = GetComponentInParent<StationaryBrain>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;
        if ((targetLayer.value & (1 << other.gameObject.layer)) == 0) return;

        _detectedPlayer = other.gameObject;
        UIEvents.FireInteractState(brain.GetPrompt());
    }

    private void OnTriggerExit(Collider other)
    {
        if (_detectedPlayer != null && other.gameObject == _detectedPlayer)
        {
            ReleaseTarget();
        }
    }

    private void Update()
    {
        if (_detectedPlayer == null) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (InventoryManager.Instance != null
                && InventoryManager.Instance.IsUIOpen
                && !InventoryManager.Instance.IsExternalInteractionActive) 
            {
                InventoryManager.Instance.ToggleInventory();
            }

            brain.OnInteract(_detectedPlayer);
        }
    }

    private void ReleaseTarget()
    {
        _detectedPlayer = null;
        UIEvents.FireInteractState(null);
    }

    private void OnDisable()
    {
        if (_detectedPlayer != null) ReleaseTarget();
    }
}