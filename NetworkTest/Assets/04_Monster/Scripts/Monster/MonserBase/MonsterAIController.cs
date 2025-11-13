using System.Collections;
using UnityEngine;
using UnityEngine.AI;

//   ʼ Ʈ մϴ.
[RequireComponent(typeof(IMonsterMovement), typeof(MonsterHealth), typeof(MonsterSensor))]
public class MonsterAIController : MonoBehaviour
{
    #region ʵ
    // --- ֿ Ʈ  ---
    private IMonsterMovement movement;
    private MonsterHealth health;
    public MonsterSensor sensor { get; private set; }

    private NavMeshAgent agent;

    private Animator animator;
    [Header(" ")]
    public MonsterConfig config;

    [Tooltip("ü ߻ ġ (:  , )  Ĺ  ͸ ")]
    public Transform firePoint;

    [Header("ִϸ̼ ")]
    public MonsterAnimationConfig animConfig;

    public MonsterFSM fsm { get; private set; }

    public GameObject player { get; private set; }

    [Header(" ")]
    public bool alwaysShowGizmos = false;
    // ---  ӽ (FSM) ---
    public ZombieBaseState<MonsterAIController> CurrentState { get; private set; }


    // <<<< 2.      >>>>
    // NavMeshAgent   ¸ ϰ, (Ź) ó Ÿ  Ǵ
    public bool arrivedAtDestination
    {
        get
        {
            if (agent != null) //  
            {
                //   ,  Ÿ  Ÿ    
                return !agent.pathPending && agent.remainingDistance <= config.stoppingDistance;
            }
            else // Ź 
            {
                return Vector3.Distance(transform.position, currentDestination) < config.stoppingDistance;
            }
        }
    }

    public Vector3 currentDestination { get; private set; }

    // --- ڷƾ  ---
    public Coroutine attackRoutineCor { get; set; }

    // --- ִϸ̼ ؽ ---
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

