using UnityEngine.Events;
using UnityEngine;

public class EventReceiver : MonoBehaviour
{
    [SerializeField] UnityEvent enableEvent;
    [SerializeField] UnityEvent disableEvent;

    private void OnEnable()
    {
        enableEvent.Invoke();
    }

    void OnDisable()
    {
        disableEvent.Invoke();
    }
}
