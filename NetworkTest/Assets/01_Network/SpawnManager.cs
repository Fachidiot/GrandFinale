using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public GameObject monsterPrefab; // Assign your monster prefab in the Inspector
    public Transform spawnPoint; // Assign a spawn point in the Inspector
    public Transform targetDestination; // Assign the target for monsters (e.g., player)

    public float timeBetweenWaves = 5f;
    public int monstersPerWave = 5;
    public float spawnInterval = 1f;

    private int currentWave = 0;
    private bool waveInProgress = false;

    void Start()
    {
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
        if (targetDestination == null)
        {
            Debug.LogWarning("Target Destination is not assigned. Monsters might not move.");
        }

        StartCoroutine(WaveSpawner());
    }

    IEnumerator WaveSpawner()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            currentWave++;
            Debug.Log($"Starting Wave {currentWave}");
            yield return StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        waveInProgress = true;
        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster();
            yield return new WaitForSeconds(spawnInterval);
        }
        waveInProgress = false;
        Debug.Log($"Wave {currentWave} finished spawning.");
    }

    void SpawnMonster()
    {
        GameObject monster = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);
        MonsterMovement monsterMovement = monster.GetComponent<MonsterMovement>();

        if (monsterMovement != null && targetDestination != null)
        {
            monsterMovement.target = targetDestination;
        }
        else if (monsterMovement == null)
        {
            Debug.LogWarning("Monster prefab does not have a MonsterMovement component.");
        }
    }
}