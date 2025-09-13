using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBoxColidersList : MonoBehaviour
{
    [SerializeField] private List<Collider> hitBoxColiders = new List<Collider>();

    public void Init()
    {
        var allColiders = transform.root.GetComponentsInChildren<Collider>();

        foreach (var collider in allColiders)
        {
            if (collider.CompareTag("HitBox"))
            {
                hitBoxColiders.Add(collider);
                collider.isTrigger = false;
                collider.GetComponent<Rigidbody>().isKinematic = true;
            }
        }
    }

    public void Activate()
    {
        foreach (var collider in hitBoxColiders)
        {
            collider.GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    public void HitboxesAsTriggers(bool asTriggers)
    {
        foreach (var collider in hitBoxColiders)
        {
            collider.isTrigger = asTriggers;
        }
    }
}
