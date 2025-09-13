using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NetworkedPlayerController : MonoBehaviour
{
    private Vector3 targetPosition;
    private Quaternion targetRotation; // We can add rotation later if needed
    private float interpolationSpeed = 10.0f;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void Update()
    {
        // Smoothly interpolate to the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * interpolationSpeed);
        // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * interpolationSpeed);
    }

    public void SetState(Vector3 position, float animForward, float animStrafe)
    {
        targetPosition = position;

        // Update animator parameters
        if (animator != null)
        {
            animator.SetFloat("Forward", animForward);
            animator.SetFloat("Strafe", animStrafe);
        }
    }
}
