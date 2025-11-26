using UnityEngine.Events;
using UnityEngine;
using System.Collections.Generic;

public class EventReceiver : MonoBehaviour
{
    [SerializeField] private List<UnityEvent> debugEvent;
    [SerializeField] UnityEvent enableEvent;
    [SerializeField] UnityEvent disableEvent;

    [ContextMenu("Debug Event 1 Active")]
    public void DebugEvent1()
    {
        debugEvent[0].Invoke();
    }

    [ContextMenu("Debug Event 2 Active")]
    public void DebugEvent2()
    {
        debugEvent[1].Invoke();
    }

    [ContextMenu("Debug Event 3 Active")]
    public void DebugEvent3()
    {
        debugEvent[2].Invoke();
    }

    [ContextMenu("Debug Event 4 Active")]
    public void DebugEvent4()
    {
        debugEvent[3].Invoke();
    }

    private void OnEnable()
    {
        enableEvent.Invoke();
    }

    private void OnDisable()
    {
        disableEvent.Invoke();
    }
}
