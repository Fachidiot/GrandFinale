using UnityEngine;

public class NetworkMonsterAnimatorSync : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"Animator not found on {gameObject.name}. NetworkMonsterAnimatorSync will not function.");
            enabled = false;
        }
    }

    // This method will be called on the Host to gather animation state
    public byte GetAnimationMask()
    {
        // For simplicity, let's assume a basic animation state for now.
        // You would typically read Animator parameters here.
        // Example:
        // byte mask = 0;
        // if (animator.GetBool("IsWalking")) mask |= 1 << 0;
        // if (animator.GetBool("IsAttacking")) mask |= 1 << 1;
        // return mask;

        return 0; // Placeholder
    }

    // This method will be called on the Client to apply animation state
    public void OnAnimationDataReceived(byte animationMask)
    {
        if (animator == null) return;

        // Apply animation states based on the mask
        // Example:
        // animator.SetBool("IsWalking", (animationMask & (1 << 0)) != 0);
        // animator.SetBool("IsAttacking", (animationMask & (1 << 1)) != 0);
    }
}
