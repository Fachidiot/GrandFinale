using UnityEngine;

namespace GazerStates
{
    // Gazer 전용 빔 애니메이션 해시
    public static class GazerAnimHashes
    {
        public static readonly int Cast3Start = Animator.StringToHash("Cast3Start");
        public static readonly int Cast3End = Animator.StringToHash("Cast3End");
    }

    // --- 1. Idle (Locomotion 0) ---
    public class Idle : ZombieBaseState<MonsterAIController>
    {
        private float idleTime;
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);

            monster.SetAnimFloat(monster.hashIdleType, Random.Range(0, 3));

            idleTime = Random.Range(monster.config.idleTimeMin, monster.config.idleTimeMax);
            timer = 0f;

            (monster.fsm as GolemFSM)?.ResetRangedHitAnim();
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer) return monster.fsm.TraceState;
            timer += Time.deltaTime;
            if (timer >= idleTime) return monster.fsm.PatrolState;
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 2. Patrol (Locomotion 1) ---
    public class Patrol : ZombieBaseState<MonsterAIController>
    {
        private Vector3 patrolDestination;
        private float entryTimer;
        public override void EnterState(MonsterAIController monster)
        {
            patrolDestination = monster.GetRandomPatrolDestination();
            monster.SetAnimFloat(monster.hashMoveSpeed, 1f);
            entryTimer = 0f;
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer) return monster.fsm.TraceState;
            monster.MoveTo(patrolDestination);
            entryTimer += Time.deltaTime;
            if (entryTimer > 0.1f && monster.arrivedAtDestination)
            {
                return monster.fsm.IdleState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0);
        }
    }

    // --- 3. Trace (두뇌 상태, Locomotion 2) ---
    public class Trace : ZombieBaseState<MonsterAIController>
    {
        private GazerConfig gazerConfig;
        private GazerFSM gazerFSM;

        public override void EnterState(MonsterAIController monster)
        {
            if (gazerConfig == null) gazerConfig = monster.config as GazerConfig;
            if (gazerFSM == null) gazerFSM = monster.fsm as GazerFSM;

            monster.SetAnimFloat(monster.hashMoveSpeed, 2f); // Locomotion 2 (Run)
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (gazerConfig == null || gazerFSM == null) return this;

            float distance = monster.GetDistanceToPlayer();

            // 1. (회피) 쿨다운이 아니고 플레이어가 가까우면
            if (!gazerFSM.IsStrafeOnCooldown && distance <= gazerConfig.strafeRange)
            {
                return monster.fsm.TauntState; // -> EvasionState
            }

            // 2. (빔 공격) 쿨다운이 아니고 빔 사거리 안이면
            if (!gazerFSM.IsBeamOnCooldown && distance <= gazerConfig.beamRange)
            {
                return monster.fsm.LookAroundState; // -> BeamAttackState
            }

            // 3. (근접 공격)
            if (distance <= monster.config.attackRange)
            {
                return monster.fsm.AttackState; // -> MeleeAttackState
            }

            if (monster.player != null)
                monster.MoveTo(monster.player.transform.position);

            if (!monster.sensor.CanSeePlayer && monster.arrivedAtDestination)
            {
                return monster.fsm.IdleState; // (LookAround 대신 Idle로)
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 4. 근접 공격 (Attack 1~4 랜덤) ---
    public class MeleeAttack : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        private bool hasAppliedDamage;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);
            if (monster.player != null) monster.LookAt(monster.player.transform.position);

            int attackIndex = Random.Range(0, 4); // 0, 1, 2, 3
            if (attackIndex == 0) monster.SetAnimTrigger(monster.hashAttack1);
            else if (attackIndex == 1) monster.SetAnimTrigger(monster.hashAttack2);
            else if (attackIndex == 2) monster.SetAnimTrigger(monster.hashAttack3);
            else monster.SetAnimTrigger(monster.hashAttack4);

            timer = 0f;
            hasAppliedDamage = false;
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            timer += Time.deltaTime;

            if (!hasAppliedDamage && timer >= monster.config.attackDelay)
            {
                hasAppliedDamage = true;
                monster.ApplyDamageToPlayer();
            }

            if (timer >= monster.config.attackCooldown)
            {
                return monster.fsm.TraceState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 5. 빔 공격 (FSM의 LookAround 슬롯) ---
    public class BeamAttack : ZombieBaseState<MonsterAIController>
    {
        private GazerConfig gazerConfig;
        private GazerFSM gazerFSM;
        private float timer;
        private bool hasFired;
        private bool hasStoppedBeam;
        private GameObject currentBeamInstance;

        // ★ (추가) 빔이 끝난 후, 상태를 종료하기까지의 추가 대기 시간 (애니 후딜레이)
        private const float beamExitDelay = 0.5f;

        public override void EnterState(MonsterAIController monster)
        {
            if (gazerConfig == null) gazerConfig = monster.config as GazerConfig;
            if (gazerFSM == null) gazerFSM = monster.fsm as GazerFSM;

            monster.StopMoving();
            if (monster.player != null) monster.LookAt(monster.player.transform.position);

            monster.SetAnimTrigger(GazerAnimHashes.Cast3Start);
            timer = 0f;
            currentBeamInstance = null;
            hasFired = false;
            hasStoppedBeam = false;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.player != null && !hasStoppedBeam)
            {
                if (monster.player != null) monster.LookAt(monster.player.transform.position, 1.5f);
            }


            timer += Time.deltaTime;

            timer += Time.deltaTime;

            // 1. (변경 없음) 빔 발사 (e.g., GazerConfig의 beamFireDelay)
            if (!hasFired && timer >= gazerConfig.beamFireDelay)
            {
                hasFired = true;
                InstantiateBeam(monster);
            }

            // 2. (변경 없음) 빔 정지 (e.g., GazerConfig의 beamCastTime)
            if (hasFired && !hasStoppedBeam && timer >= gazerConfig.beamCastTime)
            {
                hasStoppedBeam = true;
                StopBeam();
                monster.SetAnimTrigger(GazerAnimHashes.Cast3End);
            }

            // 'attackCooldown' 대신 'beamCastTime + beamExitDelay'를 사용합니다.
            // (빔 애니메이션이 완전히 끝나고 Trace로 복귀하도록)
            if (hasStoppedBeam && timer >= (gazerConfig.beamCastTime + beamExitDelay))
            {
                gazerFSM.StartBeamCooldown(gazerConfig.beamCooldown); // 빔 쿨다운 시작
                return monster.fsm.TraceState;
            }

            return this; // 상태 유지
        }

        public override void ExitState(MonsterAIController monster)
        {
            // 상태가 강제로 종료될 경우(e.g., 피격) 빔 즉시 정지
            StopBeam();
        }

        /// <summary>
        /// 빔 프리팹을 생성하고 firePoint에 부착합니다.
        /// </summary>
        private void InstantiateBeam(MonsterAIController monster)
        {
            if (gazerConfig.beamPrefab == null)
            {
                Debug.LogError("GazerConfig에 beamPrefab이 없습니다!");
                return;
            }
            if (monster.firePoint == null)
            {
                Debug.LogError("MonsterAIController에 firePoint가 없습니다!");
                return;
            }

            currentBeamInstance = Object.Instantiate(gazerConfig.beamPrefab, monster.firePoint.position, monster.firePoint.rotation);
            currentBeamInstance.transform.SetParent(monster.firePoint, true);

            Debug.LogWarning($"액션빔 발사! (딜레이: {gazerConfig.beamFireDelay}초)");
        }

        /// <summary>
        /// 현재 발사 중인 빔이 있다면 파괴합니다.
        /// </summary>
        private void StopBeam()
        {
            if (currentBeamInstance != null)
            {
                Object.Destroy(currentBeamInstance);
                currentBeamInstance = null;
                Debug.LogWarning($"액션빔 정지! (시간: {gazerConfig.beamCastTime}초)");
            }
        }
    }

    // --- 6. 회피 (FSM의 Taunt 슬롯) ---
    public class Evasion : ZombieBaseState<MonsterAIController>
    {
        private GazerConfig gazerConfig;
        private GazerFSM gazerFSM;
        private float timer;

        public override void EnterState(MonsterAIController monster)
        {
            if (gazerConfig == null) gazerConfig = monster.config as GazerConfig;
            if (gazerFSM == null) gazerFSM = monster.fsm as GazerFSM;

            monster.StopMoving();

            // 50% 확률로 좌/우 회피
            if (Random.value > 0.5f)
                monster.SetAnimTrigger(monster.hashLookAround); // (StrafeLeft)
            else
                monster.SetAnimTrigger(monster.hashTaunt); // (StrafeRight)

            timer = 0f;
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (gazerConfig == null || gazerFSM == null) return this;

            timer += Time.deltaTime;
            // 회피 애니메이션이 끝나면
            if (timer >= gazerConfig.strafeDuration)
            {
                gazerFSM.StartStrafeCooldown(gazerConfig.strafeCooldown); // 5초 쿨다운 시작
                return monster.fsm.TraceState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 7. 피격 (GotHit 1/2 랜덤) ---
    public class Hit : ZombieBaseState<MonsterAIController>
    {
        private float hitStunDuration = 0.5f;
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            timer = 0f;
            monster.StopMoving();
            if (monster.fsm is GazerFSM gazerFSM)
            {
                gazerFSM.StartHitCooldown(10f);
            }

        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            timer += Time.deltaTime;
            if (timer >= hitStunDuration)
            {
                return monster.fsm.TraceState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 8. 사망 ---
    public class Die : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            // (사망 애니메이션은 MonsterAIController가 Death1/2 대신
            //  Config의 dieTrigger(Death)만 호출하도록 되어있음)
            monster.StopMoving();
            monster.StopAllCoroutines();
            if (monster.TryGetComponent<Collider>(out var c)) c.enabled = false;
            if (monster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var a)) a.enabled = false;
            MonsterHealth health = monster.GetComponent<MonsterHealth>();
            if (health != null && monster.config.lootTable != null)
                health.SpawnLoot(monster.config.lootTable);
            if (MonsterManager.Instance != null)
                MonsterManager.Instance.RegisterMonsterDied();
            Object.Destroy(monster.gameObject, monster.config.corpseDestroyDelay);
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController m) { return this; }
        public override void ExitState(MonsterAIController m) { }
    }
}