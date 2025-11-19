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
            BuildDictionaries();
        }
        else
        {
            // 이미 씬에 DataManager가 있다면, 새로 생긴 것은 파괴합니다.
            Destroy(gameObject);
        }
    }

    private void BuildDictionaries()
    {
        if (masterDatabase == null)
        {
            Debug.LogError("[DataManager] MasterDatabase 에셋이 인스펙터에 연결되지 않았습니다!");
            return;
        }


        // 5-2. Resources.LoadAll 대신, masterDatabase의 리스트에서 직접 딕셔너리 생성
        AbilityDB = masterDatabase.allAbilities.ToDictionary(ability => ability.abilityID, ability => ability);
        RelicDB = masterDatabase.allRelics.ToDictionary(relic => relic.itemID, relic => relic);
    }

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