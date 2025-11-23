using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Interactable))]
public class Ladder : MonoBehaviour
{
    [Tooltip("The point where the player starts climbing from the bottom.")]
    [SerializeField] private Transform bottomPoint;

    [Tooltip("The point where the player dismounts at the top.")]
    [SerializeField] private Transform topPoint;

    [Tooltip("The point where the player dismounts to after climbing to the top.")]
    [SerializeField] private Transform topDismountPoint;

    [Tooltip("The point where the player dismounts to after climbing to the bottom.")]
    [SerializeField] private Transform bottomDismountPoint;

    public Transform BottomPoint => bottomPoint;
    public Transform TopPoint => topPoint;
    public Transform TopDismountPoint => topDismountPoint;
    public Transform BottomDismountPoint => bottomDismountPoint;

    /// <summary>
    /// Called by the Interactable's UnityEvent.
    /// Finds the local player, determines the closest start point (top or bottom),
    /// and tells the player to start climbing this ladder.
    /// </summary>
    public void UseLadder()
    {
        IPlayerControllable localPlayer = null;

        if (PlayerManager.Instance)
            localPlayer = PlayerManager.Instance?.LocalPlayer;
        else    // Test 용.
            localPlayer = FindObjectOfType<SinglePlayer>();

        if (localPlayer != null && localPlayer.gameObject != null)
        {
            var climber = localPlayer.gameObject.GetComponent<PlayerLadderClimber>();
            if (climber != null)
            {
                // Determine the start position based on player's proximity
                float distanceToTop = Vector3.Distance(localPlayer.gameObject.transform.position, topPoint.position);
                float distanceToBottom = Vector3.Distance(localPlayer.gameObject.transform.position, bottomPoint.position);

                climber.StartClimbing(this, (distanceToTop < distanceToBottom) ? topPoint : bottomPoint);
            }
            else
            {
                Debug.LogWarning("Player is missing a PlayerLadderClimber component.", localPlayer.gameObject);
            }
        }
        else
        {
            Debug.LogError("Could not find local player to use ladder.");
        }
    }

    // This could be used by the climber to know when to get off
    public float GetLadderHeight()
    {
        return Vector3.Distance(bottomPoint.position, topPoint.position);
    }
}
