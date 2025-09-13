using UnityEngine;

public class BulletNetwork : BulletBehaviour
{
    // private PhotonView photonView;
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.CreatePool(decalPrefab, 10);
            PoolManager.Instance.CreatePool(bloodPrefab, 10);
        }
    }

    private void OnEnable()
    {
        // Reset physics state when taken from pool
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Automatically return to pool after lifetime expires
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject, lifeTime);
        }
        else
        {
            Destroy(gameObject, lifeTime);
        }
    }

    public override void BulletStart(Transform bulletCreator)
    {
        var weap = bulletCreator.GetComponent<Weapon>();

        // photonView = bulletCreator.root.GetComponent<PhotonView>();
        PlayerDamage = weap.PlayerDamage;
        force = weap.BulletForce;
        startSpeed = weap.BulletStartSpeed;
        weaponName = bulletCreator.name;

        // Apply force and set start point now that parameters are initialized
        if (rb != null)
        {
            rb.AddForce(transform.forward * startSpeed, ForceMode.Impulse);
        }
        _startPoint = transform.position;
    }

    void Update()
    {
        if (Physics.Linecast(_startPoint, transform.position, out RaycastHit hit, mask))
        {
            // Spawn decals from pool
            if (decalPrefab && hit.transform.CompareTag("HitBox"))
            {
                if (PoolManager.Instance != null)
                {
                    var decal = PoolManager.Instance.Spawn(decalPrefab, hit.point + (hit.normal * 0.001f), Quaternion.FromToRotation(Vector3.up, hit.normal));
                    if (decal != null)
                    {
                        decal.transform.SetParent(hit.transform);
                        PoolManager.Instance.ReturnToPool(decal, 15f);
                    }
                }
            }

            // Spawn blood effects from pool
            if (bloodPrefab && hit.transform.CompareTag("Entity"))
            {
                if (PoolManager.Instance != null)
                {
                    var blood = PoolManager.Instance.Spawn(bloodPrefab, hit.point + (hit.normal * 0.001f), Quaternion.FromToRotation(Vector3.up, hit.normal));
                    if (blood != null)
                    {
                        blood.transform.SetParent(hit.transform);
                        PoolManager.Instance.ReturnToPool(blood, 3f);
                    }
                }
            }

            // if (photonView.IsMine)
            // {

            //     // add force for rigid body hit
            //     if (hit.collider.CompareTag("HitBox") && hit.transform.root.CompareTag("Player"))
            //     {
            //         hit.transform.root.GetComponent<PhotonView>().RPC("DamageRPC", RpcTarget.All, PlayerDamage *= hit.collider.name == "Head" ? 3 : 1, photonView.ViewID, hit.collider.name == "Head", weaponName);
            //     }
            // }

            if (hit.rigidbody)
            {
                hit.rigidbody.AddForceAtPosition(force * transform.forward, hit.point);
            }

            // Return bullet to the pool on collision
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.ReturnToPool(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        _startPoint = transform.position;
    }
}
