using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI; // ★★★★★ 1. NavMesh 사용을 위해 추가 ★★★★★

/// <summary>
/// 플레이어가 근접하면 몬스터를 '안전한 위치'에 스폰합니다.
/// 몬스터는 '목줄' 없이 플레이어를 끝까지 쫓아갑니다.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [Header("1. 기본 설정")]
    public GameObject monsterPrefab;
    private Transform playerTransform;

    [Header("2. 스폰 조건 (Trigger)")]
    public float triggerRadius = 20f;
    public bool disableSpawn = false;

    [Header("3. 스폰 방식 (Burst)")]
    public int spawnBurstCount = 3;
    public int maxAliveMonsters = 3;
    public float spawnCooldown = 15f;

    [Header("4. 스폰 위치")]
    public float spawnRadius = 2f;
    [Tooltip("몬스터가 서로 겹치지 않도록 보장하는 최소 반경")]
    public float spawnOverlapRadius = 1.0f; // 몬스터 캡슐의 반지름보다 약간 크게
    private LayerMask monsterLayerMask; // "Monster" 레이어

    // --- 내부 변수 ---
    private List<GameObject> spawnedMonsters = new List<GameObject>();
    private bool isSpawning = false;
    private bool playerInTriggerZone = false;

    private void Start()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("Monster Prefab이 할당되지 않았습니다!", this);
            disableSpawn = true;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // "Monster" 레이어 마스크를 가져옵니다. (끼임 방지용)
        monsterLayerMask = LayerMask.GetMask("Monster");

        StartCoroutine(SpawnCheckRoutine());
    }

    private IEnumerator SpawnCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            if (disableSpawn || isSpawning || playerTransform == null) continue;

            CleanupDeadMonsters();

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            playerInTriggerZone = (distanceToPlayer <= triggerRadius);

            if (playerInTriggerZone && spawnedMonsters.Count < maxAliveMonsters)
            {
                if (MonsterManager.Instance != null && !MonsterManager.Instance.CanSpawnMonster())
                {
                    Debug.Log($"[{name}] 글로벌 몬스터 한도 도달. 스폰 대기.");
                    continue;
                }
                StartCoroutine(SpawnBurst());
            }
        }
    }

    private IEnumerator SpawnBurst()
    {
        isSpawning = true;
        Debug.Log($"[{name}] 플레이어 감지! 몬스터 스폰을 시작합니다.");

        int spawnedCount = 0;
        int attemptCount = 0; // 무한 루프 방지

        while (spawnedCount < spawnBurstCount &&
               spawnedMonsters.Count < maxAliveMonsters &&
               attemptCount < 20) // 최대 20번만 시도
        {
            attemptCount++; // 시도 횟수 증가

            if (MonsterManager.Instance != null && !MonsterManager.Instance.CanSpawnMonster())
            {
                Debug.Log($"[{name}] 스폰 중 글로벌 한도 도달. 중지.");
                break;
            }

            // SpawnMonster() 대신 TrySpawnMonster() 호출
            bool success = TrySpawnMonster();

            if (success)
            {
                spawnedCount++; // 성공한 경우에만 카운트 증가
            }

            yield return null; // 1프레임 대기 (성공하든 실패하든)
        }

        Debug.Log($"[{name}] 스폰 완료. 쿨다운 ({spawnCooldown}초) 시작.");
        yield return new WaitForSeconds(spawnCooldown);
        isSpawning = false;
    }
    /// <summary>
    /// 안전한 NavMesh 위치에 몬스터 스폰을 '시도'하고 성공 여부를 반환합니다.
    /// </summary>
    /// <returns>스폰 성공 시 true, 실패 시 false</returns>
    private bool TrySpawnMonster()
    {
        // 1. 스포너 주변의 무작위 2D 위치 선정
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 randomPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // 2. (벽/하늘 방지) NavMesh 위에서 가장 가까운 유효한 지점 찾기
        // (spawnRadius의 절반 정도까지만 탐색)
        if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, spawnRadius * 0.5f, NavMesh.AllAreas))
        {
            Vector3 spawnPosition = hit.position;

            // 3. (끼임 방지) 해당 위치에 이미 다른 몬스터가 있는지 확인
            // (spawnOverlapRadius는 몬스터 캡슐의 반지름보다 약간 크게 설정)
            if (Physics.CheckSphere(spawnPosition, spawnOverlapRadius, monsterLayerMask))
            {
                // 이미 몬스터가 있음 -> 끼임 방지를 위해 스폰 실패
                Debug.LogWarning($"[{name}] 스폰 위치 ({spawnPosition})에 이미 다른 몬스터가 있어 스폰을 취소합니다.");
                return false;
            }

            // --- 몬스터 스폰 ---
            GameObject spawnedMonster = Instantiate(monsterPrefab, spawnPosition, transform.rotation);
            spawnedMonsters.Add(spawnedMonster);

            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.RegisterMonsterSpawned();
            }
            return true; // 스폰 성공
        }
        else
        {
            // NavMesh.SamplePosition이 유효한 위치를 찾지 못함 (예: 벽 속)
            Debug.LogWarning($"[{name}] 랜덤 위치 ({randomPosition}) 근처에 유효한 NavMesh가 없어 스폰에 실패했습니다.");
            return false; // 스폰 실패
        }
    }

    private void CleanupDeadMonsters()
    {
        for (int i = spawnedMonsters.Count - 1; i >= 0; i--)
        {
            if (spawnedMonsters[i] == null)
            {
                spawnedMonsters.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 0, 0.75f); // 트리거 (노랑)
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        Gizmos.color = new Color(0, 0, 1, 0.5f); // 스폰 (파랑)
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}