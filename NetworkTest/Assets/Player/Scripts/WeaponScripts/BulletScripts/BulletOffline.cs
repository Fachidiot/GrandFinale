using UnityEngine;

public class BulletOffline : BulletBehaviour, IBulletInitialize
{
    public float lifeTime;
    private Vector3 _startPoint;
    public float startSpeed;
    public float force = 1;
    public GameObject decalPrefab;
    public GameObject bloodPrefab;
    public LayerMask mask; // Raycast Ignored Layers;

    private Rigidbody rb;
    private bool _isPooled; // Flag to know if this instance is from a pool

    // Awake is called when the script instance is being loaded.
    protected override void Awake()
    {
        base.Awake(); // Call the base class Awake method
        rb = GetComponent<Rigidbody>();
        if (PoolManager.Instance != null)
        {
            if (decalPrefab != null) PoolManager.Instance.CreatePool(decalPrefab, 10);
            if (bloodPrefab != null) PoolManager.Instance.CreatePool(bloodPrefab, 10);
        }
    }

    // This method is called when the object becomes enabled and active.
    private void OnEnable()
    {
        // The decision to return/destroy is now handled by the BulletStart method
        // and the collision logic in Update.
    }

    // Overriding the base method is not needed if we implement the interface explicitly.
    // public override void BulletStart(Transform bulletCreator) { }

    // Explicitly implement the new interface
    public void BulletStart(Transform bulletCreator, bool isPooled)
    {
        _isPooled = isPooled;

        var weap = bulletCreator.GetComponent<Weapon>();
        force = weap.BulletForce;
        startSpeed = weap.BulletStartSpeed;

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
            if (decalPrefab && hit.transform.CompareTag("HitBox"))
            {
                SpawnEffect(decalPrefab, hit, 15f);
            }

            if (bloodPrefab && hit.transform.CompareTag("Entity"))
            {
                SpawnEffect(bloodPrefab, hit, 3f);
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
