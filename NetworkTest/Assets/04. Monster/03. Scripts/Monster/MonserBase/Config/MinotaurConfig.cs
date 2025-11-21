using UnityEngine;

[CreateAssetMenu(fileName = "NewMinotaurConfig", menuName = "Monster/Minotaur Config")]
public class MinotaurConfig : MonsterConfig
{
    [Header("=== Minotaur Charge Attack ===")]

    [Tooltip("돌진을 시작할 최대 거리 (이 거리 안에 들어오면 무조건 돌진!)")]
    public float chargeRange = 10f;

    [Tooltip("돌진 속도 (기본 runSpeed보다 빠르게)")]
    public float chargeSpeed = 8f;

    [Tooltip("돌진이 멈추는 거리 (플레이어와 이 거리만큼 가까워지면 공격으로 전환)")]
    public float chargeStoppingDistance = 1.5f;

    [Tooltip("돌진 후 다음 돌진까지의 쿨다운 시간 (초)")]
    public float chargeCooldown = 5f;

    [Tooltip("돌진이 중단되었을 때(근접무기 맞음) 경직 시간 (초)")]
    public float chargeStunDuration = 1.5f;

    [Tooltip("돌진 중 데미지 (플레이어에게 부딪혔을 때)")]
    public float chargeDamage = 15f;

    [Header("=== Combat Behavior ===")]

    [Tooltip("연속 공격 후 후퇴할 공격 횟수")]
    public int attacksBeforeRetreat = 2;

    [Tooltip("후퇴할 거리")]
    public float retreatDistance = 6f;

    [Tooltip("HP가 이 비율 이하로 떨어지면 회피 패턴 사용 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float dodgeHealthThreshold = 0.3f;
}