using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// A helper class to map a MonsterType enum to a GameObject prefab in the Inspector
[System.Serializable]
public class MonsterPrefabMapping
{
    public MonsterType type;
    public GameObject prefab;
}

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawning Configuration")]
    public int maxEntity = 30;
    public List<MonsterPrefabMapping> monsterPrefabs;
    public List<MonsterPrefabMapping> NetworkedMonsterPrefabs;
    public Transform spawnPoint;
    public int initialPoolSize = 10; // Number of each monster type to pre-spawn

    [Header("Wave Settings")]
    public float timeBetweenWaves = 5f;
    public int monstersPerWave = 5;
    public float spawnInterval = 1f;

    private int currentWave = 0;
    private ushort nextMonsterId = 0;

    private readonly List<GameObject> spawnedMonsters = new List<GameObject>();
    public IReadOnlyList<GameObject> SpawnedMonsters => spawnedMonsters;

    // Object Pooling
    private Dictionary<MonsterType, Queue<GameObject>> monsterPools = new Dictionary<MonsterType, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (NetworkManager.Instance.Mode == NetworkMode.Client)
        {
            enabled = false;
            return;
        }

        if (monsterPrefabs == null || monsterPrefabs.Count == 0 || spawnPoint == null)
        {
            Debug.LogError("SpawnManager is not configured correctly.");
            return;
        }

        PrewarmPools();
        StartCoroutine(WaveSpawner());
    }

    void PrewarmPools()
    {
        foreach (var mapping in monsterPrefabs)
        {
            if (mapping.prefab == null)
            {
                Debug.LogWarning($"SpawnManager: Prefab for monster type '{mapping.type}' is null. Skipping.");
                continue;
            }

            if (!monsterPools.ContainsKey(mapping.type))
            {
                Queue<GameObject> pool = new Queue<GameObject>();
                monsterPools.Add(mapping.type, pool);
                for (int i = 0; i < initialPoolSize; i++)
                {
                    GameObject monsterGO = Instantiate(mapping.prefab, spawnPoint.position, spawnPoint.rotation);
                    monsterGO.SetActive(false);
                    pool.Enqueue(monsterGO);
                }
            }
            else
            {
                Debug.LogWarning($"SpawnManager: Duplicate monster type '{mapping.type}' found in prefab list. Ignoring duplicate.");
            }
        }
    }

    IEnumerator WaveSpawner()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            currentWave++;
            // Debug.Log($"[SpawnManager] Starting Wave {currentWave}");
            yield return StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        for (int i = 0; i < monstersPerWave; i++)
        {
            if (monsterPrefabs.Count > 0)
            {
                int randomIndex = Random.Range(0, monsterPrefabs.Count);
                MonsterType randomType = monsterPrefabs[randomIndex].type;
                SpawnMonster(randomType);
            }
            yield return new WaitForSeconds(spawnInterval);
        }
        // Debug.Log($"[SpawnManager] Wave {currentWave} finished spawning.");
    }

    GameObject SpawnMonster(MonsterType monsterType)
    {
        GameObject monsterPrefab = GetPrefab(monsterType);
        if (monsterPrefab == null) return null;

        GameObject monsterGO = null;
        if (monsterPools.TryGetValue(monsterType, out Queue<GameObject> pool) && pool.Count > 0)
        {
            monsterGO = pool.Dequeue();
            monsterGO.transform.position = spawnPoint.position;
            monsterGO.transform.rotation = spawnPoint.rotation;
            monsterGO.SetActive(true);
        }
        else
        {
            monsterGO = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        NetworkMonster networkMonster = monsterGO.GetComponent<NetworkMonster>();
        if (networkMonster == null)
        {
            Debug.LogError("Monster prefab is missing the NetworkMonster component!");
            Destroy(monsterGO);
            return null;
        }

        ushort newId = nextMonsterId++;
        networkMonster.Initialize(newId, monsterType);
        monsterGO.name = $"{monsterPrefab.name}_{newId}";

        spawnedMonsters.Add(monsterGO);
        Debug.Log($"[SpawnManager] Spawned monster {monsterGO.name} of type {monsterType}");

        // Broadcast the spawn event to all clients
        var monsterState = new MonsterState
        {
            monsterId = newId,
            monsterType = monsterType,
            position = monsterGO.transform.position,
            rotation = monsterGO.transform.rotation,
            animationData = null // Animation data will be sent in the regular game state updates
        };
        NetworkManager.Instance.BroadcastMonsterSpawn(monsterState);

        return monsterGO;
    }

    public void ReturnMonsterToPool(GameObject monsterGO)
    {
        var networkMonster = monsterGO.GetComponent<NetworkMonster>();
        if (networkMonster == null)
        {
            Destroy(monsterGO); // Not a pooled monster
            return;
        }

        if (monsterPools.TryGetValue(networkMonster.MonsterType, out Queue<GameObject> pool))
        {
            // Only the host should broadcast despawn messages
            if (NetworkManager.Instance.Mode == NetworkMode.Host)
            {
                NetworkManager.Instance.BroadcastMonsterDespawn(networkMonster.MonsterId);
            }

            monsterGO.SetActive(false);
            pool.Enqueue(monsterGO);
            spawnedMonsters.Remove(monsterGO);
        }
        else
        {
            Destroy(monsterGO); // No pool for this type
        }
    }

    public GameObject GetMonsterFromPool(MonsterType monsterType)
    {
        GameObject monsterGO = null;
        if (monsterPools.TryGetValue(monsterType, out Queue<GameObject> pool) && pool.Count > 0)
        {
            monsterGO = pool.Dequeue();
            monsterGO.SetActive(true);
        }
        else
        {
            // If pool is empty, instantiate a new one (fallback)
            GameObject prefab = GetPrefab(monsterType);
            if (prefab != null)
            {
                monsterGO = Instantiate(prefab);
            }
        }
        return monsterGO;
    }

    public GameObject GetPrefab(MonsterType type)
    {
        var mapping = monsterPrefabs.FirstOrDefault(m => m.type == type);
        return mapping?.prefab;
    }
}