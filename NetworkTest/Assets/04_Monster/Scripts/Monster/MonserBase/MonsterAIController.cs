using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 이제 모든 필수 컴포넌트를 명시합니다.
[RequireComponent(typeof(IMonsterMovement), typeof(MonsterHealth), typeof(MonsterSensor))]
public class MonsterAIController : MonoBehaviour
{
    #region 필드
    // --- 주요 컴포넌트 참조 ---
    private IMonsterMovement movement;
    private MonsterHealth health;
    public MonsterSensor sensor { get; private set; }

    private NavMeshAgent agent;

    private Animator animator;
    [Header("몬스터 설정")]
    public MonsterConfig config;

    [Tooltip("투사체가 발사될 위치 (예: 몬스터의 입, 손) 현재는 식물 형태 몬스터만 사용")]
    public Transform firePoint;

    [Header("애니메이션 설정")]
    public MonsterAnimationConfig animConfig;

    public MonsterFSM fsm { get; private set; }

    public GameObject player { get; private set; }

    [Header("디버그 설정")]
    public bool alwaysShowGizmos = false;
    // --- 상태 머신 (FSM) ---
    public ZombieBaseState<MonsterAIController> CurrentState { get; private set; }


    // <<<< 2. 도착 여부 판정 로직 수정 >>>>
    // NavMeshAgent가 있으면 그 상태를 사용하고, 없으면(거미) 기존처럼 거리 기반으로 판단
    public bool arrivedAtDestination
    {
        get
        {
            if (agent != null) // 좀비의 경우
            {
                // 경로 계산이 끝나고, 남은 거리가 정지 거리보다 작으면 도착한 것으로 간주
                return !agent.pathPending && agent.remainingDistance <= config.stoppingDistance;
            }
            else // 거미의 경우
            {
                return Vector3.Distance(transform.position, currentDestination) < config.stoppingDistance;
            }
        }
    }

    public Vector3 currentDestination { get; private set; }

    // --- 코루틴 참조 ---
    public Coroutine attackRoutineCor { get; set; }

    // --- 애니메이션 해시 ---
    public int hashMoveSpeed { get; private set; }
    public int hashIsWalking { get; private set; }
    public int hashIsRunning { get; private set; }
    public int hashAttack1 { get; private set; }
    public int hashAttack2 { get; private set; }
    public int hashAttack3 { get; private set; }
    public int hashAttack4 { get; private set; }
    public int hashBlockStart { get; private set; } 
    public int hashBlockEnd { get; private set; } 
    public int hashHit { get; private set; }
    public int hashHit2 { get; private set; }
    public int hashDie { get; private set; }
    public int hashDie2 { get; private set; }
    public int hashLookAround { get; private set; }
    public int hashTaunt { get; private set; }
    public int hashIdleType { get; private set; }
    #endregion

    #region 초기화 및 루프
    void Awake()
    {
        movement = GetComponent<IMonsterMovement>();
        health = GetComponent<MonsterHealth>();
        sensor = GetComponent<MonsterSensor>();
        animator = GetComponentInChildren<Animator>();
        fsm = GetComponent<MonsterFSM>();
        player = GameObject.FindGameObjectWithTag("Player");


        if (animConfig == null)
        {
            Debug.LogError(gameObject.name + "에 MonsterAnimationConfig 파일이 할당 안됨");
            return;
        }

        TryGetComponent<NavMeshAgent>(out agent);

        if (config == null)
        {
            Debug.LogError(gameObject.name + "에 MonsterConfig 파일이 할당 안됨");
            return;
        }

        if (fsm == null)
        {
            Debug.LogError(gameObject.name + "에 MonsterFSM ('게임팩') 컴포넌트가 없습니다! GolemFSM, GazerFSM 등을 추가해주세요.", this);
            return; // Start() 함수가 실행되지 않도록 중단
        }

        // 3. Health 컴포넌트에 Config 값을 넘겨 초기화시킵니다.
        InitializeAnimationHashes();
        health.Initialize(config);
    }

