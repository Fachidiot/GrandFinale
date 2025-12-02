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

    // --- 1. 숨기 상태 (FSM의 IdleState 역할) ---
    public class Plant_HidingState : ZombieBaseState<MonsterAIController>
    {
        private PlantMonsterConfig plantConfig;

        // (참고) HidingState에 있는 동안에는 RangedAttack 쿨다운이 초기화되었습니다.

        public override void EnterState(MonsterAIController monster)
        {
            if (plantConfig == null)
                plantConfig = monster.config as PlantMonsterConfig;

            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f); // Locomotion 0

            // 주석 (중요) EnterState에서는 goPlant를 호출하지 않아야 합니다.
            // monster.SetAnimTrigger(PlantAnimHashes.goPlant); 
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (plantConfig == null) return this;

            if (monster.GetDistanceToPlayer() <= plantConfig.activationRange)
            {
                // 주석 (중요) 살아있는 상태로 전환 "직전"에 goAlive 애니메이션을 트리거합니다.
                monster.SetAnimTrigger(PlantAnimHashes.goAlive);
                return monster.fsm.TraceState; // -> Plant_AliveState
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }
    // --- 2. 살아있는 상태 (FSM의 TraceState 역할) ---
    public class Plant_AliveState : ZombieBaseState<MonsterAIController>
    {
        private PlantMonsterConfig plantConfig;
        private PlantMonsterFSM plantFSM;
        private float checkTimer = 0f;
        private float persistenceTimer = 0f;

        public override void EnterState(MonsterAIController monster)
        {
            // 주석 (성능 개선) Config 와 FSM 캐싱
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

            // 플레이어 감지 체크
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

            // (성능 개선) 1초마다 행동 결정
            if (checkTimer >= 1.0f)
            {
                checkTimer = 0f;
                float distance = monster.GetDistanceToPlayer();

                // (요청) 우선순위 1: 원거리 공격 (쿨타임이 아닐 때)
                if (distance <= plantConfig.rangedAttackRange && !plantFSM.IsRangedAttackOnCooldown)
                {
                    return monster.fsm.LookAroundState; // -> Plant_RangedAttackState
                }

                // (요청) 우선순위 2: 근접 공격
                if (distance <= monster.config.attackRange)
                {
                    return monster.fsm.AttackState; // -> Plant_MeleeAttackState
                }

                // (요청) 우선순위 3: 도약 공격
                if (distance <= plantConfig.jumpRange)
                {
                    return monster.fsm.PatrolState; // -> Plant_LeapState
                }

                // (수정) 4순위: 아무것도 해당 안되면 대기 (locomotion 1)
                // (주석 처리된 FSM 로직을 사용하여 이동하는 등 다른 상태로 변경 가능)
            }

            return this; // 현재 상태 유지
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 3. 도약 공격 상태 (FSM의 PatrolState 역할) ---
    public class Plant_LeapState : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            // (AnimConfig의 'tauntTrigger'를 'jump'로 사용)
            monster.SetAnimTrigger(monster.hashTaunt);
            // (점프 애니메이션이 Root Motion으로 이동시킨다고 가정)
            monster.SetAnimFloat(monster.hashMoveSpeed, 1f);
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            // (Root Motion이 없다면 NavMesh로 이동)
            if (monster.player != null)
                monster.MoveTo(monster.player.transform.position);

            // (요청) "Jump -> Attack" 연계
            // 점프 공격 도중에라도 가까워지면 '숨기 상태'가 아닌 '근접 공격' 상태로 바로 전환
            if (monster.GetDistanceToPlayer() <= monster.config.attackRange)
            {
                return monster.fsm.AttackState; // -> Plant_MeleeAttackState
            }

            // (참고) 점프 애니메이션이 끝나면 살아있는 상태로 돌아가야 한다면
            // 애니메이션 이벤트를 사용하는 방식이 필요함 (현재는 없음)

            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);
        }
    }

    // --- 4. 근접 공격 상태 (FSM의 AttackState 역할) ---
    public class Plant_MeleeAttackState : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        private bool hasAppliedDamage;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();

            // 주석 (수정) attack 1~3 중 랜덤
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

    // --- 5. 원거리 공격 상태 (FSM의 LookAroundState 역할) ---
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
                Debug.LogError($"{monster.name}의 projectilePrefab에 Projectile_Arc.cs 스크립트가 없습니다!");
            }

            Debug.Log($"[{monster.name}] 원거리 {shotsFired + 1} / {totalShots} 번째 발사!");
            shotsFired++;
            timer = 0f;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 6. 피격 상태 (FSM의 HitState 역할) ---
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
                return monster.fsm.TraceState; // (피격 후 살아있는 상태로)
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 7. 죽음 상태 (FSM의 DieState 역할) ---
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
            
            monster.StartCoroutine(DieRoutine(monster));
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController m) { return this; }
        public override void ExitState(MonsterAIController m) { }
        
        private System.Collections.IEnumerator DieRoutine(MonsterAIController monster)
        {
            yield return new WaitForSeconds(monster.config.corpseDestroyDelay);
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.ReturnMonsterToPool(monster.gameObject);
            }
            else
            {
                Object.Destroy(monster.gameObject);
            }
        }
    }
}