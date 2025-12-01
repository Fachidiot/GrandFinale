using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 몬스터의 AI 로직, 이동, 애니메이션, 상태 관리를 담당하는 메인 컨트롤러
[RequireComponent(typeof(IMonsterMovement), typeof(MonsterHealth), typeof(MonsterSensor))]
public class MonsterAIController : MonoBehaviour
{
    #region 필드 및 프로퍼티

    // --- 컴포넌트 참조 ---
    private IMonsterMovement movement;
    private MonsterHealth health;
    public MonsterSensor sensor { get; private set; }
    private NavMeshAgent agent;
    private Animator animator;
    public MonsterFSM fsm { get; private set; }
    public GameObject player { get; private set; }

    [Header("설정")]
    public MonsterConfig config;
    public Transform firePoint;

    [Header("애니메이션 설정")]
    public MonsterAnimationConfig animConfig;

    [Header("디버그")]
    public bool alwaysShowGizmos = false;

    // --- 상태 머신 ---
    public ZombieBaseState<MonsterAIController> CurrentState { get; private set; }

    // [신규] 첫 조우 시 돌진 공격 수행 여부
    public bool hasPerformedFirstCharge { get; set; } = false;

    // --- 이동 관련 프로퍼티 ---
    public Vector3 currentDestination { get; private set; }

    // NavMeshAgent 사용 여부에 따른 도착 판정 로직
    public bool arrivedAtDestination
    {
        get
        {
            if (agent != null)
            {
                return !agent.pathPending && agent.remainingDistance <= config.stoppingDistance;
            }
            else
            {
                return Vector3.Distance(transform.position, currentDestination) < config.stoppingDistance;
            }
        }
    }

    public Coroutine attackRoutineCor { get; set; }

    // --- 애니메이션 해시 (최적화) ---
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

    // [신규] 엘든 링 스타일 패턴용 해시
    public int hashDodge { get; private set; }
    public int hashRamStart { get; private set; }
    public int hashRamEnd { get; private set; }
    public int hashRamWall { get; private set; }

    #endregion

    #region 초기화 (Awake / Start)

    void Awake()
    {
        movement = GetComponent<IMonsterMovement>();
        health = GetComponent<MonsterHealth>();
        sensor = GetComponent<MonsterSensor>();
        animator = GetComponentInChildren<Animator>();
        fsm = GetComponent<MonsterFSM>();
        player = GameObject.FindGameObjectWithTag("Player");
        TryGetComponent<NavMeshAgent>(out agent);

        if (animConfig == null || config == null || fsm == null)
        {
            Debug.LogError($"{gameObject.name}: 필수 Config 또는 FSM 컴포넌트가 누락되었습니다.");
            return;
        }

        InitializeAnimationHashes();
        health.Initialize(config);
    }

    void Start()
    {
        health.OnHit.AddListener(HandleHit);
        health.OnDeath.AddListener(HandleDeath);
        health.OnBlock.AddListener(HandleBlock);

        // [신규] 초기화
        hasPerformedFirstCharge = false;

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

    private void InitializeAnimationHashes()
    {
        // Config 기반 해시
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

        // [신규] 추가 패턴 해시
        hashDodge = Animator.StringToHash("Dodge");
        hashRamStart = Animator.StringToHash("RamStartAttack");
        hashRamEnd = Animator.StringToHash("RamStopAttack");
        hashRamWall = Animator.StringToHash("RamStartWall");
    }

    #endregion

    #region 업데이트 (FSM)

    void Update()
    {
        if (player == null)
        {
            if (sensor.CanSeePlayer)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }

            if (player == null) return;
        }

        if (CurrentState == null || health.IsDead) return;

        ZombieBaseState<MonsterAIController> nextState = CurrentState.UpdateState(this);
        if (nextState != CurrentState)
        {
            ChangeState(nextState);
        }
    }

    public void ChangeState(ZombieBaseState<MonsterAIController> newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState?.EnterState(this);
    }

    #endregion

    #region 이동 및 회전

    public void MoveTo(Vector3 destination)
    {
        currentDestination = destination;
        float speed = (CurrentState == fsm.TraceState) ? config.runSpeed : config.walkSpeed;

        movement.Move(destination, speed);

        // NavMeshAgent가 없으면 수동 회전
        if (agent == null)
        {
            movement.TurnTowards(destination, config.turnSpeed);
        }
    }

    //속도를 직접 지정해서 이동합니다. (돌진 공격용)
    public void MoveTo(Vector3 destination, float customSpeed)
    {
        currentDestination = destination;

        // 입력받은 customSpeed(chargeSpeed)로 이동
        movement.Move(destination, customSpeed);

        if (agent == null)
        {
            movement.TurnTowards(destination, config.turnSpeed);
        }
    }

    // [신규] 특정 방향으로 이동 (좌우 무빙/Strafe 용)
    public void MoveDirection(Vector3 dir, float speed)
    {
        if (agent != null)
        {
            agent.speed = speed;
            agent.SetDestination(transform.position + dir);
        }
        else
        {
            movement.Move(transform.position + dir, speed);
        }
    }

    public void StopMoving() { movement.Stop(); }

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

        if (Physics.Raycast(destination + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
        {
            return hit.point;
        }
        return destination;
    }

    #endregion

    #region 감지 및 전투 로직

    public bool CanSeePlayer => sensor.CanSeePlayer;
    public Vector3 targetLastPos => sensor.TargetLastPosition;

    public float GetDistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        // y축 차이 무시하고 수평 거리만 계산
        Vector3 monsterPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPos = new Vector3(player.transform.position.x, 0, player.transform.position.z);
        return Vector3.Distance(monsterPos, playerPos);
    }

