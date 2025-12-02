using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;

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
    public int globalMaxMonsters = 50;
    public List<SpawnSector> sectors;
    public List<MonsterPrefabMapping> monsterPrefabs;
    public List<MonsterPrefabMapping> NetworkedMonsterPrefabs;
    public int initialPoolSize = 10; // Number of each monster type to pre-spawn

    [Header("Wave Settings")]
    public float timeBetweenWaves = 5f;
    public int monstersPerWave = 5;
    public float spawnInterval = 1f;

    private int currentWave = 0;
    private ushort nextMonsterId = 0;
    private int currentGlobalMonsters = 0;

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

        if (monsterPrefabs == null || monsterPrefabs.Count == 0 || sectors == null || sectors.Count == 0)
        {
            Debug.LogError("SpawnManager is not configured correctly. Check monster prefabs and sectors.");
            enabled = false;
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
                    // Instantiate at own position and disable, ready for spawning.
                    GameObject monsterGO = Instantiate(mapping.prefab, transform.position, transform.rotation);
                    monsterGO.SetActive(false);
                    pool.Enqueue(monsterGO);
                }
            }
        }
    }

    IEnumerator WaveSpawner()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            currentWave++;
            yield return StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        for (int i = 0; i < monstersPerWave; i++)
        {
            // Find all sectors that are not full
            List<SpawnSector> availableSectors = sectors.Where(s => s.currentMonsters < s.sectorMaxMonsters).ToList();

            if (availableSectors.Count > 0 && monsterPrefabs.Count > 0)
            {
                // Choose a random sector from the available ones
                SpawnSector chosenSector = availableSectors[Random.Range(0, availableSectors.Count)];

                // Choose a random monster type
                MonsterType randomType = monsterPrefabs[Random.Range(0, monsterPrefabs.Count)].type;
                
                SpawnMonster(randomType, chosenSector);
            }
            else
            {
                // Optional: Log that no available sectors were found
                // Debug.Log("[SpawnManager] No available sectors to spawn in. Skipping spawn.");
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    GameObject SpawnMonster(MonsterType monsterType, SpawnSector sector)
    {
        // Check global and sector limits before spawning
        if (currentGlobalMonsters >= globalMaxMonsters || sector.currentMonsters >= sector.sectorMaxMonsters)
        {
            return null;
        }

        GameObject monsterPrefab = GetPrefab(monsterType);
        if (monsterPrefab == null) return null;

        // Select a random spawn point from the chosen sector
        Transform spawnPoint = sector.spawnPoints[Random.Range(0, sector.spawnPoints.Count)];
        
        // Find a valid position on the NavMesh near the spawn point
        if (!NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            Debug.LogError($"[SpawnManager] Could not find a valid NavMesh position near spawn point {spawnPoint.name} in sector {sector.sectorName}. Monster not spawned.");
            return null;
        }
        Vector3 spawnPosition = hit.position;

        GameObject monsterGO = null;
        if (monsterPools.TryGetValue(monsterType, out Queue<GameObject> pool) && pool.Count > 0)
        {
            monsterGO = pool.Dequeue();
            monsterGO.transform.position = spawnPosition;
            monsterGO.transform.rotation = spawnPoint.rotation;
            monsterGO.SetActive(true);
        }
        else
        {
            monsterGO = Instantiate(monsterPrefab, spawnPosition, spawnPoint.rotation);
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
        networkMonster.Sector = sector; // Assign sector to the monster
        monsterGO.name = $"{monsterPrefab.name}_{newId}";

        spawnedMonsters.Add(monsterGO);
        currentGlobalMonsters++;
        sector.currentMonsters++;
        
        // Broadcast the spawn event to all clients
        var monsterState = new MonsterState
        {
            monsterId = newId,
            monsterType = monsterType,
            position = monsterGO.transform.position,
            rotation = monsterGO.transform.rotation,
            animationData = null
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
            // Only the host should broadcast despawn messages and manage counts
            if (NetworkManager.Instance.Mode == NetworkMode.Host)
            {
                NetworkManager.Instance.BroadcastMonsterDespawn(networkMonster.MonsterId);
                
                // Decrement counts
                currentGlobalMonsters--;
                if (networkMonster.Sector != null)
                {
                    networkMonster.Sector.currentMonsters--;
                }
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