using UnityEngine;

// 1. MonsterConfig를 상속받습니다.
[CreateAssetMenu(fileName = "NewPlantConfig", menuName = "Monster/Plant Monster Config")]
public class PlantMonsterConfig : MonsterConfig
{
    // 2. PlantMonster만 사용하는 고유 변수들을 여기에 추가합니다.
    [Header("Plant Monster (Hide)")]
    [Tooltip("이 범위 안으로 플레이어가 들어오면 활성화(goAlive)됩니다.")]
    public float activationRange = 15f;

    [Header("Plant Monster (Attack)")]
    [Tooltip("이 거리보다 멀면 LeapState(돌진)를 시도합니다.")]
    public float jumpRange = 10f;
    [Tooltip("이 거리 안이면 RangedAttackState(원거리)를 시도합니다.")]
    public float rangedAttackRange = 8f;
    [Tooltip("원거리 공격 시 기를 모으는 시간 (castStart)")]
    public float castTime = 2.0f;
   [Tooltip("다중 발사 시, 각 발사 사이의 시간 간격")]
    public float timeBetweenShots = 1.2f;
    [Header("Projectile")]
    [Tooltip("원거리 공격 시 발사할 투사체 프리팹")]
    public GameObject projectilePrefab;
    [Header("Poison Attack (투사체 독 공격)")]
    [Tooltip("투사체 또는 충돌 이펙트(Impact Effect)에 의한 단발 피해량")]
    public float impactDamage = 20f;
    [Tooltip("바닥에 은은하게 깔리는 독 장판 프리팹 (PoisonArea.cs 스크립트 포함)")]
    public GameObject poisonAreaPrefab;
    [Tooltip("독 장판에 의한 틱 피해량")]
    public float tickDamage = 5f;
    [Tooltip("독 장판의 지속 시간 (초)")]
    public float poisonDuration = 5.0f;
    [Tooltip("독 장판 틱 피해 적용 간격 (초)")]
    public float poisonTickRate = 1.0f; // 1초마다 피해를 준다고 가정
    [Header("Projectile Arc")]
    [Tooltip("포물선의 최대 높이")]
    public float projectileArcHeight = 5.0f;
    [Tooltip("포물선 투사체의 속도")]
    public float projectileSpeed = 6.0f;

}