using GazerStates;
using System;
using UnityEngine;

public class GazerFSM : MonsterFSM
{
    // --- 쿨다운 타이머 ---
    private float beamCooldownTimer = 0f;
    private float strafeCooldownTimer = 0f;
    private float hitCooldownTimer = 0f;

    private bool canTriggerHit90 = true;
    private bool canTriggerHit60 = true;
    private bool canTriggerHit30 = true;
    public bool IsHitOnCooldown => hitCooldownTimer > 0;

    public bool IsBeamOnCooldown => beamCooldownTimer > 0;
    public bool IsStrafeOnCooldown => strafeCooldownTimer > 0;
    public void StartHitCooldown(float duration) { hitCooldownTimer = duration; }

    public void StartBeamCooldown(float duration) { beamCooldownTimer = duration; }
    public void StartStrafeCooldown(float duration) { strafeCooldownTimer = duration; }

    private void Update()
    {
        if (beamCooldownTimer > 0) beamCooldownTimer -= Time.deltaTime;
        if (strafeCooldownTimer > 0) strafeCooldownTimer -= Time.deltaTime;
        if (hitCooldownTimer > 0) hitCooldownTimer -= Time.deltaTime;
    }
    /// <summary>
    /// Gazer의 HP 임계점을 확인하고, 경직이 발동되어야 하는지 알려줍니다.
    /// </summary>
    /// <returns>경직이 발동되면 true</returns>
    public bool CheckAndTriggerThreshold(float hpPercent)
    {
        // 쿨다운이 아니며, HP가 30% 이하이고, 30% 플래그가 켜져있을 때
        if (hpPercent <= 0.3f && canTriggerHit30)
        {
            canTriggerHit30 = false; // 플래그를 끔 (다음엔 발동 안 함)
            return true; // 경직 발동!
        }
        // 60%
        if (hpPercent <= 0.6f && canTriggerHit60)
        {
            canTriggerHit60 = false;
            return true; // 경직 발동!
        }
        // 90%
        if (hpPercent <= 0.9f && canTriggerHit90)
        {
            canTriggerHit90 = false;
            return true; // 경직 발동!
        }

        return false; // 경직 발동 조건이 아님
    }

    // (참고: 몬스터가 리스폰될 때 이 플래그들을 다시 true로 켜줘야 합니다.)
    public void ResetThresholds()
    {
        canTriggerHit90 = true;
        canTriggerHit60 = true;
        canTriggerHit30 = true;
    }

    // --- 상태 정의 ---
    private readonly Idle _idleState = new Idle();
    private readonly Patrol _patrolState = new Patrol();
    private readonly Trace _traceState = new Trace();
    private readonly MeleeAttack _attackState = new MeleeAttack();
    private readonly BeamAttack _lookAroundState = new BeamAttack(); // (LookAround 슬롯 -> 빔)
    private readonly Hit _hitState = new Hit();
    private readonly Die _dieState = new Die();
    private readonly Evasion _tauntState = new Evasion(); // (Taunt 슬롯 -> 회피)

    // --- 슬롯 연결 ---
    public override ZombieBaseState<MonsterAIController> IdleState => _idleState;
    public override ZombieBaseState<MonsterAIController> PatrolState => _patrolState;
    public override ZombieBaseState<MonsterAIController> TraceState => _traceState;
    public override ZombieBaseState<MonsterAIController> AttackState => _attackState;
    public override ZombieBaseState<MonsterAIController> LookAroundState => _lookAroundState;
    public override ZombieBaseState<MonsterAIController> HitState => _hitState;
    public override ZombieBaseState<MonsterAIController> DieState => _dieState;
    public override ZombieBaseState<MonsterAIController> TauntState => _tauntState;
    public override ZombieBaseState<MonsterAIController> BlockState => null;
}