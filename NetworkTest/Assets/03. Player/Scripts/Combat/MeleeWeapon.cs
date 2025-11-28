using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : BaseWeapon
{
    [Header("Weapon Configuration")]
    [SerializeField] private MeleeHitbox meleeHitbox;
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private int maxCombo = 3;

    [Header("Position and Points")]
    [SerializeField] private Vector3 inHandsPositionOffset;
    public Vector3 InHandsPositionOffset => inHandsPositionOffset;

    [SerializeField] private float collisionDetectionLength = 0.5f;
    public float CollisionDetectionLength => collisionDetectionLength;

    [SerializeField] private float maxZPositionOffsetCollision = 0.2f;
    public float MaxZPositionOffsetCollision => maxZPositionOffsetCollision;

    [SerializeField] private WeaponPoint[] weaponPoints;
    public readonly Dictionary<WeaponPoint.PointType, Transform> WeaponPointsDict = new Dictionary<WeaponPoint.PointType, Transform>();
    [SerializeField] private WeaponPoint[] femaleWeaponPoints;
    public readonly Dictionary<WeaponPoint.PointType, Transform> FemaleWeaponPointsDict = new Dictionary<WeaponPoint.PointType, Transform>();

    [Header("Sound")]
    [SerializeField] private AudioClip[] fireSounds; // Array for combo sounds
    [SerializeField] private AudioClip emptySound;

    private float lastAttackTime = -1f;
    private int attackCount = 0;

    private Animator playerAnimator;
    private NetworkAnimatorSync networkAnimatorSync;
    private AudioSource _audioSource;

    private readonly int TwoHandAttackHash = Animator.StringToHash("twoHandAttack");

    private void Awake()
    {
        playerAnimator = GetComponentInParent<Animator>();
        networkAnimatorSync = GetComponentInParent<NetworkAnimatorSync>();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

        if (playerAnimator == null) Debug.LogError("MeleeWeapon: Animator not found on the same object.");
        if (meleeHitbox == null) Debug.LogError("MeleeWeapon: MeleeHitbox is not assigned.");

        InitializeWeaponPoints();
    }

    private void InitializeWeaponPoints()
    {
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
    }

    public override bool Attack()
    {
        if (playerAnimator == null || meleeHitbox == null) return false;

        if (Time.time > lastAttackTime + attackCooldown)
        {
            attackCount = 0; // Reset combo if time is up
        }

        if (Time.time >= lastAttackTime + 0.2f) // Allow next combo attack
        {
            lastAttackTime = Time.time;
            attackCount = (attackCount % maxCombo) + 1;

            if (networkAnimatorSync != null)
            {
                networkAnimatorSync.SetInteger(TwoHandAttackHash, attackCount);
            }
            else
            {
                // Fallback for offline mode
                playerAnimator.SetInteger(TwoHandAttackHash, attackCount);
            }

            PlayAttackSound(attackCount - 1);

            // The actual hitbox enabling/disabling will be done by animation events
            return true;
        }

        return false;
    }

    // Called by animation event
    public void EnableDamageCollider()
    {
        if (meleeHitbox != null)
        {
            meleeHitbox.EnableHitbox(PlayerDamage);
        }
    }

    // Called by animation event
    public void DisableDamageCollider()
    {
        if (meleeHitbox != null)
        {
            meleeHitbox.DisableHitbox();
        }
    }

    private void PlayAttackSound(int index)
    {
        if (fireSounds != null && fireSounds.Length > index && _audioSource != null)
        {
            _audioSource.PlayOneShot(fireSounds[index]);
        }
    }

    // This method is called by an animation event when the attack animation finishes
    // to allow another attack to start.
    public void ResetAttack()
    {
        // Optionally reset combo counter here if desired by design
        // attackCount = 0;
        // playerAnimator.SetInteger(TwoHandAttackHash, 0);
    }

    public void EndAttack() // Kept for compatibility if called elsewhere
    {
        DisableDamageCollider();
    }
}
