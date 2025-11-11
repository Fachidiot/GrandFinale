using UnityEngine;
using PlantMonsterStates; // <--- 네임스페이스 확인

public class PlantMonsterFSM : MonsterFSM
{
    private float rangedAttackCooldownTimer = 0f;

    public bool IsRangedAttackOnCooldown => rangedAttackCooldownTimer > 0;

    public void StartRangedCooldown(float duration) { rangedAttackCooldownTimer = duration; }

    // (FSM 컴포넌트가 스스로 쿨타임을 줄이도록 함)
    private void Update()
    {
        if (rangedAttackCooldownTimer > 0)
        {
            rangedAttackCooldownTimer -= Time.deltaTime;
        }

    }
    private readonly Plant_HidingState _idleState = new Plant_HidingState();
    private readonly Plant_LeapState _patrolState = new Plant_LeapState(); // (Patrol -> Leap)
    private readonly Plant_AliveState _traceState = new Plant_AliveState();
    private readonly Plant_MeleeAttackState _attackState = new Plant_MeleeAttackState(); // (Attack -> Melee)
    private readonly Plant_RangedAttackState _lookAroundState = new Plant_RangedAttackState();
    private readonly Plant_HitState _hitState = new Plant_HitState();
    private readonly Plant_DieState _dieState = new Plant_DieState();

    public override ZombieBaseState<MonsterAIController> IdleState => _idleState;
    public override ZombieBaseState<MonsterAIController> PatrolState => _patrolState;
    public override ZombieBaseState<MonsterAIController> TraceState => _traceState;
    public override ZombieBaseState<MonsterAIController> AttackState => _attackState;
    public override ZombieBaseState<MonsterAIController> LookAroundState => _lookAroundState;
    public override ZombieBaseState<MonsterAIController> HitState => _hitState;
    public override ZombieBaseState<MonsterAIController> DieState => _dieState;
    public override ZombieBaseState<MonsterAIController> BlockState => null;
    public override ZombieBaseState<MonsterAIController> TauntState => null;

}