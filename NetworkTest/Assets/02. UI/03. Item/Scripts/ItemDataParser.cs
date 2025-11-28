#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ItemDataParser
{
    #region Paths & Constants

    private const string ABILITY_DATA_PATH = "Assets/02. UI/03. Item/Data/Abilities";
    private const string RELIC_DATA_PATH = "Assets/02. UI/03. Item/Data/Relics";
    private const string MASTER_DB_PATH = "Assets/02. UI/03. Item/Data/MasterDatabase/MasterDatabase.asset";

    // Resource Prefab Path (e.g., "Assets/Resources/")
    private const string RESOURCE_PREFAB_PATH = "Assets/Resources/";

    #endregion

    #region 1. Import AbilityData

    [MenuItem("MyTools/Import Data/1. Import AbilityData (CSV)")]
    public static void ImportAbilityData()
    {
        string path = EditorUtility.OpenFilePanel("Import Ability CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        Directory.CreateDirectory(ABILITY_DATA_PATH);
        string[] allLines = File.ReadAllLines(path);

        if (allLines.Length <= 1) return;

        Debug.Log($"[AbilityParser] Importing {allLines.Length - 1} entries...");

        for (int i = 1; i < allLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(allLines[i])) continue;
            string[] row = SplitCSVLine(allLines[i]);

            if (row.Length < 9)
            {
                Debug.LogWarning($"[AbilityParser] Invalid row skipped: {allLines[i]}");
                continue;
            }

            string abilityID = row[0].Trim();
            if (string.IsNullOrEmpty(abilityID)) continue;

            string assetPath = $"{ABILITY_DATA_PATH}/{abilityID}.asset";
            AbilityData ability = AssetDatabase.LoadAssetAtPath<AbilityData>(assetPath);

            if (ability == null)
            {
                ability = ScriptableObject.CreateInstance<AbilityData>();
                AssetDatabase.CreateAsset(ability, assetPath);
            }

            ability.abilityID = abilityID;
            ability.abilityName = GetRow(row, 1);
            ability.activationType = GetRow(row, 2);
            ability.abilityLogicID = GetRow(row, 3);
            ability.param_Key = GetRow(row, 4);
            ability.param_ValueA = GetRow(row, 5);
            ability.param_ValueB = GetRow(row, 6);
            ability.param_ValueC = GetRow(row, 7);
            ability.resourcePath = GetRow(row, 8);

            EditorUtility.SetDirty(ability);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AbilityParser] Import Completed!");
        UpdateMasterDatabase();
    }

    #endregion

    #region 2. Import RelicData

    [MenuItem("MyTools/Import Data/2. Import RelicData (CSV)")]
    public static void ImportRelicData()
    {
        string path = EditorUtility.OpenFilePanel("Import Relic CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        Directory.CreateDirectory(RELIC_DATA_PATH);
        string[] allLines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        if (allLines.Length <= 1) return;

        Debug.Log($"[RelicParser] Importing {allLines.Length - 1} entries...");

        for (int i = 1; i < allLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(allLines[i])) continue;
            string[] row = SplitCSVLine(allLines[i]);

            if (row.Length < 2) continue;

            string itemID = row[0].Trim();
            if (string.IsNullOrEmpty(itemID)) continue;

            string assetPath = $"{RELIC_DATA_PATH}/{itemID}.asset";
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(assetPath);

            if (relic == null)
            {
                relic = ScriptableObject.CreateInstance<RelicData>();
                AssetDatabase.CreateAsset(relic, assetPath);
            }

            // 0. Basic Info
            relic.itemID = itemID;
            relic.itemName = GetRow(row, 1);

            // 1. Enum Types
            relic.itemTypeEnum = ParseEnum<ItemType>(GetRow(row, 2), ItemType.Etc);
            relic.equipmentSlot = ParseEnum<EquipmentSlot>(GetRow(row, 3), EquipmentSlot.None);
            relic.weaponType = ParseEnum<WeaponType>(GetRow(row, 4), WeaponType.None);

            // 2. Attributes
            relic.grade = GetRow(row, 5, "Common");
            relic.description = GetRow(row, 6).Replace("\\n", "\n");
            relic.iconPath = GetRow(row, 7);

            // 3. Ability Link
            string abilityID = GetRow(row, 8);
            if (!string.IsNullOrEmpty(abilityID) && abilityID != "0")
            {
                relic.grantedAbility = AssetDatabase.LoadAssetAtPath<AbilityData>($"{ABILITY_DATA_PATH}/{abilityID}.asset");
            }
            else
            {
                relic.grantedAbility = null;
            }

            // 4. Stats
            int.TryParse(GetRow(row, 9, "1"), out relic.maxStack);
            int.TryParse(GetRow(row, 10, "0"), out relic.price);

            // 5. Model Prefab Link
            string modelPath = GetRow(row, 11);
            relic.modelPath = modelPath;

            if (!string.IsNullOrEmpty(modelPath) && modelPath != "None")
            {
                string fullPrefabPath = $"{RESOURCE_PREFAB_PATH}{modelPath}.prefab";
                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPrefabPath);

                if (modelPrefab != null)
                {
                    relic.modelPrefab = modelPrefab;
                }
                else
                {
                    Debug.LogWarning($"[RelicParser] Model not found: {fullPrefabPath} (Item: {itemID})");
                    relic.modelPrefab = null;
                }
            }
            else
            {
                relic.modelPrefab = null;
            }

            EditorUtility.SetDirty(relic);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[RelicParser] Import Completed!");
        UpdateMasterDatabase();
    }

    #endregion

    #region 3. Update Master Database

    [MenuItem("MyTools/Import Data/3. Update Master Database")]
    public static void UpdateMasterDatabase()
    {
        Debug.Log("[MasterDB] Updating Database...");

        MasterDatabase db = AssetDatabase.LoadAssetAtPath<MasterDatabase>(MASTER_DB_PATH);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<MasterDatabase>();
            AssetDatabase.CreateAsset(db, MASTER_DB_PATH);
        }

        if (db.allAbilities == null) db.allAbilities = new List<AbilityData>();
        if (db.allRelics == null) db.allRelics = new List<RelicData>();

        db.allAbilities.Clear();
        db.allRelics.Clear();

        string[] abilityGUIDs = AssetDatabase.FindAssets("t:AbilityData", new[] { ABILITY_DATA_PATH });
        foreach (string guid in abilityGUIDs)
        {
            db.allAbilities.Add(AssetDatabase.LoadAssetAtPath<AbilityData>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        string[] relicGUIDs = AssetDatabase.FindAssets("t:RelicData", new[] { RELIC_DATA_PATH });
        foreach (string guid in relicGUIDs)
        {
            db.allRelics.Add(AssetDatabase.LoadAssetAtPath<RelicData>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MasterDB] Update Finished (Abilities: {db.allAbilities.Count}, Relics: {db.allRelics.Count})");
    }

    #endregion

    #region Helper Methods

    private static string GetRow(string[] row, int index, string defaultVal = "")
    {
        return (row.Length > index) ? row[index].Trim() : defaultVal;
    }

    private static T ParseEnum<T>(string value, T defaultValue) where T : struct
    {
        if (System.Enum.TryParse(value, true, out T result)) return result;
        return defaultValue;
    }

    private static string[] SplitCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        foreach (char c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Replace("\"", "").Trim());
                current = "";
            }
            else current += c;
        }
        result.Add(current.Replace("\"", "").Trim());
        return result.ToArray();
    }

    #endregion
}
#endif