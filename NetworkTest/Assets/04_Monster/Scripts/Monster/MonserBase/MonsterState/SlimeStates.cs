using UnityEngine;

namespace SlimeStates
{
    // --- 1. Idle ���� ---
    // (Locomotion 0)
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

    // --- 2. Patrol ���� ---
    // (Locomotion 1)
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

    // --- 3. Trace ���� ---
    // (Locomotion 2)
    public class Trace : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.SetAnimFloat(monster.hashMoveSpeed, 2f); // Locomotion 2 (Run)
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
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
            // (StopMoving�� Attack/LookAround ���°� �ϹǷ� ���⼭ �� ��)
        }
    }

    // --- 4. (����) ���� ���� ���� ---
    // (attack1, 2, 3 �� ����)
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

            // (��û) Attack 1~3 �� �ϳ��� �������� ����
            int attackIndex = Random.Range(0, 3); // 0, 1, 2

            if (attackIndex == 0)
                monster.SetAnimTrigger(monster.hashAttack1);
            else if (attackIndex == 1)
                monster.SetAnimTrigger(monster.hashAttack2);
            else
                monster.SetAnimTrigger(monster.hashAttack3);

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
                if (Random.value > 0.5f)
                {
                    return monster.fsm.TauntState; // (DodgeState)
                }
                return monster.fsm.TraceState;
            }

            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 5. LookAround ���� (���߱�) ---
    public class LookAround : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            // (��û) LookAround �ؽ�(idleBreak) �ߵ�
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

    // --- 6. Hit ���� ---
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

    // --- 7. Die ���� ---
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

    // --- 8. (�ű�) ȸ�� ���� ---
    public class DodgeState : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        private float dodgeAnimTime = 1.2f; // (Dodge �ִϸ��̼� ���̿� �°� ����)

        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            // (Taunt ���Կ� ����� 'dodge' Ʈ���� �ߵ�)
            monster.SetAnimTrigger(monster.hashTaunt);
            timer = 0f;
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            timer += Time.deltaTime;
            // �ִϸ��̼� �ð��� ������ ���� ���·� ����
            if (timer >= dodgeAnimTime)
            {
                return monster.fsm.TraceState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }
}