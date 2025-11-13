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
    public List<MonsterPrefabMapping> monsterPrefabs; // List of all available monster prefabs
    public Transform spawnPoint; // Assign a spawn point in the Inspector

    public float timeBetweenWaves = 5f;
    public int monstersPerWave = 5;
    public float spawnInterval = 1f;

    private int currentWave = 0;
    private ushort nextMonsterId = 0;

    private readonly List<GameObject> spawnedMonsters = new List<GameObject>();
    public IReadOnlyList<GameObject> SpawnedMonsters => spawnedMonsters;

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
        // Spawning is a host-only responsibility
        if (NetworkManager.Instance.Mode != NetworkMode.Host)
        {
            enabled = false;
            return;
        }

        if (monsterPrefabs == null || monsterPrefabs.Count == 0)
        {
            Debug.LogError("Monster Prefabs are not assigned in SpawnManager.");
            return;
        }
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn Point is not assigned in SpawnManager.");
            return;
        }

        StartCoroutine(WaveSpawner());
    }

    IEnumerator WaveSpawner()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            currentWave++;
            Debug.Log($"[SpawnManager] Starting Wave {currentWave}");
            yield return StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        for (int i = 0; i < monstersPerWave; i++)
        {
            // Randomly select a monster type to spawn from the available prefabs
            if (monsterPrefabs.Count > 0)
            {
                int randomIndex = Random.Range(0, monsterPrefabs.Count);
                MonsterType randomType = monsterPrefabs[randomIndex].type;
                SpawnMonster(randomType);
            }
            yield return new WaitForSeconds(spawnInterval);
        }
        Debug.Log($"[SpawnManager] Wave {currentWave} finished spawning.");
    }

    void SpawnMonster(MonsterType monsterType)
    {
        GameObject monsterPrefab = GetPrefab(monsterType);
        if (monsterPrefab == null)
        {
            Debug.LogError($"No prefab found for monster type: {monsterType}");
            return;
        }

        GameObject monsterGO = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Initialize network identity
        NetworkMonster networkMonster = monsterGO.GetComponent<NetworkMonster>();
        if (networkMonster == null)
        {
            Debug.LogError("Monster prefab is missing the NetworkMonster component!");
            Destroy(monsterGO);
            return;
        }
        
        ushort newId = nextMonsterId++;
        networkMonster.Initialize(newId, monsterType); // Pass monster type during initialization
        monsterGO.name = $"{monsterPrefab.name}_{newId}";
        
        spawnedMonsters.Add(monsterGO);
        Debug.Log($"[SpawnManager] Spawned monster {monsterGO.name} of type {monsterType}");

        // AI logic is now handled by MonsterMovement.cs on the host
    }

    /// <summary>
    /// Gets the prefab associated with a given MonsterType.
    /// </summary>
    public GameObject GetPrefab(MonsterType type)
    {
        var mapping = monsterPrefabs.FirstOrDefault(m => m.type == type);
        return mapping?.prefab;
    }
}