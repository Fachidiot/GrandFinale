using UnityEngine;

public class UnarmedWeapon : BaseWeapon
{
    [Header("Unarmed Colliders")]
    [SerializeField] private MeleeHitbox leftFistBoxArm;
    [SerializeField] private MeleeHitbox rightFistBoxArm;

    [Header("Unarmed Attack Settings")]
    [SerializeField] private float attackCooldown = 0.8f;
    private float lastAttackTime = -1f;
    private bool isJab = true; // To alternate between Jab and Cross

    private Animator playerAnimator;
    private PlayerInputs playerInputs;
    private NetworkAnimatorSync networkAnimatorSync;

    // Animator Parameter Hashes
    private readonly int JabHash = Animator.StringToHash("Jab");
    private readonly int CrossHash = Animator.StringToHash("Cross");

    // Animator Parameter Hashes are now handled in NetworkAnimatorSync

    private void Awake()
    {
        slotType = IWeapon.SlotType.unarmed;
        playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();
        playerAnimator = GetComponent<Animator>();
        networkAnimatorSync = GetComponentInParent<NetworkAnimatorSync>();


        if (playerInputs == null)
            Debug.LogError("UnarmedWeapon: PlayerInputs not found in GameManager's Instance. Unarmed combat will not function.");
        if (playerAnimator == null)
            Debug.LogError("UnarmedWeapon: Animator not found in Model Prefab. Unarmed combat animations will not function.");
        if (networkAnimatorSync == null)
            Debug.LogError("UnarmedWeapon: NetworkAnimatorSync not found in parent. Unarmed combat animations will not be synced.");
    }

    public override bool Attack()
    {
        if (playerInputs == null || playerAnimator == null || networkAnimatorSync == null) return false;

        // Punching logic
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            if (networkAnimatorSync)
                networkAnimatorSync.TriggerUnarmedAttack(isJab);
            else
            {
                if (isJab)
                {
                    playerAnimator.SetTrigger(JabHash);
                    // leftForeArm의 collider 충돌 체크 및 데미지 넣기
                }
                else
                {
                    playerAnimator.SetTrigger(CrossHash);
                    // rightForeArm의 collider 충돌 체크 및 데미지 넣기
                }
            }

            isJab = !isJab; // Toggle for next punch
            return true;
        }
        return false;
    }

    // 잽 애니메이션의 "주먹이 뻗는 순간"에 이벤트로 등록
    public void OpenLeftHitbox()
    {
        if (leftFistBoxArm != null) leftFistBoxArm.EnableHitbox(PlayerDamage);
    }

    // 잽 애니메이션의 "주먹을 거두는 순간"에 이벤트로 등록
    public void CloseLeftHitbox()
    {
        if (leftFistBoxArm != null) leftFistBoxArm.DisableHitbox();
    }

    // 크로스 애니메이션용
    public void OpenRightHitbox()
    {
        if (rightFistBoxArm != null) rightFistBoxArm.EnableHitbox(PlayerDamage); // 데미지를 더 높게 줄 수도 있음
    }

    public void CloseRightHitBox()
    {
        if (rightFistBoxArm != null) rightFistBoxArm.DisableHitbox();
    }
}