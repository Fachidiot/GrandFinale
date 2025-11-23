using UnityEngine;

/// <summary>
/// This component resides on the player and orchestrates melee attacks.
/// It works in conjunction with the Animator and the MeleeWeapon component.
/// </summary>
public class CombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator playerAnimator;
    private PlayerInputs playerInputs;

    // This would be dynamically assigned when the player equips a melee weapon.
    [SerializeField] private MeleeWeapon currentMeleeWeapon;

    [Header("Attack State")]
    [SerializeField] private float attackCooldown = 0.8f;
    private float lastAttackTime = -1f;

    void Awake()
    {
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
        if (GameManager.Instance != null)
        {
            playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();
        }
    }

    void Update()
    {
        // This is a simplified check. In a real game, you'd want to check
        // if the player is in a state that allows attacking (e.g., not rolling, not in a menu).
        if (playerInputs.GetAttack())
        {
            PerformAttack();
        }
    }

    /// <summary>
    /// Initiates a melee attack if the cooldown has passed.
    /// </summary>
    public void PerformAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
        {
            // Attack is on cooldown.
            return;
        }

        if (currentMeleeWeapon == null)
        {
            // Debug.LogWarning("[CombatManager] No melee weapon equipped.");
            return;
        }

        lastAttackTime = Time.time;

        // The animator has a parameter "TriggerAttack" which is a trigger.
        // This will start the melee attack animation (e.g., "SwordSwing").
        playerAnimator.SetTrigger("TriggerAttack");
        Debug.Log("[CombatManager] Attack performed. Animator trigger set.");
    }

    // --- Animation Event Handlers ---
    // These methods are intended to be called by Animation Events set up on the attack animation clips.

    /// <summary>
    /// ANIMATION EVENT: Called at the point in the animation where the swing starts and can deal damage.
    /// </summary>
    public void Handle_AttackSwingStart()
    {
        if (currentMeleeWeapon != null)
        {
            currentMeleeWeapon.BeginAttack();
        }
    }

    /// <summary>
    /// ANIMATION EVENT: Called at the point in the animation where the swing has finished.
    /// </summary>
    public void Handle_AttackSwingEnd()
    {
        if (currentMeleeWeapon != null)
        {
            currentMeleeWeapon.EndAttack();
        }
    }
}
