using UnityEngine;

[CreateAssetMenu(fileName = "NewSpiderConfig", menuName = "Spider/Config", order = 1)]
public class SpiderConfig : ScriptableObject
{
    #region Core Stats
    [Header("기본 능력치")]
    public float maxHP = 100f;
    public float attackDamage = 10f;
    public float defense = 0f;
    public float currentHP;
    #endregion

    #region AI & Sensing
    [Header("AI 행동 및 감지")]
    public float fovRange = 10f;
    public float fovAngle = 120f;
    public float soundRange = 15f; // 소리 감지 범위
    public float attackRange = 2f;
    public float stoppingDistance = 1.5f;
    public float patrolRadiusMin = 5f;
    public float patrolRadiusMax = 10f;
    public float idleTimeMin = 2f;
    public float idleTimeMax = 4f;
    public float persistenceTime = 5f; // 추적 유지 시간
    #endregion

    #region Movement
    [Header("이동 관련")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3f;
    public float turnSpeed = 5f;
    public float looktime = 3f;
    public float lookAroundTime = 3f;
    public float lookAroundTurnInterval = 1.5f;
    #endregion

    #region Attack Pattern
    [Header("공격 패턴")]
    public float attackDelay = 0.5f; // 공격 선딜레이
    public float attackCooldown = 2f; // 공격 후딜레이 (쿨타임)
    #endregion

    #region Sound & Effects
    [Header("사운드 및 이펙트")]
    public AudioClip idleSound;
    public AudioClip chaseSound;
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip dieSound;
    public GameObject hitEffect;
    public GameObject dieEffect;
    #endregion

    #region Rewards
    [Header("보상")]
    public int experiencePoints = 50;
    public GameObject[] lootItems;
    #endregion
}