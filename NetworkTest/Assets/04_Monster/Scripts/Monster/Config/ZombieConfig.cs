using UnityEngine;

/// <summary>
/// 몬스터의 모든 설정 값을 담는 ScriptableObject입니다.
/// 이 파일을 에셋으로 만들어두면, 코드를 수정하지 않고도 몬스터의 능력치와 행동 패턴을 쉽게 변경할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "NewZombieConfig", menuName = "Monster/Zombie Config")]
public class ZombieConfig : ScriptableObject
{
    [Header("기본 능력치")]
    public float maxHP = 100f;
    public float attackDamage = 10f;
    public float defense = 0f;

    [Header("AI 행동 및 감지")]
    [Tooltip("플레이어를 감지할 수 있는 최대 거리")]
    public float fovRange = 10f;
    [Tooltip("전방을 기준으로 한 시야각 (도)")]
    [Range(0, 360)]
    public float fovAngle = 120f;
    [Tooltip("공격을 시작할 수 있는 최대 거리")]
    public float attackRange = 2f;
    [Tooltip("목표 지점과 이 거리 이내로 가까워지면 '도착'으로 간주")]
    public float stoppingDistance = 1.5f;

    [Tooltip("소리를 감지할 수 있는 최대 거리")]
    public float soundRange = 15f;

    [Header("순찰 (Patrol)")]
    public float patrolRadiusMin = 5f;
    public float patrolRadiusMax = 10f;

    [Header("대기 (Idle)")]
    public float idleTimeMin = 2f;
    public float idleTimeMax = 4f;

    [Header("주변 둘러보기 (LookAround)")]
    public float lookAroundTime = 3f;
    public float lookAroundTurnInterval = 1.5f;

    [Header("이동 관련")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3f;
    public float turnSpeed = 5f;

    [Header("공격 패턴")]
    [Tooltip("공격 애니메이션 시작 후 실제 데미지가 들어가기까지의 시간")]
    public float attackDelay = 0.5f;
    [Tooltip("한 번 공격한 후 다음 공격까지의 최소 시간")]
    public float attackCooldown = 2f;
}