    #region ʱȭ  
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
            Debug.LogError(gameObject.name + " MonsterAnimationConfig  Ҵ ȵ");
            return;
        }

        TryGetComponent<NavMeshAgent>(out agent);

        if (config == null)
        {
            Debug.LogError(gameObject.name + " MonsterConfig  Ҵ ȵ");
            return;
        }

        if (fsm == null)
        {
            Debug.LogError(gameObject.name + " MonsterFSM ('') Ʈ ϴ! GolemFSM, GazerFSM  ּ߰.", this);
            return; // Start() Լ  ʵ ߴ
        }

        // 3. Health Ʈ Config  Ѱ ʱȭŵϴ.
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

    #region  
    public void ChangeState(ZombieBaseState<MonsterAIController> newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);


    }
    #endregion


    #region ̵ 
    public void MoveTo(Vector3 destination)
    {
        currentDestination = destination;
        float speed = (CurrentState == fsm.TraceState) ? config.runSpeed : config.walkSpeed;
        //   ̵ ýۿ մϴ.
        movement.Move(destination, speed);

        // NavMeshAgent  (Ź)  ȸ ݴϴ.
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

    #region , , ̺Ʈ ڵ鷯

    public bool CanSeePlayer => sensor.CanSeePlayer;
    public Vector3 targetLastPos => sensor.TargetLastPosition;

    public float GetDistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;

        Vector3 monsterPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPos = new Vector3(player.transform.position.x, 0, player.transform.position.z);
        return Vector3.Distance(monsterPos, playerPos);
    }
    public IEnumerator AttackRoutine() { WaitForSeconds attackCooldown = new WaitForSeconds(config.attackCooldown); while (GetDistanceToPlayer() <= config.attackRange) { Debug.Log(" !"); yield return attackCooldown; } }
    public void StopAttackRoutine() { if (attackRoutineCor != null) { StopCoroutine(attackRoutineCor); attackRoutineCor = null; } }
    // MonsterAIController.cs -> HandleHit ( 1븸 ¾Ƶ Block ϱ Ѵٸ)

    private void HandleHit()
    {
        if (health.IsDead) return;

        if (fsm is GazerFSM gazerFSM)
        {
            //  (ű) 1-1. ÷̾   (Idle/Patrol) ¾Ҵ°?
            // (û 1:  ȵƴµ )
            if (CurrentState == fsm.IdleState || CurrentState == fsm.PatrolState)
            {
                Debug.Log("GAZER HIT: (Idle/Patrol)  ǰ!     .");
                if (player != null)
                {
                    sensor.ForceDetection(player.transform.position); // (û: iƿ)
                }
                SetAnimTrigger(hashHit);     // (û: hitִϸ̼)
                ChangeState(fsm.HitState); // Hit · ȯ ( Trace )
                return; // (߿)  HP Ӱ  ŵ
            }

            //  () 1-2. (Trace/Attack )  ߿ ¾Ҵ°?
            // ( HP Ӱ )
            if (gazerFSM.IsHitOnCooldown)
            {
                Debug.Log("Gazer Hit: ٿ ...  .");
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
                Debug.Log("Gazer Hit: HP Ӱ ƴϹǷ  .");
            }
        }
        //  2. () GOLEM ǰ  
        else if (fsm is GolemFSM golemFSM)
        {
            // --- (켱 1: Block ) ---
            // (䱸 4: Block ΰ?)
            if (CurrentState == fsm.BlockState)
            {
                var blockState = CurrentState as GolemStates.Block;

                // (䱸 3: Block Ǯ 1  Ÿ̹ΰ?)
                if (blockState != null && blockState.CurrentPhase == GolemStates.Block.Phase.VulnerableCheck)
                {
                    Debug.Log("GOLEM HIT: (Vulnerable) ¿ ǰ! HitState ȯ.");
                    SetAnimTrigger(hashHit); // Hit ִϸ̼ 
                    ChangeState(fsm.HitState); // Hit (ӵ) ȯ
                }
                else
                {
                    // 'Blocking' ̹ܰǷ    (Hit ִϸ̼ )
                    Debug.Log("GOLEM HIT: (Blocking) ! ǰ .");
                }
                return; //  ̹Ƿ Ʒ    
            }

            // --- (켱 2: Block ߵ ) ---
            // (䱸 4: Block ؾ !)
            // OnBlock ̺Ʈ OnHit ʰ Ƿ, Health ī͸  üũ
            if (health.hitCounter >= health.blockTriggerHits)
            {
                Debug.Log("GOLEM HIT: Block ߵ  ! Hit ִϸ̼ .");
                //  HandleBlock ȣǾ BlockState ٲ ̹Ƿ HitState  
                // (ӵ ϵ )
                return;
            }

            // --- (켱 3: Ÿ ǰ) ---
            // (䱸 2, 5: ָ  ѹ)
            float distance = GetDistanceToPlayer();
            // (:  Ÿ 2躸 ְ,  Ÿ Hit ִϸ  )
            if (distance > (config.attackRange * 2) && !golemFSM.HasPlayedRangedHitAnim)
            {
                Debug.Log("GOLEM HIT: Ÿ ǰ! HitState ȯ (ִϸ̼ ).");
                golemFSM.SetRangedHitAnimPlayed(); // ÷  (ٽ ϰ)
                SetAnimTrigger(hashHit); // Hit ִϸ̼ 
                ChangeState(fsm.HitState); // Hit (ӵ) ȯ
                return;
            }

            // --- (켱 4:    ǰ) ---
            // (䱸 1: ׳ ӵ )
            // (: ̼ ¾ , Ǵ Ÿ  ° ̻ ¾ )
            Debug.Log("GOLEM HIT: Ϲ ǰ. HitState ȯ (ִϸ̼ ).");
            // SetAnimTrigger(hashHit) ȣ  
            ChangeState(fsm.HitState); // Hit (ӵ)θ ȯ
        }

        else if (fsm is MinotaurFSM)
        {
            // Minotaur ִϸ̼(SetAnimTrigger)  ʰ
            // HitState(ӵ )θ  ȯմϴ.
            Debug.Log("MINOTAUR HIT: HitState ȯ (ִϸ̼ ).");
            ChangeState(fsm.HitState); //
        }
        // 4.    (,  )
        else
        {
            //   (ִϸ̼  + HitState)
            Debug.Log("DEFAULT HIT: HitState ȯ (ִϸ̼ ).");
            SetAnimTrigger(hashHit);
            ChangeState(fsm.HitState);
        }
    }
    private void HandleBlock()
    {
        if (health.IsDead) return;

        // 1. GolemFSM Ȯ
        var golemFSM = fsm as GolemFSM;

        // 2. GolemFSM ƴϸ (: , ) /ݰ  
        if (golemFSM == null)
        {
            return;
        }

        if (CurrentState == fsm.BlockState)
        {
            return;
        }

        // 4. 10 ٿ   
        if (golemFSM.IsBlockOnCooldown)
        {
            Debug.Log(" ٿ ... !");
            return;
        }

        // 5. Golem ° ٿ ƴϹǷ Block · ȯ
        ChangeState(fsm.BlockState);
    }
    private void HandleDeath()
    {
        StopAllCoroutines();
        if (string.IsNullOrEmpty(animConfig.dieTrigger2))
        {
            // 1. DieTrigger2 ִ  (Gazer  Death ִϸ̼ 1 )
            //    config  ù ° dieTrigger (hashDie) մϴ.
            SetAnimTrigger(hashDie);
        }
        else
        {
            // 2. DieTrigger2 Ǿ ִ  (Death ִϸ̼ 2 ̻ )
            //    ó 50% Ȯ  մϴ.
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
    ///   (IK) Ѱų ϴ. Ź̿Ը ش˴ϴ.
    /// </summary>
    public void SetProceduralMovement(bool isActive)
    {
        // Spider Ʈ ִ Ȯ
        if (TryGetComponent<Spider>(out var spiderBody))
        {
            spiderBody.enabled = isActive;
        }
        // IKStepManager Ʈ ִ Ȯ
        if (TryGetComponent<IKStepManager>(out var spiderStepManager))
        {
            spiderStepManager.enabled = isActive;
        }
    }

    /// <summary>
    /// (ű) Attack ¿ ȣǾ ÷̾  մϴ.
    /// </summary>
    public void ApplyDamageToPlayer()
    {
        if (player == null || health.IsDead) return;

        // 1.  (attackDelay) Ŀ ÷̾ Ÿ ȿ ִ ٽ üũ
        if (GetDistanceToPlayer() <= config.attackRange)
        {
            // 2.  () 'PlayerHealth' -> 'PlayerStats'  
            if (player.TryGetComponent<PlayerStats>(out PlayerStats playerStats))
            {
                Debug.Log($"[Golem] ÷̾ ! : {config.attackDamage}");
                playerStats.TakeDamage(config.attackDamage);
            }
            else
            {
                Debug.LogWarning($"[Golem] ÷̾({player.name}) 'PlayerStats' ũƮ ϴ!");
            }
        }
        else
        {
            Debug.Log("[Golem] ÷̾ Ÿ   ϴ.");
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // config   ƹ͵ ׸ ʽϴ.
        if (config == null) return;

        // 1.   (Attack Range)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.attackRange);

        // 2. ߴ Ÿ (Stopping Distance)
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, config.stoppingDistance);

        // 3. Ҹ   (Sound Range)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.soundRange);

        // 4.   ( ߿ ǥ)
        if (Application.isPlaying && fsm != null && (CurrentState == fsm.PatrolState || CurrentState == fsm.TraceState))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(currentDestination, 0.5f);
            Gizmos.DrawLine(transform.position, currentDestination);
        }
    }
#endif

}