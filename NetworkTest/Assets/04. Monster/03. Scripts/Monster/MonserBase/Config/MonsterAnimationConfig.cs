// MonsterAnimationConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterAnimConfig", menuName = "Monster/Animation Config")]
public class MonsterAnimationConfig : ScriptableObject
{
    [Header("Movement Parameters")]
    [Tooltip("이동 속도를 제어하는 Float 파라미터 이름 (예: Speed, Locomotion)")]
    public string moveSpeedFloat = "Locomotion"; // NavMeshMovement와 일치시킬 수 있음

    [Tooltip("Idle 애니메이션 타입을 제어하는 Int 파라미터 이름 (예: IdleType)")]
    public string idleTypeInt = "IdleType";

    [Header("State Parameters (Bool)")]
    [Tooltip("필요한 경우 Bool 상태 파라미터 (예: isWalking, isStunned)")]
    public string isWalkingBool = "Patrol"; // (기존 좀비 호환용)
    public string isRunningBool = "Trace"; // (기존 좀비 호환용)

    [Header("Action Parameters (Trigger)")]
    [Tooltip("공격 1번 트리거")]
    public string attackTrigger1 = "Attack"; // (기존 좀비 호환용)
    [Tooltip("공격 2번 트리거")]
    public string attackTrigger2 = "Attack2";
    [Tooltip("공격 3번 트리거")]
    public string attackTrigger3 = "Attack3";
    [Tooltip("공격 4번 트리거")]
    public string attackTrigger4 = "Attack4";

    [Space(10)]
    [Tooltip("피격 트리거")]
    public string hitTrigger = "GotHit";
    public string hitTrigger2 = "GotHit2";

    [Tooltip("사망 트리거 또는 Bool (트리거 권장)")]
    public string dieTrigger = "Death1"; // (기존 좀비 Die Bool과 이름이 같음)

    [Tooltip("사망 2번 트리거")]
    public string dieTrigger2 = "Death2";

    [Header("Defense Triggers")]
    public string blockStartTrigger = "BlockStart";
    public string blockEndTrigger = "BlockEnd";

    [Header("Special Triggers")]
    public string lookAroundTrigger = "LookAround";
    public string tauntTrigger = "Taunt";


}
