using UnityEngine;

/// <summary>
/// 모든 몬스터가 공통으로 사용할 최종 설정 파일입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewMonsterConfig", menuName = "Monster/Monster Config")]
public class MonsterConfig : ScriptableObject
{
    [Header("기본 능력치")]
    public float maxHP = 100f;
    public float attackDamage = 10f;
    public float defense = 0f;
    // 'currentHP'는 이제 MonsterHealth.cs가 관리하므로 여기서 제거합니다.

    [Header("AI 행동 및 감지")]
    public float fovRange = 10f;
    [Range(0, 360)]
    public float fovAngle = 120f;
    public float soundRange = 15f;
    public float attackRange = 2f;
    public float stoppingDistance = 1.5f;
    public float persistenceTime = 5f; // 추적 유지 시간 (SpiderConfig에서 가져옴)

    [Header("순찰 및 대기")]
    public float patrolRadiusMin = 5f;
    public float patrolRadiusMax = 10f;
    public float idleTimeMin = 10f;
    public float idleTimeMax = 15f;

    [Header("주변 둘러보기")]
    public float lookAroundTime = 3f; // looktime 대신 lookAroundTime으로 통일
    public float lookAroundTurnInterval = 1.5f;

    [Header("이동 관련")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3f;
    public float turnSpeed = 2000f;

    [Header("공격 패턴")]
    public float attackDelay = 0.5f;
    public float attackCooldown = 10f;

    [Header("사운드 및 이펙트 (SpiderConfig에서 가져옴)")]
    public AudioClip idleSound;
    public AudioClip chaseSound;
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip dieSound;
    public GameObject hitEffect;
    public GameObject dieEffect;

    /*    [Header("보상 (SpiderConfig에서 가져옴)")]
        public int experiencePoints = 50;
        public GameObject[] lootItems;
    */
    [Header("보상 (LootTable 가져옴)")]
    public LootTable lootTable;

    [Header("사망 후 처리")]
    [Tooltip("몬스터가 죽은 후 시체가 사라지기까지 걸리는 시간 (초)")]
    public float corpseDestroyDelay = 5.0f; // 5초 뒤 사라짐
}