    void Start()
    {
        health.OnHit.AddListener(HandleHit);
        health.OnDeath.AddListener(HandleDeath);
        health.OnBlock.AddListener(HandleBlock);
        ChangeState(fsm.IdleState);
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.OnHit.RemoveListener(HandleHit);
            health.OnDeath.RemoveListener(HandleDeath);
        }
    }

    void Update()
    {
        if (CurrentState == null || health.IsDead) return;
        ZombieBaseState<MonsterAIController> nextState = CurrentState.UpdateState(this);

        if (nextState != CurrentState) { ChangeState(nextState); }
    }

    private void InitializeAnimationHashes()
    {
        hashIdleType = Animator.StringToHash(animConfig.idleTypeInt);
        hashMoveSpeed = Animator.StringToHash(animConfig.moveSpeedFloat);
        hashIsWalking = Animator.StringToHash(animConfig.isWalkingBool);
        hashIsRunning = Animator.StringToHash(animConfig.isRunningBool);
        hashAttack1 = Animator.StringToHash(animConfig.attackTrigger1);
        hashAttack2 = Animator.StringToHash(animConfig.attackTrigger2);
        hashAttack3 = Animator.StringToHash(animConfig.attackTrigger3);
        hashAttack4 = Animator.StringToHash(animConfig.attackTrigger4);
        hashHit = Animator.StringToHash(animConfig.hitTrigger);
        hashHit2 = Animator.StringToHash(animConfig.hitTrigger2);
        hashDie = Animator.StringToHash(animConfig.dieTrigger);
        hashDie2 = Animator.StringToHash(animConfig.dieTrigger2);
        hashLookAround = Animator.StringToHash(animConfig.lookAroundTrigger);
        hashTaunt = Animator.StringToHash(animConfig.tauntTrigger);
        hashBlockStart = Animator.StringToHash(animConfig.blockStartTrigger);
        hashBlockEnd = Animator.StringToHash(animConfig.blockEndTrigger);
    }
    #endregion

    #region 상태 관리
    public void ChangeState(ZombieBaseState<MonsterAIController> newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);


    }
    #endregion


    #region 이동 제어
    public void MoveTo(Vector3 destination)
    {
        currentDestination = destination;
        float speed = (CurrentState == fsm.TraceState) ? config.runSpeed : config.walkSpeed;
        // 최종 목적지를 이동 시스템에 전달합니다.
        movement.Move(destination, speed);

        // NavMeshAgent가 없는 몬스터(거미)는 수동으로 회전시켜 줍니다.
        if (agent == null)
        {
            movement.TurnTowards(destination, config.turnSpeed);
        }
    }

    public void StopMoving() { movement.Stop();  }
    public void LookAt(Vector3 target) { movement.TurnTowards(target, config.turnSpeed); }
    public void LookAt(Vector3 target, float customTurnSpeed)
    {
        movement.TurnTowards(target, customTurnSpeed);
    }
    public Vector3 GetRandomPatrolDestination()
    {
        float distance = Random.Range(config.patrolRadiusMin, config.patrolRadiusMax);
        Vector3 randomDir = Random.onUnitSphere * distance;
        randomDir.y = 0;
        Vector3 destination = transform.position + randomDir;
        if (Physics.Raycast(destination + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f)) { return hit.point; }
        return destination;
    }
    #endregion

    #region 감지, 공격, 이벤트 핸들러

    public bool CanSeePlayer => sensor.CanSeePlayer;
    public Vector3 targetLastPos => sensor.TargetLastPosition;

    public float GetDistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;

        Vector3 monsterPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPos = new Vector3(player.transform.position.x, 0, player.transform.position.z);
        return Vector3.Distance(monsterPos, playerPos);
    }
    public IEnumerator AttackRoutine() { WaitForSeconds attackCooldown = new WaitForSeconds(config.attackCooldown); while (GetDistanceToPlayer() <= config.attackRange) { Debug.Log("몬스터 공격!"); yield return attackCooldown; } }
    public void StopAttackRoutine() { if (attackRoutineCor != null) { StopCoroutine(attackRoutineCor); attackRoutineCor = null; } }
    // MonsterAIController.cs -> HandleHit (만약 1대만 맞아도 Block 하길 원한다면)

    private void HandleHit()
    {
        if (health.IsDead) return;

        if (fsm is GazerFSM gazerFSM)
        {
            // ★ (신규) 1-1. 플레이어를 감지 못했을 때(Idle/Patrol) 맞았는가?
            // (요청사항 1: 감지 안됐는데 맞으면)
            if (CurrentState == fsm.IdleState || CurrentState == fsm.PatrolState)
            {
                Debug.Log("GAZER HIT: (Idle/Patrol) 중 피격! 강제 감지 및 추적 시작.");
                if (player != null)
                {
                    sensor.ForceDetection(player.transform.position); // (요청: 쫒아오기)
                }
                SetAnimTrigger(hashHit);     // (요청: hit애니메이션)
                ChangeState(fsm.HitState); // Hit 상태로 전환 (이후 Trace로 감)
                return; // (중요) 기존 HP 임계점 로직을 스킵
            }

            // ★ (기존) 1-2. (Trace/Attack 등) 전투 중에 맞았는가?
            // (기존 HP 임계점 로직)
            if (gazerFSM.IsHitOnCooldown)
            {
                Debug.Log("Gazer Hit: 쿨다운 중... 경직 무시.");
                return;
            }
            float hpPercent = health.CurrentHP / health._maxHP;
            bool thresholdCrossed = gazerFSM.CheckAndTriggerThreshold(hpPercent);

            if (thresholdCrossed)
            {
                if (!sensor.CanSeePlayer && player != null)
                {
                    sensor.ForceDetection(player.transform.position);
                }
                if (Random.value > 0.5f)
                    SetAnimTrigger(hashHit);
                else
                    SetAnimTrigger(hashHit2);
                ChangeState(fsm.HitState);
            }
            else
            {
                Debug.Log("Gazer Hit: HP 임계점이 아니므로 경직 무시.");
            }
        }
        // ★ 2. (수정) GOLEM 피격 로직 ★
        else if (fsm is GolemFSM golemFSM)
        {
            // --- (우선순위 1: Block 중) ---
            // (요구사항 4: Block 중인가?)
            if (CurrentState == fsm.BlockState)
            {
                var blockState = CurrentState as GolemStates.Block;

                // (요구사항 3: Block이 풀리는 1초의 취약한 타이밍인가?)
                if (blockState != null && blockState.CurrentPhase == GolemStates.Block.Phase.VulnerableCheck)
                {
                    Debug.Log("GOLEM HIT: 취약(Vulnerable) 상태에서 피격! HitState 전환.");
                    SetAnimTrigger(hashHit); // Hit 애니메이션 재생
                    ChangeState(fsm.HitState); // Hit 상태(속도저하)로 전환
                }
                else
                {
                    // 'Blocking' 단계이므로 모든 데미지 무시 (Hit 애니메이션 없음)
                    Debug.Log("GOLEM HIT: 방어(Blocking) 중! 피격 무시.");
                }
                return; // 방어 중이므로 아래 로직 실행 안 함
            }

            // --- (우선순위 2: Block 발동 직전) ---
            // (요구사항 4: Block 해야 해!)
            // OnBlock 이벤트가 OnHit보다 늦게 오므로, Health의 카운터를 직접 체크
            if (health.hitCounter >= health.blockTriggerHits)
            {
                Debug.Log("GOLEM HIT: Block 발동 조건 충족! Hit 애니메이션 무시.");
                // 곧 HandleBlock이 호출되어 BlockState로 바꿀 것이므로 HitState로 가지 않음
                // (속도 저하도 없음)
                return;
            }

            // --- (우선순위 3: 원거리 피격) ---
            // (요구사항 2, 5: 멀리서 쏘면 한번만)
            float distance = GetDistanceToPlayer();
            // (예: 공격 사거리의 2배보다 멀고, 아직 원거리 Hit 애니를 안했을 때)
            if (distance > (config.attackRange * 2) && !golemFSM.HasPlayedRangedHitAnim)
            {
                Debug.Log("GOLEM HIT: 원거리 피격! HitState 전환 (애니메이션 포함).");
                golemFSM.SetRangedHitAnimPlayed(); // 플래그 설정 (다시 안하게)
                SetAnimTrigger(hashHit); // Hit 애니메이션 재생
                ChangeState(fsm.HitState); // Hit 상태(속도저하)로 전환
                return;
            }

            // --- (우선순위 4: 그 외 모든 피격) ---
            // (요구사항 1: 그냥 속도만 느리게)
            // (예: 가까이서 맞았을 때, 또는 원거리에서 두 번째 이상 맞았을 때)
            Debug.Log("GOLEM HIT: 일반 피격. HitState 전환 (애니메이션 없음).");
            // SetAnimTrigger(hashHit) 호출 안 함
            ChangeState(fsm.HitState); // Hit 상태(속도저하)로만 전환
        }

        else if (fsm is MinotaurFSM)
        {
            // Minotaur는 애니메이션(SetAnimTrigger)을 재생하지 않고
            // HitState(속도 저하)로만 즉시 전환합니다.
            Debug.Log("MINOTAUR HIT: HitState 전환 (애니메이션 없음).");
            ChangeState(fsm.HitState); //
        }
        // 4. 그 외 몬스터 (좀비, 슬라임 등)
        else
        {
            // 기존 로직 (애니메이션 재생 + HitState)
            Debug.Log("DEFAULT HIT: HitState 전환 (애니메이션 포함).");
            SetAnimTrigger(hashHit);
            ChangeState(fsm.HitState);
        }
    }
    private void HandleBlock()
    {
        if (health.IsDead) return;

        // 1. GolemFSM인지 확인
        var golemFSM = fsm as GolemFSM;

        // 2. GolemFSM이 아니면 (예: 좀비, 슬라임) 방어/반격 안 함
        if (golemFSM == null)
        {
            return;
        }

        if (CurrentState == fsm.BlockState)
        {
            return;
        }

        // 4. 10초 쿨다운이 돌고 있으면 무시
        if (golemFSM.IsBlockOnCooldown)
        {
            Debug.Log("방어 쿨다운 중... 무시!");
            return;
        }

        // 5. Golem이 맞고 쿨다운도 아니므로 Block 상태로 전환
        ChangeState(fsm.BlockState);
    }
    private void HandleDeath()
    {
        StopAllCoroutines();
        if (string.IsNullOrEmpty(animConfig.dieTrigger2))
        {
            // 1. DieTrigger2가 비어있는 경우 (Gazer 등 Death 애니메이션이 1개인 몬스터)
            //    config에 설정된 첫 번째 dieTrigger (hashDie)만 실행합니다.
            SetAnimTrigger(hashDie);
        }
        else
        {
            // 2. DieTrigger2가 설정되어 있는 경우 (Death 애니메이션이 2개 이상인 몬스터)
            //    기존처럼 50% 확률로 랜덤 실행합니다.
            if (Random.value > 0.5f)
                SetAnimTrigger(hashDie); // "Death1"
            else
                SetAnimTrigger(hashDie2); // "Death2"
        }
        ChangeState(fsm.DieState);
    }

    #endregion


    #region 애니메이션

    /// <summary>
    /// 애니메이터의 'Bool' 파라미터를 설정합니다.
    /// </summary>
    public void SetAnimBool(int animHash, bool value)
    {
        if (animator == null) return;
        animator.SetBool(animHash, value);
    }

    /// <summary>
    /// 애니메이터의 'Float' 파라미터를 설정합니다.
    /// </summary>
    public void SetAnimFloat(int animHash, float value)
    {
        if (animator == null) return;
        animator.SetFloat(animHash, value);
    }

    /// <summary>
    /// 애니메이터의 'Trigger' 파라미터를 발동시킵니다.
    /// (기존 SetTrigger 함수와 동일, 이름만 변경)
    /// </summary>
    public void SetAnimTrigger(int animHash)
    {
        if (animator != null)
        {
            animator.SetTrigger(animHash);
        }
    }

    public void SetAnimInt(int animHash, int value)
    {
        if (animator == null) return;
        animator.SetInteger(animHash, value);
    }

    #endregion

    /// <summary>
    /// 몬스터의 절차적 움직임(IK)을 켜거나 끕니다. 거미에게만 해당됩니다.
    /// </summary>
    public void SetProceduralMovement(bool isActive)
    {
        // Spider 컴포넌트가 있는지 확인
        if (TryGetComponent<Spider>(out var spiderBody))
        {
            spiderBody.enabled = isActive;
        }
        // IKStepManager 컴포넌트가 있는지 확인
        if (TryGetComponent<IKStepManager>(out var spiderStepManager))
        {
            spiderStepManager.enabled = isActive;
        }
    }

    /// <summary>
    /// (신규) Attack 상태에서 호출되어 플레이어에게 데미지를 적용합니다.
    /// </summary>
    public void ApplyDamageToPlayer()
    {
        if (player == null || health.IsDead) return;

        // 1. 공격 딜레이(attackDelay) 후에도 플레이어가 사거리 안에 있는지 다시 체크
        if (GetDistanceToPlayer() <= config.attackRange)
        {
            // 2. ★ (수정) 'PlayerHealth' -> 'PlayerStats'로 변경 ★
            if (player.TryGetComponent<PlayerStats>(out PlayerStats playerStats))
            {
                Debug.Log($"[Golem] 플레이어 공격! 데미지: {config.attackDamage}");
                playerStats.TakeDamage(config.attackDamage);
            }
            else
            {
                Debug.LogWarning($"[Golem] 플레이어({player.name})에게 'PlayerStats' 스크립트가 없습니다!");
            }
        }
        else
        {
            Debug.Log("[Golem] 플레이어가 사거리를 벗어나서 공격이 빗나갔습니다.");
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // config 파일이 없으면 아무것도 그리지 않습니다.
        if (config == null) return;

        // 1. 공격 범위 (Attack Range)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.attackRange);

        // 2. 멈추는 거리 (Stopping Distance)
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, config.stoppingDistance);

        // 3. 소리 감지 범위 (Sound Range)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.soundRange);

        // 4. 현재 목적지 (실행 중에만 표시)
        if (Application.isPlaying && fsm != null && (CurrentState == fsm.PatrolState || CurrentState == fsm.TraceState))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(currentDestination, 0.5f);
            Gizmos.DrawLine(transform.position, currentDestination);
        }
    }
#endif

}



