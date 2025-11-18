using UnityEngine;

/// <summary>
/// 게임 전체의 몬스터 수를 관리하는 싱글톤 클래스입니다.
/// 씬에 단 하나만 존재해야 합니다.
/// </summary>
public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }

    [Header("글로벌 몬스터 설정")]
    [Tooltip("게임 전체에 존재할 수 있는 최대 몬스터 수")]
    public int globalMonsterLimit = 50;

    public int CurrentMonsterCount { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지할 경우
        }
    }

    /// <summary>
    /// 스포너가 몬스터 스폰을 '요청'할 때 사용
    /// </summary>
    public bool CanSpawnMonster()
    {
        return CurrentMonsterCount < globalMonsterLimit;
    }

    /// <summary>
    /// 스포너가 몬스터를 성공적으로 스폰했을 때 호출
    /// </summary>
    public void RegisterMonsterSpawned()
    {
        CurrentMonsterCount++;
        Debug.Log($"[MonsterManager] 몬스터 생성. 현재 총 몬스터: {CurrentMonsterCount}/{globalMonsterLimit}");
    }

    /// <summary>
    /// 몬스터가 죽을 때 MonsterHealth가 호출 (이건 나중에 추가)
    /// </summary>
    public void RegisterMonsterDied()
    {
        CurrentMonsterCount--;
        Debug.Log($"[MonsterManager] 몬스터 사망. 현재 총 몬스터: {CurrentMonsterCount}/{globalMonsterLimit}");
    }
}