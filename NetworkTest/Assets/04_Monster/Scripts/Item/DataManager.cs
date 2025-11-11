// Assets/Scripts/Managers/DataManager.cs
using System.Collections.Generic;
using System.Linq; // Dictionary 변환(ToDictionary)을 위해 필요
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("데이터베이스 원본 에셋")]
    [SerializeField]
    private MasterDatabase masterDatabase; // (MasterDatabase.asset을 여기로 드래그)


    // ID(string)를 Key로, 실제 ScriptableObject(.asset)를 Value로 가집니다.
    public Dictionary<string, RelicData> RelicDB { get; private set; }
    public Dictionary<string, AbilityData> AbilityDB { get; private set; }


    void Awake()
    {
        // 3. 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            // 씬이 바뀌어도 이 매니저는 파괴되지 않고 유지됩니다.
            //DontDestroyOnLoad(gameObject);

            // ★★★ 4. LoadAllData -> BuildDictionaries 함수 호출로 변경 ★★★
            BuildDictionaries();
        }
        else
        {
            // 이미 씬에 DataManager가 있다면, 새로 생긴 것은 파괴합니다.
            Destroy(gameObject);
        }
    }

    // ★★★ 5. 함수 이름 및 로직 변경 ★★★
    private void BuildDictionaries()
    {
        // 5-1. MasterDatabase가 인스펙터에 연결되었는지 확인
        if (masterDatabase == null)
        {
            Debug.LogError("[DataManager] MasterDatabase 에셋이 인스펙터에 연결되지 않았습니다!");
            return;
        }

        Debug.Log("[DataManager] MasterDatabase로부터 딕셔너리 빌드 시작...");

        // 5-2. Resources.LoadAll 대신, masterDatabase의 리스트에서 직접 딕셔너리 생성
        AbilityDB = masterDatabase.allAbilities.ToDictionary(ability => ability.abilityID, ability => ability);
        RelicDB = masterDatabase.allRelics.ToDictionary(relic => relic.itemID, relic => relic);

        Debug.Log($"[DataManager] 로드 완료: Abilities({AbilityDB.Count}개), Relics({RelicDB.Count}개)");
    }

    // 7. (선택사항) 외부에서 데이터를 안전하게 가져오는 헬퍼 함수
    public AbilityData GetAbility(string abilityID)
    {
        if (AbilityDB.TryGetValue(abilityID, out AbilityData ability))
        {
            return ability;
        }
        Debug.LogWarning($"[DataManager] AbilityDB에서 ID를 찾을 수 없습니다: {abilityID}");
        return null;
    }
}