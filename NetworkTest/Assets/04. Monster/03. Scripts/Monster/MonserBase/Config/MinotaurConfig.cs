using UnityEngine;

[CreateAssetMenu(fileName = "NewMinotaurConfig", menuName = "Monster/Minotaur Config")]
public class MinotaurConfig : MonsterConfig
{
    [Header("=== Minotaur Charge (Ram) ===")]
    [Tooltip("돌진 속도")]
    public float chargeSpeed = 10f;

    [Tooltip("돌진 데미지")]
    public float chargeDamage = 20f;

    [Tooltip("돌진 최대 지속 시간 (못 맞췄을 때 멈추는 시간)")]
    public float chargeMaxDuration = 3.0f;

    [Tooltip("벽 충돌 감지 거리")]
    public float wallCheckDist = 1.5f;

    [Header("=== Combat Movement (Strafe) ===")]
    [Tooltip("공격 쿨타임 동안 플레이어 주위를 돌(Strafe) 속도")]
    public float strafeSpeed = 2.0f;

    [Tooltip("플레이어와 유지하려는 적정 거리 (이보다 가까우면 백스탭)")]
    public float keepDistance = 3.0f;

    [Tooltip("한 방향으로 이동하는 시간 (좌/우 방향 전환 주기)")]
    public float changeDirectionTime = 2.0f;

    [Header("=== AI Patterns ===")]
    [Tooltip("전투 대치(Combat Idle) 유지 시간")]
    public float combatIdleTime = 3.0f;

    [Tooltip("백스탭(Dodge) 후 바로 돌진할 확률 (0~1)")]
    public float ramAfterDodgeChance = 0.5f;
}