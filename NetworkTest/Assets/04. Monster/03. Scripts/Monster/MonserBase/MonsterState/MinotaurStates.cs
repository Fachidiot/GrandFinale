using System.Collections;
using UnityEngine;

/*
 * [MinotaurStates.cs]
 * 미노타우로스의 AI 행동 패턴을 정의하는 클래스 모음입니다.
 * * 주요 흐름:
 * 1. Idle / Patrol / LookAround: 기본 배회 및 탐색
 * 2. Trace: 추적 중 첫 조우 시 'RamAttack(돌진)' 우선 수행. 근접 시 'CombatIdle'로 전환.
 * 3. CombatIdle: 플레이어 주위를 돌며(Strafe) 간보기. 상황에 따라 Dodge, Attack, RamAttack으로 분기.
 * 4. RamAttack: 돌진 공격. 벽 충돌(Wall)과 플레이어 충돌(Hit)을 구분하여 처리.
 * 5. Dodge: 백스탭 회피 후 다시 패턴 복귀.
 */

namespace MinotaurStates
{
    // 1. Idle (대기): 제자리에서 쉬다가 Patrol로 전환
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

    // 2. Patrol (순찰): 랜덤한 위치로 이동
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

            // 이동 시작 후 약간의 시간이 지난 뒤 도착 여부 체크
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

    // 3. Trace (추적): 첫 조우 시 돌진(Ram) 판단, 근접 시 간보기(CombatIdle) 전환
    public class Trace : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.SetAnimFloat(monster.hashMoveSpeed, 2f); // 뛰기
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            var minoFsm = monster.fsm as MinotaurFSM;
            float dist = monster.GetDistanceToPlayer();

            // [조건 1] 첫 돌진을 아직 안 했다면? -> 사거리(15m) 내 진입 시 무조건 돌진
            if (!monster.hasPerformedFirstCharge)
            {
                if (dist <= 15.0f)
                {
                    return minoFsm.RamAttackState;
                }
            }

            // [조건 2] 일반 공격 사거리 진입 시 -> 바로 공격하지 않고 'CombatIdle'로 전환
            if (dist <= monster.config.attackRange)
            {
                return minoFsm.CombatIdleState;
            }

            // 플레이어 추격
            Vector3 target = monster.sensor.CanSeePlayer ? monster.player.transform.position : monster.sensor.TargetLastPosition;
            monster.MoveTo(target, monster.config.runSpeed);

