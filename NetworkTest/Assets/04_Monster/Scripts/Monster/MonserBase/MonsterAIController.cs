using System.Collections;
using UnityEngine;
using UnityEngine.AI;

//   몬스터의 AI 로직을 담당합니다.
[RequireComponent(typeof(IMonsterMovement), typeof(MonsterHealth), typeof(MonsterSensor))]
public class MonsterAIController : MonoBehaviour
{
    #region 필드
    // --- 필수 컴포넌트 ---
    private IMonsterMovement movement;
    private MonsterHealth health;
    public MonsterSensor sensor { get; private set; }

    private NavMeshAgent agent;

    private Animator animator;
    [Header("설정")]
    public MonsterConfig config;

    [Tooltip("발사체 생성 위치 (예: 손, 입) 또는 이펙트의 시작점을 지정합니다")]
    public Transform firePoint;

    [Header("애니메이션")]
    public MonsterAnimationConfig animConfig;

    public MonsterFSM fsm { get; private set; }

    public GameObject player { get; private set; }

    [Header("디버그")]
    public bool alwaysShowGizmos = false;
    // --- 상태 머신 (FSM) ---
    public ZombieBaseState<MonsterAIController> CurrentState { get; private set; }


    // <<<< 2. 상태 확인용 프로퍼티 >>>>
    // NavMeshAgent 유무에 따라, (물리) 도착 여부 판단을 다르게 함
    public bool arrivedAtDestination
    {
        get
        {
            if (agent != null) // NavMeshAgent 사용 시
            {
                // 경로 계산이 끝나고, 남은 거리가 정지 거리보다 가까우면 도착
                return !agent.pathPending && agent.remainingDistance <= config.stoppingDistance;
            }
            else // 물리 기반 이동 시
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

    #region 초기화 & 생명주기
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
            Debug.LogError(gameObject.name + "의 MonsterAnimationConfig가 할당되지 않았습니다");
            return;
        }

        TryGetComponent<NavMeshAgent>(out agent);

        if (config == null)
        {
            Debug.LogError(gameObject.name + "의 MonsterConfig가 할당되지 않았습니다");
            return;
        }

        if (fsm == null)
        {
            Debug.LogError(gameObject.name + "에 MonsterFSM ('상속받은') 컴포넌트가 없습니다! GolemFSM, GazerFSM 등을 추가해주세요.", this);
            return; // Start() 함수에서 널 참조가 발생하는 것을 방지
        }

        // 3. Health 컴포넌트를 Config 파일과 함께 초기화시킵니다.
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

    #region 상태 변경
    public void ChangeState(ZombieBaseState<MonsterAIController> newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);


    }
    #endregion


