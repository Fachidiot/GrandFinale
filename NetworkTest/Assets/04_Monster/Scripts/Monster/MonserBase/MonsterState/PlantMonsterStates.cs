using UnityEngine;

namespace PlantMonsterStates
{

    public static class PlantAnimHashes
    {
        public static readonly int goAlive = Animator.StringToHash("goAlive");
        public static readonly int goPlant = Animator.StringToHash("goPlant");
        public static readonly int castStart = Animator.StringToHash("castStart");
        public static readonly int castEnd = Animator.StringToHash("castEnd");
    }

    // --- 1. ���� ���� (FSM�� IdleState ����) ---
    public class Plant_HidingState : ZombieBaseState<MonsterAIController>
    {
        private PlantMonsterConfig plantConfig;

        // (����) HidingState�� �ִ� ���ʿ��� RangedAttack �������� �����߽��ϴ�.

        public override void EnterState(MonsterAIController monster)
        {
            if (plantConfig == null)
                plantConfig = monster.config as PlantMonsterConfig;

            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f); // Locomotion 0

            // �� (����) EnterState������ goPlant�� ȣ������ �ʽ��ϴ�.
            // monster.SetAnimTrigger(PlantAnimHashes.goPlant); 
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (plantConfig == null) return this;

            if (monster.GetDistanceToPlayer() <= plantConfig.activationRange)
            {
                // �� (����) ���� ���·� ��ȯ "����"�� goAlive �ִϸ��̼��� Ʈ�����մϴ�.
                monster.SetAnimTrigger(PlantAnimHashes.goAlive);
                return monster.fsm.TraceState; // -> Plant_AliveState
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }
    // --- 2. ���� ��� ���� (FSM�� TraceState ����) ---
    public class Plant_AliveState : ZombieBaseState<MonsterAIController>
    {
        private PlantMonsterConfig plantConfig;
        private PlantMonsterFSM plantFSM;
        private float checkTimer = 0f;
        private float persistenceTimer = 0f;

        public override void EnterState(MonsterAIController monster)
        {
            // �� (���� ����) Config �� FSM ĳ����
            if (plantConfig == null)
                plantConfig = monster.config as PlantMonsterConfig;
            if (plantFSM == null)
                plantFSM = monster.fsm as PlantMonsterFSM;

            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f); // Locomotion 0
            persistenceTimer = 0f;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (plantConfig == null || plantFSM == null) return this;

            checkTimer += Time.deltaTime;

            if (monster.player != null)
                monster.LookAt(monster.player.transform.position);

            // �÷��̾� ���� üũ
            if (monster.sensor.CanSeePlayer)
            {
                persistenceTimer = 0f;
            }
            else
            {
                persistenceTimer += Time.deltaTime;
                if (persistenceTimer >= monster.config.persistenceTime)
                {
                    monster.SetAnimTrigger(PlantAnimHashes.goPlant);
                    return monster.fsm.IdleState; // -> Plant_HidingState
                }
            }

            // (�� ����) 1�ʸ��� ���� ���� ����
            if (checkTimer >= 1.0f)
            {
                checkTimer = 0f;
                float distance = monster.GetDistanceToPlayer();

                // (��û) �켱���� 1: ���Ÿ� ���� (��ٿ� �ƴ� ��)
                if (distance <= plantConfig.rangedAttackRange && !plantFSM.IsRangedAttackOnCooldown)
                {
                    return monster.fsm.LookAroundState; // -> Plant_RangedAttackState
                }

                // (��û) �켱���� 2: ���� ����
                if (distance <= monster.config.attackRange)
                {
                    return monster.fsm.AttackState; // -> Plant_MeleeAttackState
                }

                // (��û) �켱���� 3: ���� ����
                if (distance <= plantConfig.jumpRange)
                {
                    return monster.fsm.PatrolState; // -> Plant_LeapState
                }

                // (����) 4����: ��� ���� ���̸� �ȱ� (locomotion 1)
                // (�� ������ FSM ������ �����Ͽ� ����� ��� ���·� ������)
            }

            return this; // ���� ��� ���� ����
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 3. ���� ���� ���� (FSM�� PatrolState ����) ---
    public class Plant_LeapState : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            // (AnimConfig�� 'tauntTrigger'�� 'jump'�� ����)
            monster.SetAnimTrigger(monster.hashTaunt);
            // (���� �ִϸ��̼��� Root Motion���� �̵���Ų�ٰ� ����)
            monster.SetAnimFloat(monster.hashMoveSpeed, 1f);
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            // (Root Motion�� ���ٸ� NavMesh�� �̵�)
            if (monster.player != null)
                monster.MoveTo(monster.player.transform.position);

