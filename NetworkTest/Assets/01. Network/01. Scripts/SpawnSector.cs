using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnSector
{
    public string sectorName;
    public int sectorMaxMonsters;
    public List<Transform> spawnPoints;

    [HideInInspector]
    public int currentMonsters;
}