    public IEnumerator AttackRoutine()
    {
        WaitForSeconds attackCooldown = new WaitForSeconds(config.attackCooldown);
        while (GetDistanceToPlayer() <= config.attackRange)
        {
            Debug.Log("공격!");
            yield return attackCooldown;
        }
    }

    public void StopAttackRoutine()
    {
        if (attackRoutineCor != null)
        {
            StopCoroutine(attackRoutineCor);
            attackRoutineCor = null;
        }
    }

    // 기본 공격 데미지 적용
    public void ApplyDamageToPlayer()
    {
        if (player == null || health.IsDead) return;

        if (GetDistanceToPlayer() <= config.attackRange)
        {
            if (player.TryGetComponent<PlayerStats>(out PlayerStats playerStats))
            {
                playerStats.TakeDamage(config.attackDamage);
            }
            else
            {
                // PlayerStats가 없을 경우 대비
                player.SendMessage("TakeDamage", config.attackDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    // [신규] 커스텀 데미지 적용 (돌진 공격용)
    public void ApplyDamageToPlayer(float customDamage)
    {
        if (player == null || health.IsDead) return;

        if (player.TryGetComponent<PlayerStats>(out PlayerStats playerStats))
        {
            playerStats.TakeDamage(customDamage);
        }
        else
        {
            player.SendMessage("TakeDamage", customDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    #endregion

    #region 이벤트 핸들러 (Hit, Block, Death)

    private void HandleHit()
    {
        if (health.IsDead) return;

        // 1. Gazer (원거리) 피격 로직
        if (fsm is GazerFSM gazerFSM)
        {
            if (CurrentState == fsm.IdleState || CurrentState == fsm.PatrolState)
            {
                if (player != null) sensor.ForceDetection(player.transform.position);
                SetAnimTrigger(hashHit);
                ChangeState(fsm.HitState);
                return;
            }

            if (gazerFSM.IsHitOnCooldown) return;

            float hpPercent = health.CurrentHP / health._maxHP;
            if (gazerFSM.CheckAndTriggerThreshold(hpPercent))
            {
                if (!sensor.CanSeePlayer && player != null) sensor.ForceDetection(player.transform.position);
                SetAnimTrigger(Random.value > 0.5f ? hashHit : hashHit2);
                ChangeState(fsm.HitState);
            }
        }
        // 2. Golem (근접) 피격 로직
        else if (fsm is GolemFSM golemFSM)
        {
            // Block 상태 중 피격
            if (CurrentState == fsm.BlockState)
            {
                var blockState = CurrentState as GolemStates.Block;
                if (blockState != null && blockState.CurrentPhase == GolemStates.Block.Phase.VulnerableCheck)
                {
                    SetAnimTrigger(hashHit);
                    ChangeState(fsm.HitState);
                }
                return;
            }

            // Block 유도 (카운터)
            if (health.hitCounter >= health.blockTriggerHits) return;

            // 원거리 피격
            float distance = GetDistanceToPlayer();
            if (distance > (config.attackRange * 2) && !golemFSM.HasPlayedRangedHitAnim)
            {
                golemFSM.SetRangedHitAnimPlayed();
                SetAnimTrigger(hashHit);
                ChangeState(fsm.HitState);
                return;
            }

            // 일반 피격
            ChangeState(fsm.HitState);
        }
        // 3. Minotaur 피격 로직 (슈퍼아머 느낌)
        else if (fsm is MinotaurFSM)
        {
            // 돌진 중이 아닐 때만 HitState 전환 (필요시 조건 추가)
            ChangeState(fsm.HitState);
        }

        // 4. 그 외 (기본)
        else
        {
            if (player != null)
            {
                sensor.ForceDetection(player.transform.position);
            }

            SetAnimTrigger(hashHit);
            ChangeState(fsm.HitState);
        }
    }

    private void HandleBlock()
    {
        if (health.IsDead) return;

        var golemFSM = fsm as GolemFSM;
        if (golemFSM == null) return;
        if (CurrentState == fsm.BlockState) return;
        if (golemFSM.IsBlockOnCooldown) return;

        ChangeState(fsm.BlockState);
    }

    private void HandleDeath()
    {
        StopAllCoroutines();

        if (string.IsNullOrEmpty(animConfig.dieTrigger2))
        {
            SetAnimTrigger(hashDie);
        }
        else
        {
            SetAnimTrigger(Random.value > 0.5f ? hashDie : hashDie2);
        }

        ChangeState(fsm.DieState);
    }

    #endregion

    #region 애니메이터 래퍼

    public event System.Action<int> OnAnimatorTriggered;

    public void SetAnimBool(int animHash, bool value)
    {
        if (animator) animator.SetBool(animHash, value);
    }

    public void SetAnimFloat(int animHash, float value)
    {
        if (animator) animator.SetFloat(animHash, value);
    }

    public void SetAnimTrigger(int animHash)
    {
        if (animator)
        {
            animator.SetTrigger(animHash);
            OnAnimatorTriggered?.Invoke(animHash);
        }
    }

    public void SetAnimInt(int animHash, int value)
    {
        if (animator) animator.SetInteger(animHash, value);
    }

    // 거미 IK 제어용
    public void SetProceduralMovement(bool isActive)
    {
        if (TryGetComponent<Spider>(out var spiderBody)) spiderBody.enabled = isActive;
        if (TryGetComponent<IKStepManager>(out var spiderStepManager)) spiderStepManager.enabled = isActive;
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (config == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.attackRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, config.stoppingDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.soundRange);

        if (Application.isPlaying && fsm != null && (CurrentState == fsm.PatrolState || CurrentState == fsm.TraceState))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(currentDestination, 0.5f);
            Gizmos.DrawLine(transform.position, currentDestination);
        }
    }
#endif
}