            return this;
        }
        public override void ExitState(MonsterAIController monster) { monster.StopMoving(); }
    }

    // 4. LookAround (주변 경계): 추적 실패 시 두리번거림
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

    // 5. CombatIdle (전투 대치): 플레이어를 보며 좌우 이동(Strafe) 및 패턴 분기
    public class CombatIdle : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        private float strafeTimer;
        private int strafeDir = 1; // 1: 오른쪽, -1: 왼쪽
        private MinotaurFSM minoFsm;

        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 1f); // 걷는 모션 (Strafe 연출용)
            timer = 0f;
            strafeTimer = 0f;
            strafeDir = Random.value > 0.5f ? 1 : -1;
            minoFsm = monster.fsm as MinotaurFSM;

            // 확률적으로 포효(Taunt)
            if (Random.value < 0.2f) monster.SetAnimTrigger(monster.hashTaunt);
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            MinotaurConfig mConfig = (MinotaurConfig)monster.config;
            timer += Time.deltaTime;
            strafeTimer += Time.deltaTime;

            // 1. 플레이어 주시 (회전)
            if (monster.player != null) monster.LookAt(monster.player.transform.position);

            // 2. 거리 체크: 너무 가까우면 -> Dodge(백스탭)으로 거리 벌리기
            float dist = monster.GetDistanceToPlayer();
            if (dist < mConfig.keepDistance)
            {
                return minoFsm.DodgeState;
            }

            // 3. 좌우 무빙 (Strafe) 로직
            if (strafeTimer >= mConfig.changeDirectionTime)
            {
                strafeDir *= -1; // 방향 전환
                strafeTimer = 0f;
            }

            // 플레이어 기준 좌우 벡터 계산
            Vector3 moveDir = monster.transform.right * strafeDir;
            monster.MoveDirection(moveDir, mConfig.strafeSpeed);

            // 4. 대치 시간이 끝나면 공격 패턴 결정
            if (timer >= mConfig.combatIdleTime)
            {
                // 거리가 멀면 다시 돌진, 가까우면 일반 공격
                if (dist > 6.0f) return minoFsm.RamAttackState;
                else return monster.fsm.AttackState;
            }

            return this;
        }
        public override void ExitState(MonsterAIController monster) { monster.StopMoving(); }
    }

    // 6. Dodge (회피): 백스탭 후 재정비
    public class Dodge : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            timer = 0f;
            monster.SetAnimTrigger(monster.hashDodge);
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            timer += Time.deltaTime;
            monster.LookAt(monster.player.transform.position);

            // 애니메이션 종료 대기 (약 1초)
            if (timer >= 1.0f)
            {
                var minoFsm = monster.fsm as MinotaurFSM;
                MinotaurConfig mConfig = (MinotaurConfig)monster.config;

                // 백스탭 후 확률적으로 바로 돌진 (기습)
                if (Random.value < mConfig.ramAfterDodgeChance)
                    return minoFsm.RamAttackState;

                return minoFsm.CombatIdleState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // 7. RamAttack (돌진 공격): 돌진 -> 충돌(플레이어/벽) -> 정지
    public class RamAttack : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        private bool isFinished;    // 충돌 여부
        private bool isPreparing;   // [신규] 방향 맞추는 중인지 체크
        private MinotaurConfig mConfig;

        public override void EnterState(MonsterAIController monster)
        {
            Debug.Log("RamAttack시작");
            mConfig = monster.config as MinotaurConfig;
            if (mConfig == null) return;

            // 1. 일단 멈춤 (방향부터 맞춰야 함)
            monster.StopMoving();

            isFinished = false;
            isPreparing = true; // "준비 중" 상태로 시작
            timer = 0f;

            monster.hasPerformedFirstCharge = true;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            var minoFsm = monster.fsm as MinotaurFSM;
            if (minoFsm == null || mConfig == null) return monster.fsm.IdleState;
            if (monster.player == null) return monster.fsm.IdleState;

            // --------------------------------------------------------
            // [단계 1] 준비 단계: 플레이어를 정면으로 볼 때까지 회전만 함
            // --------------------------------------------------------
            if (isPreparing)
            {
                // 플레이어 방향으로 아주 빠르게 회전 (기본 회전속도 * 5배)
                monster.LookAt(monster.player.transform.position, monster.config.turnSpeed * 5f);

                // 몬스터의 정면(forward)과 플레이어 방향 사이의 각도 계산
                Vector3 dirToPlayer = (monster.player.transform.position - monster.transform.position).normalized;
                float angle = Vector3.Angle(monster.transform.forward, dirToPlayer);

                // 각도가 5도 이내로 좁혀지면 (거의 정면을 보면) -> 돌진 시작
                if (angle < 5.0f)
                {
                    isPreparing = false; // 준비 끝
                    monster.SetAnimTrigger(monster.hashRamStart); // 애니메이션 1회 실행
                }               
            }

            // --------------------------------------------------------
            // [단계 2] 돌진 단계: 실제로 이동하고 충돌 체크
            // --------------------------------------------------------

            // 충돌 후 후딜레이 처리
            if (isFinished)
            {
                timer += Time.deltaTime;
                if (timer >= 2.0f)
                {
                    return minoFsm.CombatIdleState;
                }
                return this;
            }

            timer += Time.deltaTime;

            // 1. 벽 충돌 감지
            if (Physics.Raycast(monster.transform.position + Vector3.up, monster.transform.forward, out RaycastHit hit, mConfig.wallCheckDist))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    Debug.Log("벽 충돌!");
                    monster.SetAnimTrigger(monster.hashRamWall);
                    monster.StopMoving();
                    isFinished = true;
                    timer = 0f;
                    return this;
                }
            }

            // 2. 플레이어 충돌 감지
            float dist = monster.GetDistanceToPlayer();
            if (dist <= 1.5f)
            {
                Debug.Log("플레이어 충돌!");
                monster.ApplyDamageToPlayer(mConfig.chargeDamage);
                monster.SetAnimTrigger(monster.hashRamEnd);
                monster.StopMoving();
                isFinished = true;
                timer = 0f;
                return this;
            }

            // 3. 이동 (플레이어 방향으로 유도)
            monster.MoveTo(monster.player.transform.position, mConfig.chargeSpeed);

            // 4. 시간 초과 (아무것도 못 맞추고 시간이 다 됨)
            if (timer >= mConfig.chargeMaxDuration)
            {
                monster.SetAnimTrigger(monster.hashRamEnd);
                return minoFsm.CombatIdleState;
            }

            return this;
        }

        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
        }
    }


    // 8. Attack (일반 공격): 근접 3단 공격
    public class Attack : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);

            // 랜덤 공격 선택
            int r = Random.Range(0, 3);
            if (r == 0) monster.SetAnimTrigger(monster.hashAttack1);
            else if (r == 1) monster.SetAnimTrigger(monster.hashAttack2);
            else monster.SetAnimTrigger(monster.hashAttack3);

            if (monster.player != null) monster.LookAt(monster.player.transform.position);
            timer = 0f;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            timer += Time.deltaTime;
            // 공격 초반부에는 플레이어 방향 보정
            if (timer < 0.5f && monster.player != null) monster.LookAt(monster.player.transform.position);

            if (timer >= monster.config.attackCooldown)
            {
                // 공격 후에는 다시 간보기(CombatIdle) 상태로 복귀
                return ((MinotaurFSM)monster.fsm).CombatIdleState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }


    // 9. Hit (피격): 경직 처리
    public class Hit : ZombieBaseState<MonsterAIController>
    {
        private float hitStunDuration = 0.5f;
        private float timer;

        public override void EnterState(MonsterAIController monster)
        {
            timer = 0f;
            if (monster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
            {
                agent.speed = monster.config.runSpeed * 0.3f; // 이속 감소
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

    // 10. Die (사망): 죽음 처리 및 오브젝트 삭제
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

            Object.Destroy(monster.gameObject, monster.config.corpseDestroyDelay);
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }
}