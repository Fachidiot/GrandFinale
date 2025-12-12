using UnityEngine;

public class Projectile_Arc : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float arcHeight;
    private float travelTime;
    private float timer = 0f;

    // Config에서 받아올 데이터
    private float impactDamage;
    private float poisonDuration;
    private float poisonTickDamage;
    private float poisonTickRate;
    private GameObject poisonAreaPrefab;

    [Header("이펙트 관련 (인스펙터)")]
    [Tooltip("발사체 본체에 붙일 이펙트")]
    public GameObject poisonPrefab;
    [Tooltip("땅(Ground)으로 인식할 LayerMask")]
    public LayerMask groundLayer;
    [Tooltip("몬스터(Monster)으로 인식할 LayerMask")]
    public LayerMask monsterLayer;

    // [추가] 독 이펙트 인스턴스를 저장할 변수
    private GameObject poisonInstance;

    void Awake()
    {
        if (poisonPrefab != null)
        {
            // [추가] 시작시 이펙트를 생성하여 저장합니다.
            poisonInstance = Instantiate(poisonPrefab, transform.position, transform.rotation);
            poisonInstance.transform.SetParent(transform, false);
        }
    }

    public void Initialize(Vector3 target, float height, float speed)
    {
        startPos = transform.position;
        targetPos = target;
        arcHeight = height;
        float distance = Vector3.Distance(startPos, targetPos);
        if (speed <= 0) speed = 15f;
        travelTime = distance / speed;
        Destroy(gameObject, 5.0f); // 5초 후 자동 파괴 (안전장치)
    }

    void Update()
    {
        if (travelTime <= 0) return;
        timer += Time.deltaTime;
        float t = timer / travelTime;
        if (t > 1f) t = 1f;
        Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
        float arc = 4 * arcHeight * t * (1 - t);
        pos.y += arc;
        transform.position = pos;
    }

    public void Setup(PlantMonsterConfig config)
    {
        this.impactDamage = config.impactDamage;
        this.poisonDuration = config.poisonDuration;
        this.poisonTickDamage = config.tickDamage;
        this.poisonTickRate = config.poisonTickRate;
        this.poisonAreaPrefab = config.poisonAreaPrefab;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Projectile] 트리거 감지! 대상: {other.gameObject.name}, 레이어: {LayerMask.LayerToName(other.gameObject.layer)}");

        bool isGround = ((1 << other.gameObject.layer) & groundLayer) != 0;
        bool isMonster = ((1 << other.gameObject.layer) & monsterLayer) != 0;
        bool isPlayer = other.CompareTag("Player");

        if (isGround)
        {
            Debug.Log($"[Projectile] 'Ground' 레이어에 충돌.");
            HandleGroundHit(transform.position);
        }
        else if (isPlayer)
        {
            Debug.Log($"[Projectile] 'Player'에 충돌.");
            HandlePlayerHit(other.gameObject);
        }
        else if (isMonster)
        {
            Debug.Log($"[Projectile] 'Monster' 레이어에 충돌.");
            HandleGroundHit(other.transform.position);
        }
        else
        {
            Debug.Log($"[Projectile] 기타 대상({other.gameObject.name})과 충돌. 소멸합니다.");
        }

        // 땅, 플레이어, 또는 '일반' 콜라이더에 닿으면 발사체는 파괴되어야 합니다.
        if (isGround || isPlayer || !other.isTrigger) // (Trigger가 아닌 Collider = 벽)
        {
            // [추가] DetachChildren() 대신, 독 이펙트를 분리하는 새 함수 호출
            HandleParticleStop();
            Destroy(gameObject); // 발사체 파괴
        }
    }

    private void HandlePlayerHit(GameObject target)
    {
        if (target.TryGetComponent<PlayerStats>(out var playerStats))
        {
            playerStats.TakeDamage(impactDamage);
            Debug.Log($"[Projectile] {target.name}에게 직격 데미지 {impactDamage} 적용!");
        }
    }

    private void HandleGroundHit(Vector3 impactPosition)
    {
        if (poisonAreaPrefab != null)
        {
            Debug.Log($"[HandleGroundHit] 'poisonAreaPrefab'({poisonAreaPrefab.name})을 생성합니다.");
            GameObject poisonArea = Instantiate(
                poisonAreaPrefab,
                impactPosition + Vector3.up * 0.1f,
                poisonAreaPrefab.transform.rotation
            );

            if (poisonArea.TryGetComponent<PoisonArea>(out var areaScript))
            {
                areaScript.Initialize(poisonTickDamage, poisonDuration, poisonTickRate);
            }
            else
            {
                Debug.LogError($"[Projectile_Arc] {poisonAreaPrefab.name}에 PoisonArea.cs 스크립트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("[HandleGroundHit] 이펙트 생성 실패: poisonAreaPrefab이 null입니다. (Config 설정을 확인하세요)");
        }
    }

    /// <summary>
    /// [신규] 발사체에 붙어있는 파티클(독)을 분리하고 소멸시킵니다.
    /// </summary>
    private void HandleParticleStop()
    {
        if (poisonInstance != null)
        {
            // 1. 부모-자식 관계 해제 (발사체가 사라져도 이펙트는 남도록)
            poisonInstance.transform.SetParent(null);

            // 2. 파티클 시스템 찾기
            if (poisonInstance.TryGetComponent<ParticleSystem>(out var ps))
            {
                // 3. 더 이상 파티클 생성 중지
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            // 4. 파티클이 사라질 시간(5초)이 지난 후, 이펙트 오브젝트 자체를 파괴
            Destroy(poisonInstance, 5.0f);
        }
    }
}