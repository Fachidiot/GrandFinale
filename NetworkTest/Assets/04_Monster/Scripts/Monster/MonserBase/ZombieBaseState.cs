/// <summary>
/// 몬스터 AI 상태 머신(FSM)의 모든 상태가 상속받아야 하는 기본 클래스(설계도)입니다.
/// </summary>
/// <typeparam name="T">이 상태를 소유할 컨트롤러의 타입 (예: MonsterAIController)</typeparam>
public abstract class ZombieBaseState<T> where T : class
{
    /// <summary>
    /// 상태에 처음 진입했을 때 한 번 호출됩니다.
    /// </summary>
    public abstract void EnterState(T monster);

    /// <summary>
    /// 상태가 활성화된 동안 매 프레임 호출됩니다.
    /// 다른 상태로 전환해야 할 경우, 새로운 상태 객체를 반환합니다.
    /// </summary>
    public abstract ZombieBaseState<T> UpdateState(T monster);

    /// <summary>
    /// 상태를 빠져나갈 때 한 번 호출됩니다.
    /// </summary>
    public abstract void ExitState(T monster);
}