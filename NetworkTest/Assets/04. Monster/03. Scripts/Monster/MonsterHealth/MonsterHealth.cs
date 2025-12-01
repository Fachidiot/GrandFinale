// MonsterHealth.cs
using UnityEngine;
using UnityEngine.Events;

public class MonsterHealth : MonoBehaviour
{
    #region 필드
    // [Header("아이템 드랍 관련")]
    // [Tooltip("몬스터가 사망했을 때 참조할 LootTable")]
    // [SerializeField] private LootTable lootTable; // 이 필드는 OnDeath 이벤트에서 직접 파라미터로 받는 것이 더 유연합니다.

    public float _maxHP { get; private set; }
    private float _defense;

    [Space(10)]
    [Header("방어/반격 관련")]
    [Tooltip("이 시간(초) 안에")]
    public float blockTriggerTime = 2.0f;
    [Tooltip("이 횟수(번) 이상 피격 시")]
    public int blockTriggerHits = 5;
    [Tooltip("방어/반격 이벤트")]
    public UnityEvent OnBlock;

    public int hitCounter { get; private set; } = 0; // <-- 이렇게 초기화
    private float hitTimer = 0f;

    [Header("체력 상태 (실시간)")]
    [SerializeField]
    [Tooltip("현재 체력 (실시간 디버그용)")]
    private float currentHP;

    public float CurrentHP
    {
        get { return currentHP; }
        private set { currentHP = value; }
    }

    public bool IsDead { get; private set; }

    public UnityEvent OnHit;
    public UnityEvent OnDeath;

    // 골렘 1. (추가) AI 컨트롤러 참조
    private MonsterAIController ai;

    [Space(10)]
    [Header("--- DEBUG TOOLS ---")]
    public bool _DEBUG_ForceHit = false;
    public bool _DEBUG_ForceDie = false;
    #endregion

    private void Awake()
    {
        ai = GetComponent<MonsterAIController>();
    }

    private void Update()
    {

        if (_DEBUG_ForceDie) { }
        else if (_DEBUG_ForceHit) { }

        if (hitTimer > 0)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0)
            {
                hitCounter = 0;
            }
        }
    }

    public void Initialize(MonsterConfig config)
    {
        _maxHP = config.maxHP;
        _defense = config.defense;
        currentHP = _maxHP;
        IsDead = false;
        // Debug.Log($"[{gameObject.name}] Health 초기화 완료: HP={_maxHP}, DEF={_defense}");
    }

    /// <summary>
    /// (네트워크용) 호스트로부터 받은 데이터로 체력 상태를 강제 설정합니다.
    /// 이벤트는 발생시키지 않고, UI 업데이트 등을 위해 값만 동기화합니다.
    /// </summary>
    public void SetHealthFromNetwork(float newCurrentHP, float newMaxHP)
    {
        // 첫 초기화 이후 maxHP는 변하지 않는다고 가정
        if (_maxHP != newMaxHP)
        {
            _maxHP = newMaxHP;
        }

        // 값 변경이 있을 때만 업데이트
        if (currentHP != newCurrentHP)
        {
            currentHP = newCurrentHP;
        }

        // 사망 상태 동기화
        bool wasDead = IsDead;
        IsDead = currentHP <= 0;

        // 클라이언트에서 사망 상태가 처음 true가 되는 시점에 OnDeath 이벤트를 호출
        if (!wasDead && IsDead)
        {
            OnDeath?.Invoke();
            Debug.Log($"<color=cyan>[Network Sync] {gameObject.name} 사망 처리.</color>");
        }
    }

    /// <summary>
    /// 외부로부터 데미지를 받는 함수입니다.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        // [로그] 데미지 받기 전 상태 기록
        float previousHP = currentHP;

        float actualDamage = 0f;
        float currentDefense = 0f; // 기본 방어력 0

        // --- 골렘 1. 골렘 방어 상태인지 체크 ---
        if (ai != null && ai.fsm is GolemFSM && ai.CurrentState == ai.fsm.BlockState)
        {
            // Block 상태일때, 현재 '페이즈'를 가져옵니다.
            var blockState = ai.CurrentState as GolemStates.Block;
            if (blockState != null && blockState.CurrentPhase == GolemStates.Block.Phase.Blocking)
            {
                // "방어 중" 페이즈일 때만 몬스터의 방어력(_defense)을 사용합니다.
                currentDefense = _defense;
                Debug.Log($"[MonsterHealth] {gameObject.name} 방어 성공! (방어력 {_defense} 적용됨)");
            }
        }

        // 2. 최종 데미지 계산 (방어력 적용)
        actualDamage = Mathf.Max(damage - currentDefense, 0f);
        currentHP -= actualDamage;

        // 3. [로그] 데미지 연산 결과 상세 출력
        //    (들어온 데미지, 방어력, 실제 감소량, 남은 체력 등)
        Debug.Log($"<color=orange>[{gameObject.name} 피격 상세]</color>\n" +
                  $"1. 이전 체력 : {previousHP}\n" +
                  $"2. 입력 데미지 : {damage}\n" +
                  $"3. 방어력 적용 : -{currentDefense}\n" +
                  $"4. 최종 데미지 : {actualDamage}\n" +
                  $"5. 현재 체력 : {currentHP} / {_maxHP}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            IsDead = true;
            OnDeath?.Invoke();
            Debug.Log($"<color=red>[MonsterHealth] {gameObject.name} 사망!</color>");
        }
        else
        {
            // (사망하지 않았다면 피격 신호 발생)
            OnHit?.Invoke();

            // (골렘 방어 카운터 로직 - 방어력과 무관)
            if (ai != null && ai.fsm is GolemFSM)
            {
                if (hitTimer <= 0)
                {
                    hitTimer = blockTriggerTime;
                    hitCounter = 1;
                }
                else
                {
                    hitCounter++;
                }

                Debug.Log($"[MonsterHealth] {gameObject.name} 피격 카운트: {hitCounter}/{blockTriggerHits} (남은 시간: {hitTimer:F1}초)");

                if (hitCounter >= blockTriggerHits)
                {
                    Debug.Log($"<color=cyan>[{gameObject.name}] 반격 조건 충족! (Block Triggered)</color>");
                    OnBlock?.Invoke();
                    hitTimer = 0;
                    hitCounter = 0;
                    return;
                }
            }
        }
    }

    public void SpawnLoot(LootTable lootTable)
    {
        // 호스트가 아니면 아이템 드랍 로직을 실행하지 않음
        if (NetworkManager.Instance == null || NetworkManager.Instance.Mode != NetworkMode.Host) return;
        if (LootManager.Instance == null)
        {
            Debug.LogError("[MonsterHealth] LootManager 인스턴스가 없습니다!");
            return;
        }
        if (lootTable == null || lootTable.items == null)
        {
            Debug.LogWarning("LootTable이 비어있습니다.", this);
            return;
        }

        // 바닥 찾기
        float groundY = transform.position.y;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 100f))
        {
            groundY = hit.point.y;
        }

        float scatterDistance = 1.0f;

        foreach (var entry in lootTable.items)
        {
            if (entry.item == null) continue;

            if (Random.Range(0f, 100f) <= entry.dropChance)
            {
                // 생성 위치 계산
                Vector2 randomCircle = Random.insideUnitCircle * scatterDistance;
                Vector3 spawnPos = transform.position;
                spawnPos.x += randomCircle.x;
                spawnPos.z += randomCircle.y;
                spawnPos.y = groundY + 0.5f;

                // LootManager를 통해 아이템 생성 요청
                LootManager.Instance.SpawnLoot(entry.item.itemID, spawnPos);
            }
        }
    }
}