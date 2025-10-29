using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public enum SlotType
    {
        rifle = 1,
        smg = 2,
        pistol = 3
    }

    [Header("Weapon Settings")]
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private SlotType slotType;
    public SlotType Type => slotType;

    [SerializeField] private int playerDamage = 10;
    public int PlayerDamage => playerDamage;

    [SerializeField] private float shotTemp; // 0 - fast 1 - slow
    public float ShotTemp => shotTemp;

    [SerializeField] private bool singleShoot; // only single shoot?
    public bool SingleShoot => singleShoot;

    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 30;
    public int MaxAmmo => maxAmmo;
    private int currentAmmo;
    public int CurrentAmmo => currentAmmo;

    [Header("Shotgun Parameters")]
    [SerializeField] private bool shotgun;
    public bool IsShotgun => shotgun;

    [SerializeField] private int bulletAmount;
    public int BulletAmount => bulletAmount;

    [SerializeField] private float accuracy = 1;
    public float Accuracy => accuracy;

    [Header("Components")]
    [SerializeField] private Transform aimPoint;
    public Transform AimPoint => aimPoint;

    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] private Transform casingSpawnPoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private float bulletForce;
    public float BulletForce => bulletForce;
    [SerializeField] private float bulletStartSpeed;
    public float BulletStartSpeed => bulletStartSpeed;

    [Header("Position and Points")]
    [SerializeField] private Vector3 inHandsPositionOffset; // offset in hands
    public Vector3 InHandsPositionOffset => inHandsPositionOffset;

    [SerializeField] private WeaponPoint[] weaponPoints;
    public readonly Dictionary<WeaponPoint.PointType, Transform> WeaponPointsDict = new Dictionary<WeaponPoint.PointType, Transform>();

    [Header("View Resistance")]
    [SerializeField] private float resistanceForce; // view offset rotation
    public float ResistanceForce => resistanceForce;

    [SerializeField] private float resistanceSmoothing; // view offset rotation speed
    public float ResistanceSmoothing => resistanceSmoothing;

    [SerializeField] private float collisionDetectionLength;
    public float CollisionDetectionLength => collisionDetectionLength;

    [SerializeField] private float maxZPositionOffsetCollision;
    public float MaxZPositionOffsetCollision => maxZPositionOffsetCollision;

    [Header("Recoil Parameters")]
    [SerializeField] private RecoilParametersModel recoilParametersModel = new RecoilParametersModel();
    public RecoilParametersModel RecoilParameters => recoilParametersModel;

    [Header("Sound")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip emptySound;

    [Header("Effects Settings")]
    [SerializeField] private float muzzleFlashLifetime = 0.5f;
    [SerializeField] private float casingLifetime = 5f;
    [SerializeField] private float casingEjectForce = 55f;
    [SerializeField] private Vector2 casingEjectTorqueRange = new Vector2(-20f, 40f);
    [SerializeField] private float boltAnimationDelay = 0.05f;

    private bool _canShoot = true;
    private AudioSource _audioSource;
    private BoltAnimation boltAnimation;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        boltAnimation = GetComponent<BoltAnimation>();

        // Cache weapon points for fast lookups
        foreach (var point in weaponPoints)
        {
            if (point != null && !WeaponPointsDict.ContainsKey(point.pointType))
            {
                WeaponPointsDict.Add(point.pointType, point.transform);
            }
        }
    }

    void Start()
    {
        currentAmmo = maxAmmo;
        // Initialize pools for all weapon effects
        if (PoolManager.Instance != null)
        {
            if (bulletPrefab != null) PoolManager.Instance.CreatePool(bulletPrefab, 20);
            if (casingPrefab != null) PoolManager.Instance.CreatePool(casingPrefab, 20);
            if (muzzleFlash != null) PoolManager.Instance.CreatePool(muzzleFlash, 5);
        }
    }


    public bool Shoot()
    {
        if (!_canShoot) return false;

        if (currentAmmo <= 0)
        {
            if (emptySound) _audioSource.PlayOneShot(emptySound);
            return false;
        }

        _canShoot = false;
        currentAmmo--;

        if (shotgun)
        {
            for (int i = 0; i < bulletAmount; i++)
            {
                Quaternion bulletSpawnDirection = Quaternion.Euler(bulletSpawnPoint.rotation.eulerAngles + new Vector3(Random.Range(-accuracy, accuracy), Random.Range(-accuracy, accuracy), 0));
                float bulletSpeed = Random.Range(bulletStartSpeed * 0.8f, bulletStartSpeed);
                BulletSpawn(bulletSpeed, bulletSpawnDirection);
            }
        }
        else
        {
            BulletSpawn(bulletStartSpeed, bulletSpawnPoint.rotation);
        }

        CasingSpawn();

        MuzzleFlashSpawn();

        if (fireSound) _audioSource.PlayOneShot(fireSound);

        if (boltAnimation) boltAnimation.StartAnim(boltAnimationDelay);
        StartCoroutine(ShootPause());

        return true;
    }

    private IEnumerator ShootPause()
    {
        yield return new WaitForSeconds(shotTemp);
        _canShoot = true;
    }

    private void BulletSpawn(float startSpeed, Quaternion bulletDirection)
    {
        if (bulletPrefab == null) return;

        GameObject bulletGO;

        if (useObjectPooling && PoolManager.Instance != null)
        {
            bulletGO = PoolManager.Instance.Spawn(bulletPrefab, bulletSpawnPoint.position, bulletDirection);
        }
        else
        {
            bulletGO = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletDirection);
        }

        if (bulletGO == null) return;

        // BulletBehaviour를 직접 가져와서 초기화 메소드를 호출합니다.
        var bulletComponent = bulletGO.GetComponent<BulletBehaviour>();
        if (bulletComponent != null)
        {
            // BulletStart를 확장하여 풀링 여부를 전달합니다.
            if (bulletComponent is IBulletInitialize initializer)
            {
                initializer.BulletStart(transform, useObjectPooling);
            }
            else
            {
                bulletComponent.BulletStart(transform);
            }
        }
    }

    private void MuzzleFlashSpawn()
    {
        if (PoolManager.Instance == null || muzzleFlash == null) return;

        var muzzleSpawn = PoolManager.Instance.Spawn(muzzleFlash, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        if (muzzleSpawn == null) return;

        PoolManager.Instance.ReturnToPool(muzzleSpawn, muzzleFlashLifetime);
    }

    private void CasingSpawn()
    {
        if (PoolManager.Instance == null || casingPrefab == null) return;

        var cas = PoolManager.Instance.Spawn(casingPrefab, casingSpawnPoint.transform.position, Random.rotation);
        if (cas == null) return;

        cas.GetComponent<Rigidbody>().AddForce(casingSpawnPoint.transform.forward * casingEjectForce + new Vector3(
            Random.Range(casingEjectTorqueRange.x, casingEjectTorqueRange.y),
            Random.Range(casingEjectTorqueRange.x, casingEjectTorqueRange.y),
            Random.Range(casingEjectTorqueRange.x, casingEjectTorqueRange.y)));
        PoolManager.Instance.ReturnToPool(cas, casingLifetime);
    }

    public void Reload()
    {
        currentAmmo = maxAmmo;
    }
}
