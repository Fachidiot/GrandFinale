using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ItemDataParser
{
    // 데이터 에셋 경로
    private const string ABILITY_DATA_PATH = "Assets/02. UI/03. Item/Data/Abilities";
    private const string RELIC_DATA_PATH = "Assets/02. UI/03. Item/Data/Relics";
    private const string MASTER_DB_PATH = "Assets/02. UI/03. Item/Data/MasterDatabase/MasterDatabase.asset";

    // 모델 프리팹 최상위 경로 (Resources 기준)
    private const string RESOURCE_PREFAB_PATH = "Assets/Resources/";

    // 1. AbilityData 임포트 (기존 기능 유지)
    [MenuItem("MyTools/Import Data/1. Import AbilityData (CSV)")]
    public static void ImportAbilityData()
    {
        string path = EditorUtility.OpenFilePanel("Import Ability CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        Directory.CreateDirectory(ABILITY_DATA_PATH);

        string[] allLines = File.ReadAllLines(path);
        if (allLines.Length <= 1) return;

        Debug.Log($"[AbilityParser] {allLines.Length - 1}개 데이터 임포트 시작...");

        for (int i = 1; i < allLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(allLines[i])) continue;

            string[] row = SplitCSVLine(allLines[i]);

            if (row.Length < 9)
            {
                Debug.LogWarning($"[AbilityParser] 줄 무시됨 (열 부족): {allLines[i]}");
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
            ability.abilityName = row[1].Trim();
            ability.activationType = row[2].Trim();
            ability.abilityLogicID = row[3].Trim();
            ability.param_Key = row[4].Trim();
            ability.param_ValueA = row[5].Trim();
            ability.param_ValueB = row[6].Trim();
            ability.param_ValueC = row[7].Trim();
            ability.resourcePath = row[8].Trim();

            EditorUtility.SetDirty(ability);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AbilityParser] 임포트 완료!");
        UpdateMasterDatabase();
    }

    // 2. RelicData 임포트 (모델 경로 파싱 추가됨)
    [MenuItem("MyTools/Import Data/2. Import RelicData (CSV)")]
    public static void ImportRelicData()
    {
        string path = EditorUtility.OpenFilePanel("Import Relic CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        Directory.CreateDirectory(RELIC_DATA_PATH);

        string[] allLines = File.ReadAllLines(path, System.Text.Encoding.UTF8);
        if (allLines.Length <= 1) return;

        Debug.Log($"[RelicParser] {allLines.Length - 1}개 데이터 임포트 시작...");

        for (int i = 1; i < allLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(allLines[i])) continue;

            string[] row = SplitCSVLine(allLines[i]);

            // 최소 ID, Name은 있어야 함
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

            // [기본 정보 매핑]
            relic.itemID = itemID;
            relic.itemName = (row.Length > 1) ? row[1].Trim() : "";

            // ItemType
            string typeStr = (row.Length > 2) ? row[2].Trim() : "Etc";
            if (System.Enum.TryParse(typeStr, true, out ItemType parsedType))
                relic.itemTypeEnum = parsedType;
            else
                relic.itemTypeEnum = ItemType.Etc;

            // EquipmentSlot
            string slotStr = (row.Length > 3) ? row[3].Trim() : "None";
            if (System.Enum.TryParse(slotStr, true, out EquipmentSlot parsedSlot))
                relic.equipmentSlot = parsedSlot;
            else
                relic.equipmentSlot = EquipmentSlot.None;

            // Grade, Desc, Icon
            relic.grade = (row.Length > 4) ? row[4].Trim() : "Common";
            string rawDesc = (row.Length > 5) ? row[5].Trim() : "";
            relic.description = rawDesc.Replace("\\n", "\n"); // 줄바꿈 처리
            relic.iconPath = (row.Length > 6) ? row[6].Trim() : "";

            // Ability 연결
            string abilityIDString = (row.Length > 7) ? row[7].Trim() : "";
            if (!string.IsNullOrEmpty(abilityIDString) && abilityIDString != "0")
            {
                string abilityAssetPath = $"{ABILITY_DATA_PATH}/{abilityIDString}.asset";
                AbilityData abilityAsset = AssetDatabase.LoadAssetAtPath<AbilityData>(abilityAssetPath);
                relic.grantedAbility = abilityAsset;
            }
            else
            {
                relic.grantedAbility = null;
            }

            // Stack, Price
            string stackStr = (row.Length > 8) ? row[8].Trim() : "1";
            int.TryParse(stackStr, out relic.maxStack);

            string priceStr = (row.Length > 9) ? row[9].Trim() : "0";
            int.TryParse(priceStr, out relic.price);

            // [★신규] Model Path 파싱 (11번째 컬럼)
            string modelPath = (row.Length > 10) ? row[10].Trim() : "None";
            relic.modelPath = modelPath;

            // 3D 모델 프리팹 자동 연결
            if ((relic.itemTypeEnum == ItemType.Equipment || relic.itemTypeEnum == ItemType.Weapon) &&
                !string.IsNullOrEmpty(modelPath) && modelPath != "None")
            {
                string fullPrefabPath = $"{RESOURCE_PREFAB_PATH}{modelPath}.prefab";
                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPrefabPath);

                if (modelPrefab != null)
                {
                    relic.modelPrefab = modelPrefab;
                }
                else
                {
                    Debug.LogWarning($"[RelicParser] 모델을 찾을 수 없음: {fullPrefabPath} (Item: {itemID})");
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
        Debug.Log("[RelicParser] 임포트 완료");
        UpdateMasterDatabase();
    }

    // 3. MasterDB 업데이트 (기존 기능 유지)
    [MenuItem("MyTools/Import Data/3. Update Master Database")]
    public static void UpdateMasterDatabase()
    {
        Debug.Log("[MasterDB] 업데이트 시작...");

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
            string path = AssetDatabase.GUIDToAssetPath(guid);
            db.allAbilities.Add(AssetDatabase.LoadAssetAtPath<AbilityData>(path));
        }

        string[] relicGUIDs = AssetDatabase.FindAssets("t:RelicData", new[] { RELIC_DATA_PATH });
        foreach (string guid in relicGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            db.allRelics.Add(AssetDatabase.LoadAssetAtPath<RelicData>(path));
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[MasterDB] 완료: Abilities({db.allAbilities.Count}), Relics({db.allRelics.Count})");
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
}