    #region 이동 관련
    public void MoveTo(Vector3 destination)
    {
        currentDestination = destination;
        float speed = (CurrentState == fsm.TraceState) ? config.runSpeed : config.walkSpeed;
        // 이동 방식에 맞게 이동합니다.
        movement.Move(destination, speed);

        // NavMeshAgent가 아닐 때만 (물리 기반) 회전을 담당합니다.
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
    public IEnumerator AttackRoutine() { WaitForSeconds attackCooldown = new WaitForSeconds(config.attackCooldown); while (GetDistanceToPlayer() <= config.attackRange) { Debug.Log("공격!"); yield return attackCooldown; } }
    public void StopAttackRoutine() { if (attackRoutineCor != null) { StopCoroutine(attackRoutineCor); attackRoutineCor = null; } }
    // MonsterAIController.cs -> HandleHit (골렘만 맞았을때 Block 할지 판단한다면)

    private void HandleHit()
    {
        if (health.IsDead) return;

        if (fsm is GazerFSM gazerFSM)
        {
            // 가젤(원거리) 1-1. 플레이어를 못 본 상태(Idle/Patrol)에서 맞았다면?
            // (가정 1: 무조건 감지해야 함)
            if (CurrentState == fsm.IdleState || CurrentState == fsm.PatrolState)
            {
                Debug.Log("GAZER HIT: (Idle/Patrol) 피격! 대상을 강제로 감지합니다.");
                if (player != null)
                {
                    sensor.ForceDetection(player.transform.position); // (가정: 즉시 타겟으로)
                }
                SetAnimTrigger(hashHit);     // (가정: hit애니메이션)
                ChangeState(fsm.HitState); // Hit 상태로 전환 (곧 Trace 상태로)
                return; // (중요) 아래 HP 감소 로직이 실행되지 않음
            }

            // 가젤(원거리) 1-2. (Trace/Attack 등) 인지 중에 맞았다면?
            // ( HP 감소 로직 )
            if (gazerFSM.IsHitOnCooldown)
            {
                Debug.Log("Gazer Hit: 피격 쿨타임... 무시합니다.");
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
                Debug.Log("Gazer Hit: HP 감소량이 적어 무시합니다.");
            }
        }
        // 골렘 2. (근접) GOLEM 피격 로직
        else if (fsm is GolemFSM golemFSM)
        {
            // --- (시나리오 1: Block 상태 중 피격) ---
            // (조건부 4: Block 상태인가?)
            if (CurrentState == fsm.BlockState)
            {
                var blockState = CurrentState as GolemStates.Block;

                // (조건부 3: Block 상태의 '취약' 페이즈인가?)
                if (blockState != null && blockState.CurrentPhase == GolemStates.Block.Phase.VulnerableCheck)
                {
                    Debug.Log("GOLEM HIT: (Vulnerable) 약점 피격! HitState로 전환.");
                    SetAnimTrigger(hashHit); // Hit 애니메이션 재생
                    ChangeState(fsm.HitState); // Hit (경직) 상태로 전환
                }
                else
                {
                    // 'Blocking' 단계이므로 데미지 무시 (Hit 애니메이션 없음)
                    Debug.Log("GOLEM HIT: (Blocking) 방어! 피격 무시.");
                }
                return; // 방어 상태이므로 아래 로직 실행 안됨
            }

            // --- (시나리오 2: Block 유도 피격) ---
            // (조건부 4: Block 해야 해!)
            // OnBlock 이벤트가 OnHit보다 먼저 발생하므로, Health 카운터를 먼저 체크
            if (health.hitCounter >= health.blockTriggerHits)
            {
                Debug.Log("GOLEM HIT: Block 유도 피격! Hit 애니메이션 없음.");
                // 이미 HandleBlock이 호출되어 BlockState로 바뀔 예정이므로 HitState로 바꾸지 않음
                // (경직 없이 바로 방어)
                return;
            }

            // --- (시나리오 3: 원거리 피격) ---
            // (조건부 2, 5: 멀리서 한대 맞음)
            float distance = GetDistanceToPlayer();
            // (가정: 공격 거리 2배보다 멀고, 원거리 Hit 애니메이션을 재생한 적이 없다면)
            if (distance > (config.attackRange * 2) && !golemFSM.HasPlayedRangedHitAnim)
            {
                Debug.Log("GOLEM HIT: 원거리 피격! HitState로 전환 (애니메이션 재생).");
                golemFSM.SetRangedHitAnimPlayed(); // 한번만 재생 (다시 맞으면 안함)
                SetAnimTrigger(hashHit); // Hit 애니메이션 재생
                ChangeState(fsm.HitState); // Hit (경직) 상태로 전환
                return;
            }

            // --- (시나리오 4: 일반 근접 피격) ---
            // (조건부 1: 그냥 경직)
            // (가정: 가까이서 맞았거나, 또는 원거리에서 두번 이상 맞았을 때)
            Debug.Log("GOLEM HIT: 일반 피격. HitState로 전환 (애니메이션 없음).");
            // SetAnimTrigger(hashHit) 호출 없음
            ChangeState(fsm.HitState); // Hit (경직)만으로 전환
        }

        else if (fsm is MinotaurFSM)
        {
            // Minotaur는 애니메이션(SetAnimTrigger) 없이
            // HitState(경직 상태)로만 전환합니다.
            Debug.Log("MINOTAUR HIT: HitState로 전환 (애니메이션 없음).");
            ChangeState(fsm.HitState); //
        }
        // 4. 그 외 몬스터 (좀비, 거미 등)
        else
        {
            // 기본 동작 (애니메이션 재생 + HitState)
            Debug.Log("DEFAULT HIT: HitState로 전환 (애니메이션 재생).");
            SetAnimTrigger(hashHit);
            ChangeState(fsm.HitState);
        }
    }
    private void HandleBlock()
    {
        if (health.IsDead) return;

        // 1. GolemFSM 인지 확인
        var golemFSM = fsm as GolemFSM;

        // 2. GolemFSM이 아니라면 (예: 거미, 가젤) 즉시 리턴
        if (golemFSM == null)
        {
            return;
        }

        if (CurrentState == fsm.BlockState)
        {
            return;
        }

        // 4. 10초 쿨타임 체크
        if (golemFSM.IsBlockOnCooldown)
        {
            Debug.Log("방어 쿨타임... 무시!");
            return;
        }

        // 5. Golem이고 쿨타임도 아니므로 Block 상태로 전환
        ChangeState(fsm.BlockState);
    }
    private void HandleDeath()
    {
        StopAllCoroutines();
        if (string.IsNullOrEmpty(animConfig.dieTrigger2))
        {
            // 1. DieTrigger2 애니메이션이 없다면 (Gazer 등 Death 애니메이션 1개)
            //    config에 설정된 기본 dieTrigger (hashDie)만 실행합니다.
            SetAnimTrigger(hashDie);
        }
        else
        {
            // 2. DieTrigger2가 지정되어 있다면 (Death 애니메이션 2개 이상)
            //    랜덤으로 50% 확률 실행합니다.
            if (Random.value > 0.5f)
                SetAnimTrigger(hashDie); // "Death1"
            else
                SetAnimTrigger(hashDie2); // "Death2"
        }
        ChangeState(fsm.DieState);
    }

    #endregion


    #region 애니메이션

    public event System.Action<int> OnAnimatorTriggered;

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
            OnAnimatorTriggered?.Invoke(animHash); // Notify subscribers
        }
    }

    public void SetAnimInt(int animHash, int value)
    {
        if (animator == null) return;
        animator.SetInteger(animHash, value);
    }

    #endregion

    /// <summary>
    /// 절차적 애니메이션(IK)을 켜거나 끕니다. 거미에만 해당됩니다.
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
    /// (원거리) Attack 상태에서 호출되어 플레이어에게 데미지를 입힙니다.
    /// </summary>
    public void ApplyDamageToPlayer()
    {
        if (player == null || health.IsDead) return;

        // 1. 공격 딜레이(attackDelay) 이후에 플레이어가 사거리 내에 있는지 다시 체크
        if (GetDistanceToPlayer() <= config.attackRange)
        {
            // 2. (중요) 'PlayerHealth' -> 'PlayerStats'로 변경
            if (player.TryGetComponent<PlayerStats>(out PlayerStats playerStats))
            {
                Debug.Log($"[Golem] 플레이어 공격! 데미지: {config.attackDamage}");
                playerStats.TakeDamage(config.attackDamage);
            }
            else
            {
                Debug.LogWarning($"[Golem] 플레이어({player.name})에 'PlayerStats' 스크립트가 없습니다!");
            }
        }
        else
        {
            Debug.Log("[Golem] 플레이어가 사거리를 벗어나 공격이 취소됩니다.");
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // config가 없으면 기즈모를 그리지 않습니다.
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

        // 4. 목적지 (플레이 중에만 표시)
        if (Application.isPlaying && fsm != null && (CurrentState == fsm.PatrolState || CurrentState == fsm.TraceState))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(currentDestination, 0.5f);
            Gizmos.DrawLine(transform.position, currentDestination);
        }
    }
#endif

}