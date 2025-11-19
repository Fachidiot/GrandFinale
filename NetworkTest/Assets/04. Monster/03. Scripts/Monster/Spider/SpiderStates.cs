using System.Collections;
using UnityEngine;

namespace SpiderStates
{
    public class Idle : ZombieBaseState<MonsterAIController>
    {
        private float idleTime;
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
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
        public override void EnterState(MonsterAIController monster)
        {
            patrolDestination = monster.GetRandomPatrolDestination();
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.TraceState;
            }
            monster.MoveTo(patrolDestination);
            if (monster.arrivedAtDestination)
            {
                // (수정) monster.fsm 사용
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
        public override void EnterState(MonsterAIController monster) { }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.GetDistanceToPlayer() <= monster.config.attackRange)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.AttackState;
            }
            Vector3 targetPosition = monster.sensor.CanSeePlayer ? monster.player.transform.position : monster.sensor.TargetLastPosition;
            monster.MoveTo(targetPosition);
            if (!monster.sensor.CanSeePlayer && monster.arrivedAtDestination)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.PatrolState; // (거미는 LookAround 대신 Patrol로 바로 감)
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
        }
    }

    public class Attack : ZombieBaseState<MonsterAIController>
    {
        private float attackTimer;
        // (참고) 이 값은 나중에 Config로 빼는 것이 좋습니다.
        private float attackAnimationLength = 1.5f;

        public override void EnterState(MonsterAIController monster)
        {
            attackTimer = 0f;
            monster.StopMoving();
            monster.SetProceduralMovement(false); // IK 비활성화

            if (Random.value > 0.5f)
            {
                monster.SetAnimTrigger(monster.hashAttack1);
            }
            // (참고) 거미의 2번째 공격 해시(hashAttack2)도 Config에 정의하고
            // else { monster.SetAnimTrigger(monster.hashAttack2); }
            // 처럼 사용하는 것을 권장합니다.


            if (monster.player != null) { monster.LookAt(monster.player.transform.position); }
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackAnimationLength)
            {
                // (수정) monster.fsm 사용
                return monster.fsm.TraceState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.SetProceduralMovement(true); // IK 다시 활성화
        }
    }

    public class Die : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.StopAllCoroutines();
            monster.SetProceduralMovement(false);

            // (참고) 거미 사망 애니메이션 해시
            // monster.SetAnimTrigger(monster.hashDie);

            if (monster.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            // (참고) 거미는 NavMeshAgent가 없으므로 체크할 필요 없음
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster) { return this; }
        public override void ExitState(MonsterAIController monster) { }
    }
}