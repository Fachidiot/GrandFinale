using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private readonly GameObject _prefab;
    private readonly Queue<GameObject> _availableObjects = new Queue<GameObject>();
    private readonly Transform _parent;

    public ObjectPool(GameObject prefab, int initialSize, Transform parent)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            var obj = GameObject.Instantiate(_prefab, _parent);
            obj.name = _prefab.name; // Ensure the name is consistent
            obj.SetActive(false);
            _availableObjects.Enqueue(obj);
        }
    }

    public GameObject GetObject()
    {
        // Find a valid object in the queue
        while (_availableObjects.Count > 0)
        {
            var obj = _availableObjects.Dequeue();
            
            // Check if the object was destroyed while in the pool
            if (obj != null)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        // If no valid objects were found in the pool, create a new one
        var newObj = GameObject.Instantiate(_prefab, _parent);
        newObj.name = _prefab.name;
        return newObj;
    }

    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
        _availableObjects.Enqueue(obj);
    }
}
