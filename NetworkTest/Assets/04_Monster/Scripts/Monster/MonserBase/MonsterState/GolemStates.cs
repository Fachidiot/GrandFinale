using UnityEngine;

namespace GolemStates
{
    // --- 1. Idle 상태 ---
    // (이전과 동일)
    public class Idle : ZombieBaseState<MonsterAIController>
    {
        private float idleTime;
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);
            idleTime = Random.Range(monster.config.idleTimeMin, monster.config.idleTimeMax);
            timer = 0f;
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer)
            {
                return monster.fsm.TraceState;
            }
            timer += Time.deltaTime;
            if (timer >= idleTime)
            {
                return monster.fsm.PatrolState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 2. Patrol 상태 ---
    // (이전과 동일)
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
            if (monster.sensor.CanSeePlayer)
            {
                return monster.fsm.TraceState;
            }
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
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);
        }
    }

    // --- 3. Trace 상태 ---
    // (이전과 동일)
    public class Trace : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.SetAnimFloat(monster.hashMoveSpeed, 2f); // Locomotion 2 (Run)
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            // 공격 범위에 들어오면 공격
            if (monster.GetDistanceToPlayer() <= monster.config.attackRange)
            {
                return monster.fsm.AttackState;
            }

            if (monster.player != null)
                monster.MoveTo(monster.player.transform.position);

            if (!monster.sensor.CanSeePlayer && monster.arrivedAtDestination)
            {
                return monster.fsm.LookAroundState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            if (monster.CurrentState != monster.fsm.AttackState &&
                monster.CurrentState != monster.fsm.BlockState)
            {
                monster.SetAnimFloat(monster.hashMoveSpeed, 0f);
            }
        }
    }

    // --- 4. (수정) 콤보 공격 상태 ---
    public class Attack : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        private bool hasAppliedDamage;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);

            if (monster.player != null)
                monster.LookAt(monster.player.transform.position);

            // (요청) Attack 1~4 중 하나를 랜덤으로 실행
            int attackIndex = Random.Range(0, 4); // 0, 1, 2, 3

            if (attackIndex == 0)
                monster.SetAnimTrigger(monster.hashAttack1);
            else if (attackIndex == 1)
                monster.SetAnimTrigger(monster.hashAttack2);
            else if (attackIndex == 2)
                monster.SetAnimTrigger(monster.hashAttack3);
            else
                monster.SetAnimTrigger(monster.hashAttack4);

            timer = 0f; // 공격 애니메이션 타이머

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

            if (timer < monster.config.attackCooldown)
            {
                return this; // 상태 유지
            }

            // 쿨다운이 지났으므로 Trace 상태로 복귀
            return monster.fsm.TraceState;
        }

        public override void ExitState(MonsterAIController monster)
        {
            // (콤보가 없으므로 비워둠)
        }
    }
    // --- 5. (수정) 방어/반격 상태 ---
    public class Block : ZombieBaseState<MonsterAIController>
    {
        // (요청) 방어(3-5초) -> 취약(1초) -> 돌진
        public enum Phase { Blocking, VulnerableCheck, CounterRush }
        public Phase CurrentPhase { get; private set; }

        private float timer;
        private float blockDuration; // 3~5초 랜덤 방어 시간
        private float vulnerableDuration = 1.0f; // 1초 취약 시간

        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimTrigger(monster.hashBlockStart); // BlockStart 애니메이션

            CurrentPhase = Phase.Blocking;
            timer = 0f;
            blockDuration = Random.Range(3.0f, 5.0f);
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            timer += Time.deltaTime;

            // 1. "방어 중" 단계 (3~5초)
            if (CurrentPhase == Phase.Blocking)
            {
                // 방어 시간이 끝나면 "취약" 단계로
                if (timer >= blockDuration)
                {
                    CurrentPhase = Phase.VulnerableCheck;
                    timer = 0f; // 타이머 리셋 (1초 카운트)
                }
            }
            // 2. "취약" 단계 (1초)
            else if (CurrentPhase == Phase.VulnerableCheck)
            {
                // (참고: 이 1초 동안 MonsterAIController가 HandleHit을 받으면
                //  HitState로 강제 전환될 것임)

                // 1초가 지나면 "반격 돌진" 단계로
                if (timer >= vulnerableDuration)
                {
                    CurrentPhase = Phase.CounterRush;
                    monster.SetAnimTrigger(monster.hashBlockEnd);
                    monster.SetAnimFloat(monster.hashMoveSpeed, 2f); // Locomotion 2
                }
            }
            // 3. "반격 돌진" 단계
            else if (CurrentPhase == Phase.CounterRush)
            {
                if (monster.player != null)
                    monster.MoveTo(monster.player.transform.position);

                // 공격 범위에 도착하면 공격 상태로
                if (monster.GetDistanceToPlayer() <= monster.config.attackRange)
                {
                    return monster.fsm.AttackState;
                }
            }
            return this;
        }

        public override void ExitState(MonsterAIController monster)
        {
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f); // Locomotion 0

            var golemFSM = monster.fsm as GolemFSM;
            if (golemFSM != null)
            {
                golemFSM.StartBlockCooldown(10f);
            }
        }
    }

    // --- 6. LookAround 상태 ---
    // (이전과 동일)
    public class LookAround : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimTrigger(monster.hashLookAround);
            timer = 0f;
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer)
            {
                return monster.fsm.TraceState;
            }
            timer += Time.deltaTime;
            if (timer >= monster.config.lookAroundTime)
            {
                return monster.fsm.PatrolState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 7. Hit 상태 ---
    // (이전과 동일)
    public class Hit : ZombieBaseState<MonsterAIController>
    {
        private float hitStunDuration = 0.5f;
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            timer = 0f;
            if (monster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                agent.speed = monster.config.runSpeed * 0.3f;
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

    // --- 8. Die 상태 ---
    // (이전과 동일)
    public class Die : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
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