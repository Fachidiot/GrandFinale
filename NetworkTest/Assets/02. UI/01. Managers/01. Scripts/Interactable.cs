using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Settings")]
    public string interactionText = "Interact";

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> onInteract;

    public void Interact(GameObject gameObject = null)
    {
        onInteract.Invoke(gameObject);
    }
}