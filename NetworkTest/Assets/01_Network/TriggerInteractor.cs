using System;
using UnityEngine;
using UnityEngine.Events;

public class TriggerInteractor : MonoBehaviour
{
    [SerializeField] private UnityEvent function;

    PlayerInputs playerInputs;

    void Start()
    {
        playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();
    }

    void OnTriggerStay(Collider other)
    {
        if (playerInputs.GetInteract() && other.CompareTag("Player"))
        {
            function.Invoke();
        }
    }
}
