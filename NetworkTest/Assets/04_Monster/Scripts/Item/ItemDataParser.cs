// Assets/Scripts/Editor/ItemDataParser.cs
using UnityEngine;
using UnityEditor; // Editor 스크립트
using System.IO;   // 파일 읽기/쓰기
using System.Collections.Generic; // List

public class ItemDataParser
{
    // 데이터 에셋이 저장될 기본 경로
    private const string ABILITY_DATA_PATH = "Assets/Monster_v1/Data/Abilities";
    private const string RELIC_DATA_PATH = "Assets/Monster_v1/Data/Relics";

    private const string MASTER_DB_PATH = "Assets/Monster_v1/Data/MasterDatabase.asset";

    // 1. AbilityData 임포트 메뉴
    [MenuItem("MyTools/Import Data/1. Import AbilityData (CSV)")]
    public static void ImportAbilityData()
    {
        string path = EditorUtility.OpenFilePanel("Import Ability CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        // 대상 폴더가 없으면 생성
        Directory.CreateDirectory(ABILITY_DATA_PATH);

        string[] allLines = File.ReadAllLines(path);
        if (allLines.Length <= 1) return; // 헤더만 있으면 종료

        Debug.Log($"[AbilityParser] {allLines.Length - 1}개 데이터 임포트 시작...");

        // 첫 줄은 헤더이므로 1부터 시작
        for (int i = 1; i < allLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(allLines[i])) continue;

            string[] row = SplitCSVLine(allLines[i]);

            if (row.Length < 9)
            {
                Debug.LogWarning($"[AbilityParser] 줄 무시됨 (열 부족, 9개 미만): {allLines[i]}");
                continue;
            }

            string abilityID = row[0].Trim();
            if (string.IsNullOrEmpty(abilityID)) continue;

            // 1. 에셋 경로 지정 (예: ABIL_001.asset)
            string assetPath = $"{ABILITY_DATA_PATH}/{abilityID}.asset";

            // 2. 기존 에셋 로드 또는 신규 생성
            AbilityData ability = AssetDatabase.LoadAssetAtPath<AbilityData>(assetPath);
            if (ability == null)
            {
                // 파일이 없으면 새로 만듭니다.
                ability = ScriptableObject.CreateInstance<AbilityData>();
                AssetDatabase.CreateAsset(ability, assetPath);
            }

            // 3. CSV 데이터로 ScriptableObject 필드 채우기
            ability.abilityID = abilityID;
            ability.abilityName = row[1].Trim();
            ability.activationType = row[2].Trim();
            ability.abilityLogicID = row[3].Trim();
            ability.param_Key = row[4].Trim();
            ability.param_ValueA = row[5].Trim();
            ability.param_ValueB = row[6].Trim();
            ability.param_ValueC = row[7].Trim();
            ability.resourcePath = row[8].Trim();

            // 4. 변경 사항 저장
            EditorUtility.SetDirty(ability);

            // ★★★ (최적화) 루프 안에서 UpdateMasterDatabase() 호출 제거 ★★★
            // UpdateMasterDatabase(); // (제거)
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AbilityParser] 임포트 완료!");

        // ★★★ (최적화) 임포트가 모두 끝난 후 '한 번만' 호출 ★★★
        UpdateMasterDatabase();
    }

