using UnityEngine;

public class NetworkedPlayerController : MonoBehaviour
{
    private Vector3 targetPosition;
    private float interpolationSpeed = 10.0f;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * interpolationSpeed);
    }

    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
    }
}
