using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Settings")]
    public string interactionText = "Interact";

    [Header("Events")]
    [SerializeField] private UnityEvent onInteract;

    public void Interact()
    {
        onInteract.Invoke();
    }
}