using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    // Use the prefab's instance ID as the key for robustness.
    private readonly Dictionary<int, ObjectPool> _pools = new Dictionary<int, ObjectPool>();
    // Maps a spawned object's instance ID back to its pool.
    private readonly Dictionary<int, ObjectPool> _spawnedObjects = new Dictionary<int, ObjectPool>();

    private readonly Dictionary<GameObject, BulletBehaviour> _bulletComponentCache = new Dictionary<GameObject, BulletBehaviour>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void CreatePool(GameObject prefab, int initialSize)
    {
        if (prefab == null) return;

        int poolKey = prefab.GetInstanceID();
        if (_pools.ContainsKey(poolKey)) return;

        var pool = new ObjectPool(prefab, initialSize, transform);
        _pools[poolKey] = pool;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        int poolKey = prefab.GetInstanceID();
        if (!_pools.ContainsKey(poolKey))
        {
            CreatePool(prefab, 5); // Create pool with a default size if it doesn't exist
        }

        var pool = _pools[poolKey];
        var obj = pool.GetObject();

        // IPoolable 인터페이스를 가진 컴포넌트를 찾아 리셋 메소드 호출
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.ResetState();
        }

        // Map the instance to its pool for easy return
        _spawnedObjects.Add(obj.GetInstanceID(), pool);

        obj.transform.position = position;
        obj.transform.rotation = rotation;

        return obj;
    }

    public BulletBehaviour GetBulletComponent(GameObject bulletObject)
    {
        if (_bulletComponentCache.TryGetValue(bulletObject, out var component))
        {
            return component;
        }

        var newComponent = bulletObject.GetComponent<BulletBehaviour>();
        if (newComponent != null)
        {
            _bulletComponentCache.Add(bulletObject, newComponent);
        }
        return newComponent;
    }

    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        int instanceId = obj.GetInstanceID();
        if (_spawnedObjects.TryGetValue(instanceId, out var pool))
        {
            pool.ReturnObject(obj);
            _spawnedObjects.Remove(instanceId);
        }
        else
        {
            // Debug.LogWarning("Trying to return object that was not spawned from a pool: " + obj.name);
            Destroy(obj);
        }
    }

    public void ReturnToPool(GameObject obj, float delay)
    {
        StartCoroutine(ReturnWithDelay(obj, delay));
    }

    private IEnumerator ReturnWithDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(obj);
    }
}
