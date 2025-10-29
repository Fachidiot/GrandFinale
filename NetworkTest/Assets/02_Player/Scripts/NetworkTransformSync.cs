using UnityEngine;

public class NetworkTransformSync : MonoBehaviour
{
    // 0: Body, 1: Camera, 2: Bullet
    public int viewId = 0;
    public bool IsMine = false;

    // Sync options - set in inspector. Camera might only need to sync rotation.
    public bool syncPosition = true;
    public bool syncRotation = true;

    // Interpolation targets
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float smoothingFactor = 15.0f;

    void Start()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    public void Initialize(string id, bool isOwner)
    {
        this.IsMine = isOwner;
    }

    void Update()
    {
        if (!IsMine)
        {
            if (syncPosition) transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothingFactor);
            if (syncRotation) transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothingFactor);
        }
    }



    public void OnTransformReceived(Vector3 position, Quaternion rotation)
    {
        if (!IsMine)
        {
            if (syncPosition) targetPosition = position;
            if (syncRotation) targetRotation = rotation;
        }
    }
}
