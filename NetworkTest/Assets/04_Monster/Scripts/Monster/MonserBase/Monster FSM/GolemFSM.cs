using UnityEngine;
using GolemStates;

// 1. MonsterFSM 설계도를 상속
public class GolemFSM : MonsterFSM
{
    [HideInInspector]
    public int comboStep = 0;
    [HideInInspector]
    public float lastAttackTime = 0f;

    private float blockCooldownTimer = 0f;

    private bool hasPlayedRangedHitAnim = false;

    public bool HasPlayedRangedHitAnim => hasPlayedRangedHitAnim;
    public bool IsBlockOnCooldown => blockCooldownTimer > 0;

    public void StartBlockCooldown(float duration)
    {
        blockCooldownTimer = duration;
    }
    public void SetRangedHitAnimPlayed()
    {
        hasPlayedRangedHitAnim = true;
    }
    public void ResetRangedHitAnim()
    {
        hasPlayedRangedHitAnim = false;
    }

    private void Update()
    {
        if (blockCooldownTimer > 0)
        {
            blockCooldownTimer -= Time.deltaTime;
        }

    }

    // 2. GolemStates의 상태 클래스들을 생성
    private readonly Idle _idleState = new Idle();
    private readonly Patrol _patrolState = new Patrol();
    private readonly Trace _traceState = new Trace();
    private readonly Attack _attackState = new Attack();
    private readonly LookAround _lookAroundState = new LookAround();
    private readonly Hit _hitState = new Hit();
    private readonly Die _dieState = new Die();
    private readonly Block _blockState = new Block(); // ★ 골렘 전용 방어 상태

    // 3. FSM 슬롯에 상태들을 반환
    public override ZombieBaseState<MonsterAIController> IdleState => _idleState;
    public override ZombieBaseState<MonsterAIController> PatrolState => _patrolState;
    public override ZombieBaseState<MonsterAIController> TraceState => _traceState;
    public override ZombieBaseState<MonsterAIController> AttackState => _attackState;
    public override ZombieBaseState<MonsterAIController> LookAroundState => _lookAroundState;
    public override ZombieBaseState<MonsterAIController> HitState => _hitState;
    public override ZombieBaseState<MonsterAIController> DieState => _dieState;
    public override ZombieBaseState<MonsterAIController> BlockState => _blockState;
    public override ZombieBaseState<MonsterAIController> TauntState => null;
}