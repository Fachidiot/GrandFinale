using UnityEngine;

// 이 컴포넌트는 FSM 상태들을 '소유'하고 컨트롤러에 '제공'하는 역할만 합니다.
// MonsterAIController는 이 스크립트가 MinotaurFSM인지 ZombieFSM인지 모릅니다.
public abstract class MonsterFSM : MonoBehaviour
{
    // MonsterAIController가 이 FSM의 상태들에 접근할 수 있도록
    // public abstract 프로퍼티(get)로 '슬롯'만 만들어 둡니다.

    public abstract ZombieBaseState<MonsterAIController> IdleState { get; }
    public abstract ZombieBaseState<MonsterAIController> PatrolState { get; }
    public abstract ZombieBaseState<MonsterAIController> TraceState { get; }
    public abstract ZombieBaseState<MonsterAIController> AttackState { get; }
    public abstract ZombieBaseState<MonsterAIController> LookAroundState { get; }
    public abstract ZombieBaseState<MonsterAIController> HitState { get; }
    public abstract ZombieBaseState<MonsterAIController> DieState { get; }
    public abstract ZombieBaseState<MonsterAIController> BlockState { get; }

    public abstract ZombieBaseState<MonsterAIController> TauntState { get; }
}