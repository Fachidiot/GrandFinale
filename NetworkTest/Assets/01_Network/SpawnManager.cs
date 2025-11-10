using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawning Configuration")]
    public GameObject monsterPrefab; // Assign your monster prefab in the Inspector
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

        if (monsterPrefab == null)
        {
            Debug.LogError("Monster Prefab is not assigned in SpawnManager.");
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
            SpawnMonster();
            yield return new WaitForSeconds(spawnInterval);
        }
        Debug.Log($"[SpawnManager] Wave {currentWave} finished spawning.");
    }

    void SpawnMonster()
    {
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
        networkMonster.Initialize(newId);
        monsterGO.name = $"{monsterPrefab.name}_{newId}";
        
        spawnedMonsters.Add(monsterGO);
        Debug.Log($"[SpawnManager] Spawned monster {monsterGO.name}");

        // AI logic is now handled by MonsterMovement.cs on the host
    }
}