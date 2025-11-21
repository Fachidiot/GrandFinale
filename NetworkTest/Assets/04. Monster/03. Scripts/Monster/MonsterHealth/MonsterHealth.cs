// MonsterHealth.cs
using UnityEngine;
using UnityEngine.Events;

public class MonsterHealth : MonoBehaviour
{
    #region 필드
    [Header("아이템 드랍 관련")]
    [Tooltip("몬스터가 사망했을 때 생성할 'GenericLootDrop' 프리팹")]
    [SerializeField] private GameObject genericLootPrefab;

    private MonsterConfig _config;

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
        _config = config;
        _maxHP = config.maxHP;
        _defense = config.defense;
        currentHP = _maxHP;
        IsDead = false;
    }

    /// <summary>
    /// 외부로부터 데미지를 받는 함수입니다.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        float actualDamage = 0f;
        float currentDefense = 0f; // 기본 방어력 0

        // --- 골렘 1. (수정) 골렘 방어 상태인지 체크 ---
        if (ai != null && ai.fsm is GolemFSM && ai.CurrentState == ai.fsm.BlockState)
        {
            // Block 상태일때, 현재 '페이즈'를 가져옵니다.
            var blockState = ai.CurrentState as GolemStates.Block;
            if (blockState != null && blockState.CurrentPhase == GolemStates.Block.Phase.Blocking)
            {
                // "방어 중" 페이즈일 때만 몬스터의 방어력(_defense)을 사용합니다.
                currentDefense = _defense;
                Debug.Log("GOLEM BLOCK: 방어 성공! 데미지 " + currentDefense + " 감소.");
            }

        }

        // 2. 최종 데미지 계산 (방어력 적용)
        actualDamage = Mathf.Max(damage - currentDefense, 0f);
        currentHP -= actualDamage;

        // (상세한 로그 출력)
        Debug.Log($"<color=orange>[{gameObject.name}] 피격! (들어온 데미지: {damage}, 현재 방어력: {currentDefense}, 실제 데미지: {actualDamage}) -> 남은 체력: {currentHP}/{_maxHP}</color>");

        if (currentHP <= 0)
        {
            currentHP = 0;
            IsDead = true;
            OnDeath?.Invoke();

            GiveExpToPlayer();

            Debug.Log("<color=red>사망 신호 발생!</color>");
        }
        else
        {
            // (사망하지 않았다면 피격 신호 발생)
            OnHit?.Invoke();
            Debug.Log("<color=yellow>피격 신호 발생!</color>");

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

                if (hitCounter >= blockTriggerHits)
                {
                    Debug.Log($"[{gameObject.name}] 방어/반격 조건 충족!");
                    OnBlock?.Invoke();
                    hitTimer = 0;
                    hitCounter = 0;
                    return;
                }
            }
        }
    }

    private void GiveExpToPlayer()
    {
        if (_config == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent<PlayerStats>(out var playerStats))
        {
            playerStats.GainExp(_config.experienceReward);
        }
    }

    public void SpawnLoot(LootTable lootTable)
    {
        if (lootTable == null || lootTable.items == null)
        {
            Debug.LogWarning("LootTable이 비어있습니다.", this);
            return;
        }
        if (genericLootPrefab == null)
        {
            Debug.LogError($"[아이템 드랍] {gameObject.name}의 genericLootPrefab이 설정되지 않았습니다!", this);
            return;
        }

        float groundY = transform.position.y; // 기본값은 현재 몬스터의 Y
        RaycastHit hit;


        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 100f))
        {
            // 레이가 충돌한 지점의 Y 좌표를 사용합니다.
            groundY = hit.point.y;
            Debug.Log($"[Raycast] 바닥 찾음: Y = {groundY}");
        }
        else
        {
            Debug.LogWarning("바닥을 찾지 못했습니다. 아이템이 공중에 뜰 수 있습니다.");
        }


        float scatterDistance = 1.0f; // 아이템이 흩뿌려질 반경

        foreach (var entry in lootTable.items)
        {
            if (entry.item == null) continue;

            if (Random.Range(0f, 100f) <= entry.dropChance)
            {
                GameObject prefabToSpawn = genericLootPrefab;

                // 2. 생성 위치를 랜덤하게 설정 (흩뿌리기)
                Vector2 randomCircle = Random.insideUnitCircle * scatterDistance;

                // 3. 생성 위치의 Y를 레이캐스트로 찾은 바닥(groundY)으로 설정합니다.
                Vector3 spawnPos = transform.position;
                spawnPos.x += randomCircle.x;
                spawnPos.z += randomCircle.y;

                // [바닥체크] Y 좌표를 찾은 바닥 + 오브젝트의 절반높이(0.5f)만큼 띄웁니다.
                spawnPos.y = groundY + 0.5f;

                Debug.Log($"<color=cyan>[LootSpawn] 아이템 생성: {entry.item.itemName}, Grade: {entry.item.grade}</color>");

                GameObject spawnedItem = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                Debug.Log($"<color=cyan>[LootSpawn] {entry.item.itemName} 생성! (생성 Y: {spawnPos.y}, 이름: {spawnedItem.name})</color>");


                // 4. 생성된 오브젝트에 아이템 데이터 설정
                ItemPickup pickupScript = spawnedItem.GetComponent<ItemPickup>();
                if (pickupScript != null)
                {
                    pickupScript.itemData = entry.item;
                    pickupScript.addToInventoryInstead = true;
                    Debug.Log($"<color=cyan>[LootSpawn] ItemPickup 데이터 설정 완료.</color>");
                }

                // 5. VFX 스크립트에 등급 전달 (VFX 색상 결정은 LootOrbVisuals가 담당)
                LootOrbVisuals visualScript = spawnedItem.GetComponent<LootOrbVisuals>();
                if (visualScript != null)
                {
                    visualScript.Initialize(entry.item.grade);
                    Debug.Log($"<color=cyan>[LootSpawn] LootOrbVisuals.Initialize('{entry.item.grade}') 호출 완료.</color>");
                }
                else
                {
                    Debug.LogError("[LootSpawn ERROR] GenericLootDrop 프리팹에 LootOrbVisuals.cs가 없습니다!");
                }
            }
        }
    }
}