using UnityEngine;

public class NetworkMonster : MonoBehaviour
{
    public ushort MonsterId { get; private set; }
    public MonsterType MonsterType { get; private set; }
    public SpawnSector Sector { get; set; }

    public void Initialize(ushort id, MonsterType type)
    {
        MonsterId = id;
        MonsterType = type;
    }
}