    // 2. RelicData 임포트 메뉴
    [MenuItem("MyTools/Import Data/2. Import RelicData (CSV)")]
    public static void ImportRelicData()
    {
        string path = EditorUtility.OpenFilePanel("Import Relic CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        Directory.CreateDirectory(RELIC_DATA_PATH);

        string[] allLines = File.ReadAllLines(path);
        if (allLines.Length <= 1) return;

        Debug.Log($"[RelicParser] {allLines.Length - 1}개 데이터 임포트 시작...");

        for (int i = 1; i < allLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(allLines[i])) continue;

            string[] row = SplitCSVLine(allLines[i]);
            if (row.Length < 7) continue;

            string itemID = row[0].Trim();
            if (string.IsNullOrEmpty(itemID)) continue;

            string assetPath = $"{RELIC_DATA_PATH}/{itemID}.asset";

            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(assetPath);
            if (relic == null)
            {
                relic = ScriptableObject.CreateInstance<RelicData>();
                AssetDatabase.CreateAsset(relic, assetPath);
            }

            // RelicData 필드 채우기
            relic.itemID = itemID;
            relic.itemName = row[1].Trim();
            relic.itemType = row[2].Trim();
            relic.grade = row[3].Trim();
            relic.description = row[4].Trim();
            relic.iconPath = row[5].Trim();

            // maxStack 파싱 (숫자 변환)
            int.TryParse(row[7].Trim(), out relic.maxStack);

            // ★★★ 핵심 연동 로직 ★★★
            string abilityIDString = row[6].Trim(); // 시트의 "ABIL_001"
            if (!string.IsNullOrEmpty(abilityIDString))
            {
                string abilityAssetPath = $"{ABILITY_DATA_PATH}/{abilityIDString}.asset";
                AbilityData abilityAsset = AssetDatabase.LoadAssetAtPath<AbilityData>(abilityAssetPath);

                if (abilityAsset != null)
                {
                    relic.grantedAbility = abilityAsset;
                }
                else
                {
                    Debug.LogWarning($"[RelicParser] Ability 에셋을 찾을 수 없습니다: '{abilityIDString}' (Relic: '{itemID}')");
                    relic.grantedAbility = null;
                }
            }
            else
            {
                relic.grantedAbility = null; // 연결된 능력이 없음
            }

            EditorUtility.SetDirty(relic);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[RelicParser] 임포트 완료!");

        // ★★★ (버그 수정) Relic 임포트 후에도 DB 업데이트 호출 ★★★
        UpdateMasterDatabase();
    }


    [MenuItem("MyTools/Import Data/3. Update Master Database")]
    public static void UpdateMasterDatabase()
    {
        Debug.Log("[MasterDB] 마스터 데이터베이스 업데이트 시작...");

        // 1. MasterDatabase.asset 찾기 (없으면 생성)
        MasterDatabase db = AssetDatabase.LoadAssetAtPath<MasterDatabase>(MASTER_DB_PATH);
        if (db == null)
        {
            Debug.Log("[MasterDB] MasterDatabase.asset을 새로 생성합니다.");
            db = ScriptableObject.CreateInstance<MasterDatabase>();
            AssetDatabase.CreateAsset(db, MASTER_DB_PATH);
        }

        // ★★★ (오류 수정) 리스트가 null일 경우 새로 생성 ★★★
        if (db.allAbilities == null)
        {
            db.allAbilities = new List<AbilityData>();
        }
        if (db.allRelics == null)
        {
            db.allRelics = new List<RelicData>();
        }

        // 2. 기존 리스트 초기화 (이제 안전함)
        db.allAbilities.Clear();
        db.allRelics.Clear();

        // 3. Abilities 폴더의 모든 AbilityData.asset 파일 찾기
        string[] abilityGUIDs = AssetDatabase.FindAssets("t:AbilityData", new[] { ABILITY_DATA_PATH });
        foreach (string guid in abilityGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            db.allAbilities.Add(AssetDatabase.LoadAssetAtPath<AbilityData>(path));
        }

        // 4. Relics 폴더의 모든 RelicData.asset 파일 찾기
        string[] relicGUIDs = AssetDatabase.FindAssets("t:RelicData", new[] { RELIC_DATA_PATH });
        foreach (string guid in relicGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            db.allRelics.Add(AssetDatabase.LoadAssetAtPath<RelicData>(path));
        }

        // 5. 변경사항 저장
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[MasterDB] 업데이트 완료: Abilities({db.allAbilities.Count}), Relics({db.allRelics.Count})");
    }

    // 제공해주신 CSV 파싱 유틸리티 (따옴표(")로 묶인 콤마 무시)
    private static string[] SplitCSVLine(string line)
    {
        List<string> result = new();
        bool inQuotes = false;
        string current = "";

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Replace("\"", "").Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current.Replace("\"", "").Trim());
        return result.ToArray();
    }
}