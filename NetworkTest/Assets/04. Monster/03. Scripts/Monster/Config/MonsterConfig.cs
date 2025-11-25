using UnityEngine;

/// <summary>
/// 모든 몬스터가 공통으로 사용할 최종 설정 파일입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewMonsterConfig", menuName = "Monster/Monster Config")]
public class MonsterConfig : ScriptableObject
{
    [Header("기본 능력치")]
    public float maxHP = 100f;
    public float attackDamage = 10f;
    public float defense = 0f;

    [Header("AI 행동 및 감지")]
    public float fovRange = 10f;
    [Range(0, 360)]
    public float fovAngle = 120f;
    public float soundRange = 15f;
    public float attackRange = 2f;
    public float stoppingDistance = 1.5f;
    public float persistenceTime = 5f;

    [Header("순찰 및 대기")]
    public float patrolRadiusMin = 5f;
    public float patrolRadiusMax = 10f;
    public float idleTimeMin = 10f;
    public float idleTimeMax = 15f;

    [Header("주변 둘러보기")]
    public float lookAroundTime = 3f;
    public float lookAroundTurnInterval = 1.5f;

    [Header("이동 관련")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3f;
    public float turnSpeed = 2000f;

    [Header("공격 패턴")]
    public float attackDelay = 0.5f;
    public float attackCooldown = 10f;

    [Header("--- 자동 사운드 연결 (폴더 기반) ---")]
    [Tooltip("Resources/Sound/MonsterSound/ 아래에 있는 폴더 이름 (예: Slime)")]
    public string monsterFolderName;


    [Header("사운드 및 이펙트")]
    public AudioClip idleSound;
    public AudioClip chaseSound;
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip dieSound;
    public GameObject hitEffect;
    public GameObject dieEffect;

    [Header("경험치 설정")]
    [Tooltip("이 몬스터를 처치했을 때 플레이어가 얻는 경험치")]
    public int experienceReward = 10;

    [Header("아이템")]
    public LootTable lootTable;

    [Header("사망 후 처리")]
    [Tooltip("몬스터가 죽은 후 시체가 사라지기까지 걸리는 시간 (초)")]
    public float corpseDestroyDelay = 5.0f; // 5초 뒤 사라짐

    [ContextMenu("Auto Bind Sounds (Smart Search)")]
    public void AutoBindSoundsSmart()
    {
        if (string.IsNullOrEmpty(monsterFolderName))
        {
            Debug.LogError($"[{name}] 폴더 이름을 입력해주세요! (예: Slime)");
            return;
        }

        string path = $"Sound/MonsterSound/{monsterFolderName}";
        // 해당 폴더의 모든 오디오 클립을 가져옵니다.
        AudioClip[] allClips = Resources.LoadAll<AudioClip>(path);

        if (allClips.Length == 0)
        {
            Debug.LogError($"[{name}] '{path}' 경로에 오디오 파일이 없습니다! 폴더 위치를 확인해주세요.");
            return;
        }


        foreach (var clip in allClips)
        {
            string clipName = clip.name.ToLower(); // 소문자로 변환해서 비교

            if (clipName.Contains("idle")) idleSound = clip;
            else if (clipName.Contains("walk") || clipName.Contains("run") || clipName.Contains("move") || clipName.Contains("chase")) chaseSound = clip;
            else if (clipName.Contains("attack") || clipName.Contains("hit_give")) attackSound = clip;
            else if (clipName.Contains("hit") || clipName.Contains("damage") || clipName.Contains("hurt")) hitSound = clip;
            else if (clipName.Contains("die") || clipName.Contains("dead") || clipName.Contains("death")) dieSound = clip;
        }

        // 결과 로그
        Debug.Log($"============== [{name}] 사운드 자동 연결 결과 ==============");
        Debug.Log($" 폴더: {path} (발견된 파일: {allClips.Length}개)");
        Debug.Log($"🔹 Idle (대기): {(idleSound != null ? idleSound.name : "<color=red>못 찾음 (파일 이름에 'idle' 포함 필요)</color>")}");
        Debug.Log($"🔹 Chase (이동): {(chaseSound != null ? chaseSound.name : "<color=red>못 찾음 (파일 이름에 'walk', 'run', 'chase' 등 포함 필요)</color>")}");
        Debug.Log($"🔹 Attack (공격): {(attackSound != null ? attackSound.name : "<color=red>못 찾음 (파일 이름에 'attack' 포함 필요)</color>")}");
        Debug.Log($"🔹 Hit (피격): {(hitSound != null ? hitSound.name : "<color=red>못 찾음 (파일 이름에 'hit', 'damage' 포함 필요)</color>")}");
        Debug.Log($"🔹 Die (사망): {(dieSound != null ? dieSound.name : "<color=red>못 찾음 (파일 이름에 'die', 'dead' 포함 필요)</color>")}");
        Debug.Log("===========================================================");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}


