using System.Collections;
using UnityEngine;

namespace MinotaurStates
{
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

            // 타이머를 매 프레임 증가시킵니다
            entryTimer += Time.deltaTime;

            // 태 진입 후 아주 약간의 시간이 지난 뒤에만 도착 여부를 체크합니다.
            // 이렇게 하면 NavMeshAgent가 거리를 계산할 시간을 벌 수 있습니다.
            if (entryTimer > 0.1f && monster.arrivedAtDestination)
            {
                return monster.fsm.IdleState;
            }

            return this;
        }

        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
        }
    }

    public class Trace : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.SetAnimFloat(monster.hashMoveSpeed, 2f);

            if (monster.player != null)
            {
                monster.MoveTo(monster.player.transform.position);
            }
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.GetDistanceToPlayer() <= monster.config.attackRange)
            {
                return monster.fsm.AttackState;
            }
            Vector3 targetPosition = monster.sensor.CanSeePlayer ? monster.player.transform.position : monster.sensor.TargetLastPosition;
            monster.MoveTo(targetPosition);
            if (!monster.sensor.CanSeePlayer && monster.arrivedAtDestination)
            {
                return monster.fsm.LookAroundState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
        }
    }

    public class LookAround : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimTrigger(monster.hashTaunt);
            timer = 0f;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            // 1. 플레이어를 감지하면 즉시 추적합니다.
            if (monster.sensor.CanSeePlayer)
            {
                return monster.fsm.TraceState;
            }

            // 2. C# 타이머를 증가시킵니다.
            timer += Time.deltaTime;

            // 3. MonsterConfig에 설정된 시간이 다 되면 Patrol 상태로 갑니다.
            if (timer >= monster.config.lookAroundTime)
            {
                return monster.fsm.PatrolState;
            }
            return this;
        }

        public override void ExitState(MonsterAIController monster)
        {
        }
    }

    public class Attack : ZombieBaseState<MonsterAIController>
    {
        // 1. Hit 상태처럼, 공격이 지속되는 시간을 잴 타이머를 추가합니다.
        private float timer;
        private bool hasAppliedDamage;

        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);

            int attackIndex = Random.Range(0, 3);
            if (attackIndex == 0)
            {
                monster.SetAnimTrigger(monster.hashAttack1);
            }
            else if (attackIndex == 1)
            {
                monster.SetAnimTrigger(monster.hashAttack2);
            }
            else
            {
                monster.SetAnimTrigger(monster.hashAttack3);
            }

            if (monster.player != null)
            {
                monster.LookAt(monster.player.transform.position);
            }
            timer = 0f;
            hasAppliedDamage = false;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            // 5. (유지) 공격 중에도 플레이어를 계속 바라봅니다.
            monster.StopMoving();
            if (monster.player != null)
            {
                monster.LookAt(monster.player.transform.position);
            }

            // 6. (추가) 타이머를 증가시킵니다.
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

            // 8. 시간이 다 되기 전까지는 "Attack" 상태에 "잠겨" 있습니다.
            return this;
        }

        public override void ExitState(MonsterAIController monster)
        {
            // 9. (유지) 상태를 나갈 때 애니메이션을 끕니다.
            monster.StopAttackRoutine(); // (호출한 적 없지만, 안전을 위해 놔둠)
        }
    }
    public class Hit : ZombieBaseState<MonsterAIController>
    {
        private float hitStunDuration = 0.5f; // 피격 경직 시간 (0.5초)
        private float timer;

        public override void EnterState(MonsterAIController monster)
        {
            timer = 0f;

            if (monster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
            {
                // Trace 상태의 속도(runSpeed)를 기준으로 줄입니다.
                agent.speed = monster.config.runSpeed * 0.3f;
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
        public override void ExitState(MonsterAIController monster)
        {
            //monster.SetAnimation(monster.hashHit, false);
        }
    }

    public class Die : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.StopAllCoroutines();
            //monster.SetAnimation(monster.hashDie, true); // Die 애니메이션 재생 -> Any State에서 처리

            // 1. 콜라이더 끄기 (다른 몬스터나 총알에 안 맞게)
            if (monster.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            // 2. NavMeshAgent 끄기
            if (monster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
            {
                agent.enabled = false;
            }

            MonsterHealth health = monster.GetComponent<MonsterHealth>();

            if (health != null && monster.config.lootTable != null)
            {

                health.SpawnLoot(monster.config.lootTable);
            }

            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.RegisterMonsterDied();
            }

            Object.Destroy(monster.gameObject, monster.config.corpseDestroyDelay);
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }
}