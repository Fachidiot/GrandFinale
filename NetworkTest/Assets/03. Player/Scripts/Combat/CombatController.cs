using UnityEngine;
using System;

/// <summary>
/// This component resides on the player and orchestrates all combat interactions (melee and ranged).
/// It works in conjunction with the Animator, WeaponController, and PlayerInputs.
/// </summary>
public class CombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private WeaponController weaponController;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.8f;
    private float lastAttackTime = -1f;

    private PlayerInputs playerInputs;

    // Animator Parameter Hashes
    private readonly int TriggerAttackHash = Animator.StringToHash("TriggerAttack");
    private readonly int IsArmedHash = Animator.StringToHash("isArmed");
    private readonly int WeaponTypeHash = Animator.StringToHash("WeaponType");

    void Awake()
    {
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
        if (playerInputs == null && GameManager.Instance != null)
        {
            playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();
        }
        if (weaponController == null) weaponController = GetComponent<WeaponController>();

        if (playerInputs == null) Debug.LogError("[PlayerCombatController] PlayerInputs not found!");
        if (weaponController == null) Debug.LogError("[PlayerCombatController] WeaponController not found!");
    }

    void Update()
    {
        // Update animator parameters based on current weapon state
        UpdateAnimatorArmedState();

        // Check for attack input
        if (playerInputs.GetAttack())
        {
            PerformAttack();
        }
    }

    /// <summary>
    /// Initiates an attack if the cooldown has passed.
    /// Delegates the actual attack logic to the currently equipped weapon.
    /// </summary>
    public void PerformAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
        {
            // Attack is on cooldown.
            return;
        }

        IWeapon currentWeapon = weaponController?.GETCurrentWeapon;
        if (currentWeapon == null)
        {
            Debug.LogWarning("[PlayerCombatController] No weapon equipped to attack with.");
            return;
        }

        lastAttackTime = Time.time;

        // The Weapon's Attack() method will handle specific attack logic (e.g., shooting, or triggering melee animation)
        currentWeapon.Attack();

        // For melee, we still need to set the general "TriggerAttack" for the animation controller.
        // The specific melee weapon will enable its collider via animation events.
        if (currentWeapon.Type == BaseWeapon.SlotType.melee || currentWeapon.Type == BaseWeapon.SlotType.unarmed)
        {
            playerAnimator.SetTrigger(TriggerAttackHash);
        }
        Debug.Log($"[PlayerCombatController] Attack performed with {currentWeapon.WeaponName}.");
    }

    private void UpdateAnimatorArmedState()
    {
        if (playerAnimator == null || weaponController == null) return;

        bool isArmed = weaponController.GETCurrentWeapon?.Type != BaseWeapon.SlotType.unarmed;
        int weaponType = (int)(weaponController.GETCurrentWeapon?.Type ?? BaseWeapon.SlotType.unarmed);

        playerAnimator.SetBool(IsArmedHash, isArmed);
        playerAnimator.SetInteger(WeaponTypeHash, weaponType);
    }

    // --- Animation Event Handlers ---
    // These methods are intended to be called by Animation Events set up on the attack animation clips.
    // They will now delegate to the specific MeleeWeapon component.

    /// <summary>
    /// ANIMATION EVENT: Called at the point in the animation where the swing starts and can deal damage.
    /// </summary>
    public void Handle_AttackSwingStart()
    {
        IWeapon currentWeapon = weaponController?.GETCurrentWeapon;
        if (currentWeapon is MeleeWeapon meleeWeapon)
        {
            meleeWeapon.BeginAttack();
        }
    }

    /// <summary>
    /// ANIMATION EVENT: Called at the point in the animation where the swing has finished.
    /// </summary>
    public void Handle_AttackSwingEnd()
    {
        IWeapon currentWeapon = weaponController?.GETCurrentWeapon;
        if (currentWeapon is MeleeWeapon meleeWeapon)
        {
            meleeWeapon.EndAttack();
        }
    }
}