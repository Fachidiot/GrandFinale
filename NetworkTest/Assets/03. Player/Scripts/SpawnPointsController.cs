using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointsController : MonoBehaviour
{
    public static SpawnPointsController instance;

    public Transform[] spawnPoints;
    // Start is called before the first frame update
    void Start()
    {
        if (instance != null) Destroy(instance);

        instance = this;
    }

    public Vector3 GetRandomSpawnPoints()
    {
        int pointId = Random.Range(0, spawnPoints.Length - 1);
        return spawnPoints[pointId].position;
    } 
}
