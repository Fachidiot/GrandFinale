using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine;

public class LootManager : MonoBehaviour
{
    public static LootManager Instance { get; private set; }

    [Header("Prefabs & Config")]
    [SerializeField] private GameObject genericLootPrefab;

    private readonly Dictionary<ushort, GameObject> spawnedLootItems = new Dictionary<ushort, GameObject>();
    private ushort nextLootNetId = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        NetworkManager.OnJsonMessageReceived += HandleJsonMessage;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnJsonMessageReceived -= HandleJsonMessage;
        }
    }

    private void HandleJsonMessage(CSteamID sender, string jsonMsg)
    {
        JObject msg = JObject.Parse(jsonMsg);
        string type = msg["type"]?.ToString();

        switch (type)
        {
            case "spawn_loot":
                if (NetworkManager.Instance.Mode == NetworkMode.Client)
                {
                    ushort lootNetId = msg["lootNetId"].ToObject<ushort>();
                    string itemId = msg["itemId"].ToString();
                    Vector3 position = msg["position"].ToObject<Vector3>();
                    SpawnLootInstance(lootNetId, itemId, position);
                }
                break;
            
            case "destroy_loot":
                ushort idToDestroy = msg["lootNetId"].ToObject<ushort>();
                DestroyLoot(idToDestroy);
                break;
        }
    }

    /// <summary>
    /// (HOST-ONLY) Spawns a loot item and tells clients to do the same.
    /// </summary>
    public void SpawnLoot(string itemId, Vector3 position)
    {
        if (NetworkManager.Instance.Mode != NetworkMode.Host) return;

        ushort lootNetId = nextLootNetId++;

        // Broadcast to clients
        JObject spawnMsg = new JObject
        {
            ["type"] = "spawn_loot",
            ["lootNetId"] = lootNetId,
            ["itemId"] = itemId,
            ["position"] = JToken.FromObject(position)
        };
        NetworkManager.Instance.BroadcastJsonMessage(spawnMsg);

        // Spawn locally on host
        SpawnLootInstance(lootNetId, itemId, position);
    }

    /// <summary>
    /// (HOST & CLIENT) Instantiates the actual loot item GameObject.
    /// </summary>
    private void SpawnLootInstance(ushort lootNetId, string itemId, Vector3 position)
    {
        if (spawnedLootItems.ContainsKey(lootNetId)) return; // Already spawned

        if (!DataManager.Instance.RelicDB.TryGetValue(itemId, out RelicData relicData))
        {
            Debug.LogError($"[LootManager] Could not find RelicData for itemId: {itemId}");
            return;
        }

        if (genericLootPrefab == null)
        {
             Debug.LogError("[LootManager] GenericLootDrop prefab is not assigned!");
             return;
        }

        GameObject lootGO = Instantiate(genericLootPrefab, position, Quaternion.identity);
        
        var networkLoot = lootGO.AddComponent<NetworkLoot>();
        networkLoot.lootNetId = lootNetId;

        var itemPickup = lootGO.GetComponent<ItemPickup>();
        if (itemPickup != null)
        {
            itemPickup.itemData = relicData;
        }

        var visualScript = lootGO.GetComponent<LootOrbVisuals>();
        if (visualScript != null)
        {
            visualScript.Initialize(relicData.grade);
        }
        
        lootGO.name = $"Loot_{relicData.itemName}_{lootNetId}";
        spawnedLootItems.Add(lootNetId, lootGO);
    }

    /// <summary>
    /// (HOST & CLIENT) Removes a loot object from the scene.
    /// </summary>
    public void DestroyLoot(ushort lootNetId)
    {
        if (spawnedLootItems.TryGetValue(lootNetId, out GameObject lootGO))
        {
            Destroy(lootGO);
            spawnedLootItems.Remove(lootNetId);
        }
    }
}