            // (��û) "Jump -> Attack" ����
            // ���� ���� �������� �����ϸ� '���� ���'�� �ƴ� '���� ����' ���·� �ٷ� ��ȯ
            if (monster.GetDistanceToPlayer() <= monster.config.attackRange)
            {
                return monster.fsm.AttackState; // -> Plant_MeleeAttackState
            }

            // (����) ���� �ִϸ��̼��� ������ ��� ���·� ���ư��� �Ϸ���
            // �ִϸ��̼� ���Ḧ �����ϴ� ������ �ʿ��� (������ ����)

            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);
        }
    }

    // --- 4. ���� ���� ���� (FSM�� AttackState ����) ---
    public class Plant_MeleeAttackState : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        private bool hasAppliedDamage;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();

            // �� (����) attack 1~3 �� ����
            int attackIndex = Random.Range(0, 2);
            if (attackIndex == 0)
                monster.SetAnimTrigger(monster.hashAttack1);
            else if (attackIndex == 1)
                monster.SetAnimTrigger(monster.hashAttack2);

            if (monster.player != null)
                monster.LookAt(monster.player.transform.position);
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
                return monster.fsm.TraceState; // -> Plant_AliveState
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 5. ���Ÿ� ���� ���� (FSM�� LookAroundState ����) ---
    public class Plant_RangedAttackState : ZombieBaseState<MonsterAIController>
    {
        private PlantMonsterConfig plantConfig;
        private PlantMonsterFSM plantFSM;
        private float timer;
        private int shotsFired;
        private int totalShots;
        private bool isCharging;

        public override void EnterState(MonsterAIController monster)
        {
            if (plantConfig == null)
                plantConfig = monster.config as PlantMonsterConfig;
            if (plantFSM == null)
                plantFSM = monster.fsm as PlantMonsterFSM;

            monster.StopMoving();
            monster.SetAnimTrigger(PlantAnimHashes.castStart);

            timer = 0f;
            shotsFired = 0;
            totalShots = Random.Range(3, 6);
            isCharging = true;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (plantConfig == null || plantFSM == null) return this;

            timer += Time.deltaTime;

            if (isCharging)
            {
                if (timer >= plantConfig.castTime)
                {
                    isCharging = false;
                    FireShot(monster);
                }
            }
            else if (shotsFired < totalShots)
            {
                if (timer >= plantConfig.timeBetweenShots)
                {
                    FireShot(monster);
                }
            }
            else
            {
                plantFSM.StartRangedCooldown(30f);
                return monster.fsm.TraceState; // -> Plant_AliveState
            }
            return this;
        }

        private void FireShot(MonsterAIController monster)
        {
            monster.SetAnimTrigger(PlantAnimHashes.castEnd);

            if (plantConfig.projectilePrefab == null || monster.firePoint == null || monster.player == null)
            {
                Debug.LogError("Projectile Fire Failed: Config, FirePoint, or Player is missing!");
                return;
            }

            GameObject projectile = Object.Instantiate(
                plantConfig.projectilePrefab,
                monster.firePoint.position,
                Quaternion.identity
            );

            Vector3 targetPosition = monster.player.transform.position;
            Projectile_Arc arcScript = projectile.GetComponent<Projectile_Arc>();

            if (arcScript != null)
            {
                arcScript.Setup(plantConfig);

                arcScript.Initialize(
                    targetPosition,
                    plantConfig.projectileArcHeight,
                    plantConfig.projectileSpeed
                );
            }
            else
            {
                Debug.LogError($"{monster.name} projectilePrefab�� Projectile_Arc.cs ��ũ��Ʈ�� �����ϴ�!");
            }

            Debug.Log($"[{monster.name}] ���Ÿ� {shotsFired + 1} / {totalShots} ��° �߻�!");
            shotsFired++;
            timer = 0f;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 6. �ǰ� ���� (FSM�� HitState ����) ---
    public class Plant_HitState : ZombieBaseState<MonsterAIController>
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
                return monster.fsm.TraceState; // (�ǰ� �� ������ ���� ���·�)
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 7. ��� ���� (FSM�� DieState ����) ---
    public class Plant_DieState : ZombieBaseState<MonsterAIController>
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