using Newtonsoft.Json.Linq;
using UnityEngine;

public class BulletNetwork : BulletBehaviour
{
    private string weaponName;
    private float PlayerDamage;
    public float lifeTime;
    private Vector3 _startPoint;
    public float startSpeed;
    public float force = 1;
    public GameObject decalPrefab;
    public GameObject bloodPrefab;
    public LayerMask mask; // Raycast Ignored Layers;

    private Rigidbody rb;
    private bool _isPooled = false;

    protected override void Awake()
    {
        base.Awake(); // Call the base class Awake method
        rb = GetComponent<Rigidbody>();

        if (_isPooled && PoolManager.Instance != null)
        {
            if (decalPrefab != null) PoolManager.Instance.CreatePool(decalPrefab, 10);
            if (bloodPrefab != null) PoolManager.Instance.CreatePool(bloodPrefab, 10);
        }
    }

    private void OnEnable()
    {
        // // Reset physics state when taken from pool
        // if (rb != null)
        // {
        //     rb.velocity = Vector3.zero;
        //     rb.angularVelocity = Vector3.zero;
        // }

        // // Automatically return to pool after lifetime expires
        // if (_isPooled && PoolManager.Instance != null)
        // {
        //     PoolManager.Instance.ReturnToPool(gameObject, lifeTime);
        // }
        // else
        // {
        //     Destroy(gameObject, lifeTime);
        // }
    }

    public override void BulletStart(Transform bulletCreator)
    {
        var weap = bulletCreator.GetComponent<RangedWeapon>();

        GetComponent<NetworkTransformSync>().IsMine = weap.transform.root.GetComponent<NetworkTransformSync>().IsMine;
        PlayerDamage = weap.PlayerDamage;
        force = weap.BulletForce;
        startSpeed = weap.BulletStartSpeed;
        weaponName = weap.gameObject.name;

        // Apply force and set start point now that parameters are initialized
        if (rb != null)
        {
            rb.AddForce(transform.forward * startSpeed, ForceMode.Impulse);
        }
        _startPoint = transform.position;

        // Handle lifetime based on whether it's pooled or not
        if (_isPooled)
        {
            PoolManager.Instance.ReturnToPool(gameObject, lifeTime);
        }
        else
        {
            Destroy(gameObject, lifeTime);
        }
    }

    void Update()
    {
        if (Physics.Linecast(_startPoint, transform.position, out RaycastHit hit, mask))
        {
            // Decal and blood effects spawning logic remains the same...
            if (decalPrefab && !hit.transform.CompareTag("HitBox"))
            {
                SpawnEffect(decalPrefab, hit, 15f);
            }

            if (bloodPrefab && hit.transform.CompareTag("HitBox"))
            {
                SpawnEffect(bloodPrefab, hit, 3f);
            }

            if (GetComponent<NetworkTransformSync>().IsMine)
            {
                if (hit.collider.CompareTag("HitBox"))
                {
                    if (hit.transform.root.CompareTag("Player"))
                    {// 팀킬시.
                        /* RPC Photon 예시 코드 : 
                        hit.transform.root.GetComponent<PhotonView>().RPC("DamageRPC", RpcTarget.All, PlayerDamage *= hit.collider.name == "Head" ? 2 : 1, photonView.ViewID, hit.collider.name == "Head", weaponName); */
                        // TODO : 같은 Player가 맞았을때 해당 네트워크 플레이어의 HP 감소.
                        // hit.transform.root.GetComponent<PlayerHealth>().SetDamage(PlayerDamage *= hit.collider.name == "Head" ? 2 : 1);
                    }
                    else
                    {// 몬스터 공격시.
                        var networkMonster = hit.transform.root.GetComponent<NetworkMonster>();
                        if (networkMonster != null)
                        {
                            float damage = PlayerDamage * (hit.collider.name == "Head" ? 2f : 1f);

                            // 클라이언트는 직접 데미지를 주지 않고, 서버에 데미지 요청을 보냅니다.
                            if (NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.Client)
                            {
                                JObject damageData = new JObject();
                                damageData["type"] = "player_dealt_damage";
                                damageData["monsterId"] = networkMonster.MonsterId;
                                damageData["damage"] = damage;

                                NetworkManager.Instance.SendJsonMessage(NetworkManager.Instance.LobbyHostID, damageData);
                            }
                            else if (NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.Host)
                            {
                                // 호스트는 직접 데미지를 처리합니다.
                                var monsterHealth = hit.transform.root.GetComponent<MonsterHealth>();
                                if (monsterHealth != null)
                                {
                                    monsterHealth.TakeDamage(damage);
                                }
                            }
                        }
                    }
                }

            }

            if (hit.rigidbody)
                hit.rigidbody.AddForceAtPosition(force * transform.forward, hit.point);

            // Deactivate or destroy the bullet
            Deactivate();
        }

        _startPoint = transform.position;
    }

    private void SpawnEffect(GameObject prefab, RaycastHit hit, float effectLifetime)
    {
        if (prefab == null) return;

        GameObject effectGO;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        Vector3 position = hit.point + (hit.normal * 0.001f);

        if (_isPooled && PoolManager.Instance != null)
        {
            effectGO = PoolManager.Instance.Spawn(prefab, position, rotation);
            if (effectGO != null) PoolManager.Instance.ReturnToPool(effectGO, effectLifetime);
        }
        else
        {
            effectGO = Instantiate(prefab, position, rotation);
            Destroy(effectGO, effectLifetime);
        }
    }

    private void Deactivate()
    {
        if (_isPooled && PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
