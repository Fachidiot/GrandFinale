using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class MeleeWeapon : BaseWeapon
{
    [Header("Weapon Stats")]
    public float attackRange = 1.5f; // Used for visualization or AI, the collider is the authority

    [Header("Internal References")]
    [SerializeField] private Collider damageCollider;

    [Header("Position and Points")]
    [SerializeField] private Vector3 inHandsPositionOffset; // offset in hands
    public Vector3 InHandsPositionOffset => inHandsPositionOffset;

    [SerializeField] private WeaponPoint[] weaponPoints;
    public readonly Dictionary<WeaponPoint.PointType, Transform> WeaponPointsDict = new Dictionary<WeaponPoint.PointType, Transform>();

    [SerializeField] private WeaponPoint[] femaleWeaponPoints;
    public readonly Dictionary<WeaponPoint.PointType, Transform> FemaleWeaponPointsDict = new Dictionary<WeaponPoint.PointType, Transform>();

    [SerializeField] private float collisionDetectionLength;
    public float CollisionDetectionLength => collisionDetectionLength;

    [SerializeField] private float maxZPositionOffsetCollision;
    public float MaxZPositionOffsetCollision => maxZPositionOffsetCollision;
    [Header("Sound")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip emptySound;

    // private bool _canShoot = true;
    private AudioSource _audioSource;
    private BoltAnimation boltAnimation;


    // A list to track which enemies have been hit during the current swing
    // to prevent a single swing from hitting the same enemy multiple times.
    private List<Collider> hitTargets;

    private void Start()
    {
        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider>();
        }
        damageCollider.isTrigger = true;
        damageCollider.enabled = false; // The collider should be disabled by default.

        if (IsMale)
        {
            foreach (var point in weaponPoints)
            {
                if (point != null && !WeaponPointsDict.ContainsKey(point.pointType))
                {
                    WeaponPointsDict.Add(point.pointType, point.transform);
                }
            }
        }
        else
        {
            foreach (var point in femaleWeaponPoints)
            {
                if (point != null && !FemaleWeaponPointsDict.ContainsKey(point.pointType))
                {
                    FemaleWeaponPointsDict.Add(point.pointType, point.transform);
                }
            }
        }

        hitTargets = new List<Collider>();
    }

    public override bool Attack()
    {
        hitTargets.Clear();
        damageCollider.enabled = true;
        Debug.Log($"[{gameObject.name}] Attack Begun. Collider enabled.");
        return true;
    }

    public void EndAttack()
    {
        damageCollider.enabled = false;
        Debug.Log($"[{gameObject.name}] Attack Ended. Collider disabled.");
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = GetComponentInParent<IPlayerControllable>();
        if (player == null || !player.IsMine)
        {
            return;
        }

        // Check if we've already hit this target during this swing.
        if (hitTargets.Contains(other))
        {
            return;
        }

        // Check if the hit object is a monster.
        var monsterHealth = other.GetComponent<MonsterHealth>();
        if (monsterHealth == null) return;

        hitTargets.Add(other); // Add to the list of hit targets for this swing.

        var networkMonster = other.GetComponent<NetworkMonster>();
        if (networkMonster != null && NetworkManager.Instance != null)
        {
            Debug.Log($"[{gameObject.name}] Hit monster {networkMonster.MonsterId} for {PlayerDamage} damage. Sending damage report.");

            // If we are the host, apply damage directly.
            if (NetworkManager.Instance.Mode == NetworkMode.Host)
            {
                monsterHealth.TakeDamage(PlayerDamage);
            }
            // If we are a client, send a message to the host.
            else
            {
                JObject damageData = new JObject
                {
                    ["type"] = "player_dealt_damage",
                    ["monsterId"] = networkMonster.MonsterId,
                    ["damage"] = PlayerDamage
                };
                NetworkManager.Instance.SendJsonMessage(NetworkManager.Instance.LobbyHostID, damageData);
            }
        }
    }
}
