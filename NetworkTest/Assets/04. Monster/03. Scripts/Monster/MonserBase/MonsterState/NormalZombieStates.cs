using System.Collections;
using UnityEngine;

namespace NormalZombieStates
{
    public class Idle : ZombieBaseState<MonsterAIController>
    {
        private float idleTime;
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimBool(monster.hashIsWalking, false);
            monster.SetAnimBool(monster.hashIsRunning, false);
            idleTime = Random.Range(monster.config.idleTimeMin, monster.config.idleTimeMax);
            timer = 0f;
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.TraceState;
            }
            timer += Time.deltaTime;
            if (timer >= idleTime)
            {
                // (수정) monster.fsm 사용
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
            monster.SetAnimBool(monster.hashIsWalking, true);
            entryTimer = 0f;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.TraceState;
            }

            monster.MoveTo(patrolDestination);
            entryTimer += Time.deltaTime;

            if (entryTimer > 0.1f && monster.arrivedAtDestination)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.IdleState;
            }

            return this;
        }

        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimBool(monster.hashIsWalking, false);
        }
    }

    public class Trace : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.SetAnimBool(monster.hashIsRunning, true);

            if (monster.TargetPlayer != null)
            {
                monster.MoveTo(monster.TargetPlayer.transform.position);
            }
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.GetDistanceToPlayer() <= monster.config.attackRange)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.AttackState;
            }
            Vector3 targetPosition = monster.sensor.CanSeePlayer ? monster.TargetPlayer.transform.position : monster.sensor.TargetLastPosition;
            monster.MoveTo(targetPosition);
            if (!monster.sensor.CanSeePlayer && monster.arrivedAtDestination)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.LookAroundState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimBool(monster.hashIsRunning, false);
        }
    }

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
                // (수정) monster.fsm 사용
                return monster.fsm.TraceState;
            }

            timer += Time.deltaTime;

            if (timer >= monster.config.lookAroundTime)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.PatrolState;
            }
            return this;
        }

        public override void ExitState(MonsterAIController monster) { }
    }

    public class Attack : ZombieBaseState<MonsterAIController>
    {
        private float timer;

        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimTrigger(monster.hashAttack1);
            if (monster.TargetPlayer != null)
            {
                monster.LookAt(monster.TargetPlayer.transform.position);
            }
            timer = 0f;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            monster.StopMoving();
            if (monster.TargetPlayer != null)
            {
                monster.LookAt(monster.TargetPlayer.transform.position);
            }

            timer += Time.deltaTime;

            if (timer >= monster.config.attackCooldown)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.TraceState;
            }

            return this;
        }

        public override void ExitState(MonsterAIController monster)
        {
            monster.StopAttackRoutine();
        }
    }
    public class Hit : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster) { }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            // (수정) monster.fsm 사용
            return monster.fsm.TraceState;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    public class Die : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.StopAllCoroutines();

            if (monster.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

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

            // Object.Destroy는 MonsterAIController의 DespawnRoutine에서 중앙 관리됩니다.
            // Object.Destroy(monster.gameObject, monster.config.corpseDestroyDelay);
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }
}