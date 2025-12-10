using UnityEngine;

namespace SlimeStates
{
    // 1. Idle (대기)
    public class Idle : ZombieBaseState<MonsterAIController>
    {
        private float idleTime;
        private float timer;
        private float soundTimer;
        private float soundInterval;

        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);

            idleTime = Random.Range(monster.config.idleTimeMin, monster.config.idleTimeMax);
            timer = 0f;

            PlayIdleSound(monster);
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer) return monster.fsm.TraceState;

            timer += Time.deltaTime;
            if (timer >= idleTime) return monster.fsm.PatrolState;

            soundTimer += Time.deltaTime;
            if (soundTimer >= soundInterval)
            {
                PlayIdleSound(monster);
            }
            return this;
        }

        private void PlayIdleSound(MonsterAIController monster)
        {
            // ★ 수정됨: 3D 사운드 재생 (내 몸에서 소리 나게)
            PlayLocalSound(monster, monster.config.idleSound);
            soundTimer = 0f;
            soundInterval = Random.Range(3.0f, 5.0f);
        }

        // ★ 3D 사운드 재생 도우미 함수
        private void PlayLocalSound(MonsterAIController monster, AudioClip clip)
        {
            if (clip == null) return;

            // 몬스터한테 AudioSource가 있는지 확인하고 재생
            if (monster.TryGetComponent<AudioSource>(out var source))
            {
                source.PlayOneShot(clip);
            }
            else
            {
                // 없으면 임시로 3D 위치에서 재생 (차선책)
                AudioSource.PlayClipAtPoint(clip, monster.transform.position);
            }
        }

        public override void ExitState(MonsterAIController monster) { }
    }

    // 2. Patrol (순찰)
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

            if (entryTimer > 0.1f && monster.arrivedAtDestination) return monster.fsm.IdleState;
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);
        }
    }

    // 3. Trace (추적 - 이동 소리)
    public class Trace : ZombieBaseState<MonsterAIController>
    {
        private float footstepTimer = 0f;
        private float footstepInterval = 0.5f;

        public override void EnterState(MonsterAIController monster)
        {
            monster.SetAnimFloat(monster.hashMoveSpeed, 2f);
            footstepTimer = 0f;

            // ★ 수정됨: 3D 사운드
            PlayLocalSound(monster, monster.config.chaseSound);
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                // ★ 수정됨: 3D 사운드
                PlayLocalSound(monster, monster.config.chaseSound);
                footstepTimer = 0f;
            }

            if (monster.GetDistanceToPlayer() <= monster.config.attackRange) return monster.fsm.AttackState;
            if (monster.TargetPlayer != null) monster.MoveTo(monster.TargetPlayer.transform.position);
            if (!monster.sensor.CanSeePlayer && monster.arrivedAtDestination) return monster.fsm.LookAroundState;
            return this;
        }

        private void PlayLocalSound(MonsterAIController monster, AudioClip clip)
        {
            if (clip == null) return;
            if (monster.TryGetComponent<AudioSource>(out var source)) source.PlayOneShot(clip);
            else AudioSource.PlayClipAtPoint(clip, monster.transform.position);
        }

        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
        }
    }

    // 4. Attack (공격)
    public class Attack : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        private bool hasAppliedDamage;

        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);
            if (monster.TargetPlayer != null) monster.LookAt(monster.TargetPlayer.transform.position);

            int attackIndex = Random.Range(0, 3);
            if (attackIndex == 0) monster.SetAnimTrigger(monster.hashAttack1);
            else if (attackIndex == 1) monster.SetAnimTrigger(monster.hashAttack2);
            else monster.SetAnimTrigger(monster.hashAttack3);

            timer = 0f;
            hasAppliedDamage = false;

            // ★ 수정됨: 3D 사운드
            PlayLocalSound(monster, monster.config.attackSound);
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
                if (Random.value > 0.5f) return monster.fsm.TauntState;
                return monster.fsm.TraceState;
            }
            return this;
        }

        private void PlayLocalSound(MonsterAIController monster, AudioClip clip)
        {
            if (clip == null) return;
            if (monster.TryGetComponent<AudioSource>(out var source)) source.PlayOneShot(clip);
            else AudioSource.PlayClipAtPoint(clip, monster.transform.position);
        }

        public override void ExitState(MonsterAIController monster) { }
    }

    // 5. LookAround (두리번)
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
            if (monster.sensor.CanSeePlayer) return monster.fsm.TraceState;
            timer += Time.deltaTime;
            if (timer >= monster.config.lookAroundTime) return monster.fsm.PatrolState;
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // 6. Hit (피격)
    public class Hit : ZombieBaseState<MonsterAIController>
    {
        private float hitStunDuration = 0.5f;
        private float timer;

        public override void EnterState(MonsterAIController monster)
        {
            timer = 0f;
            monster.StopMoving();
            if (monster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                agent.speed = monster.config.runSpeed * 0.3f;

            // ★ 수정됨: 3D 사운드
            PlayLocalSound(monster, monster.config.hitSound);
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            timer += Time.deltaTime;
            if (timer >= hitStunDuration) return monster.fsm.TraceState;
            return this;
        }

        private void PlayLocalSound(MonsterAIController monster, AudioClip clip)
        {
            if (clip == null) return;
            if (monster.TryGetComponent<AudioSource>(out var source)) source.PlayOneShot(clip);
            else AudioSource.PlayClipAtPoint(clip, monster.transform.position);
        }

        public override void ExitState(MonsterAIController monster) { }
    }

    // 7. Die (사망)
    public class Die : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.StopAllCoroutines();
            if (monster.TryGetComponent<Collider>(out var c)) c.enabled = false;
            if (monster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var a)) a.enabled = false;

            // ★ 수정됨: 3D 사운드
            PlayLocalSound(monster, monster.config.dieSound);

            MonsterHealth health = monster.GetComponent<MonsterHealth>();
            if (health != null && monster.config.lootTable != null)
                health.SpawnLoot(monster.config.lootTable);

            if (MonsterManager.Instance != null)
                MonsterManager.Instance.RegisterMonsterDied();

            // Object.Destroy는 MonsterAIController의 DespawnRoutine에서 중앙 관리됩니다.
            // Object.Destroy(monster.gameObject, monster.config.corpseDestroyDelay);
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController m) { return this; }

        private void PlayLocalSound(MonsterAIController monster, AudioClip clip)
        {
            if (clip == null) return;
            // 죽을 때는 AudioSource가 같이 파괴될 수 있으므로 PlayClipAtPoint가 안전함
            AudioSource.PlayClipAtPoint(clip, monster.transform.position);
        }

        public override void ExitState(MonsterAIController m) { }
    }

    // 8. Dodge (회피)
    public class DodgeState : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        private float dodgeAnimTime = 1.2f;

        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimTrigger(monster.hashTaunt);
            timer = 0f;

            // ★ 수정됨: 3D 사운드 (회피 소리)
            PlayLocalSound(monster, monster.config.attackSound);
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            timer += Time.deltaTime;
            if (timer >= dodgeAnimTime) return monster.fsm.TraceState;
            return this;
        }

        private void PlayLocalSound(MonsterAIController monster, AudioClip clip)
        {
            if (clip == null) return;
            if (monster.TryGetComponent<AudioSource>(out var source)) source.PlayOneShot(clip);
            else AudioSource.PlayClipAtPoint(clip, monster.transform.position);
        }

        public override void ExitState(MonsterAIController monster) { }
    }
}