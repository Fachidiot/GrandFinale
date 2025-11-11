// LootTable.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LootEntry
{
    public RelicData item;
    [Range(0f, 100f)]
    public float dropChance;
}

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Item/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootEntry> items;
}