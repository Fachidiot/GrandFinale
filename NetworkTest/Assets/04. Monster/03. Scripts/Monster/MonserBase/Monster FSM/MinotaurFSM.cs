// MinotaurFSM.cs
using UnityEngine;
using MinotaurStates;

// 1. MonsterFSM 설계도를 상속
public class MinotaurFSM : MonsterFSM
{
    // 2. MinotaurStates의 상태 클래스들을 생성
    private readonly Idle _idleState = new Idle();
    private readonly Patrol _patrolState = new Patrol();
    private readonly Trace _traceState = new Trace();
    private readonly Attack _attackState = new Attack();
    private readonly LookAround _lookAroundState = new LookAround();
    private readonly Hit _hitState = new Hit();
    private readonly Die _dieState = new Die();

    private readonly CombatIdle _combatIdleState = new CombatIdle();   // 좌우 무빙 & 간보기
    private readonly Dodge _dodgeState = new Dodge();                  // 백스탭
    private readonly RamAttack _ramAttackState = new RamAttack();

    // 3. FSM 슬롯에 상태들을 반환
    public override ZombieBaseState<MonsterAIController> IdleState => _idleState;
    public override ZombieBaseState<MonsterAIController> PatrolState => _patrolState;
    public override ZombieBaseState<MonsterAIController> TraceState => _traceState;
    public override ZombieBaseState<MonsterAIController> AttackState => _attackState;
    public override ZombieBaseState<MonsterAIController> LookAroundState => _lookAroundState;
    public override ZombieBaseState<MonsterAIController> HitState => _hitState;
    public override ZombieBaseState<MonsterAIController> DieState => _dieState;
    public override ZombieBaseState<MonsterAIController> BlockState => null;
    public override ZombieBaseState<MonsterAIController> TauntState => null;
    public ZombieBaseState<MonsterAIController> CombatIdleState => _combatIdleState;
    public ZombieBaseState<MonsterAIController> DodgeState => _dodgeState; // 백스탭
    public ZombieBaseState<MonsterAIController> RamAttackState => _ramAttackState;
}