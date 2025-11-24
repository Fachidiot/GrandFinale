using UnityEngine;

public class UnarmedWeapon : BaseWeapon
{
    [Header("Unarmed Attack Settings")]
    [SerializeField] private float attackCooldown = 0.8f;
    private float lastAttackTime = -1f;
    private bool isJab = true; // To alternate between Jab and Cross

    [Header("Animator")]
    [SerializeField] private Animator playerAnimator;
    private PlayerInputs playerInputs;

    // Animator Parameter Hashes
    private readonly int CombatHash = Animator.StringToHash("isCombat");
    private readonly int JabHash = Animator.StringToHash("Jab");
    private readonly int CrossHash = Animator.StringToHash("Cross");

    private void Awake()
    {
        slotType = IWeapon.SlotType.unarmed;
        playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();

        if (playerInputs == null)
            Debug.LogError("UnarmedWeapon: PlayerInputs not found in GameManager's Instance. Unarmed combat will not function.");
        if (playerAnimator == null)
            Debug.LogError("UnarmedWeapon: Animator not found in Model Prefab. Unarmed combat animations will not function.");
    }

    public override bool Attack()
    {
        if (playerInputs == null || playerAnimator == null) return false;

        // Punching logic
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            if (isJab)
            {
                playerAnimator.SetTrigger(JabHash);
            }
            else
            {
                playerAnimator.SetTrigger(CrossHash);
            }
            isJab = !isJab; // Toggle for next punch
            return true;
        }
        return false;
    }


}