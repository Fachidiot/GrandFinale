using UnityEngine;

public class NetworkTransformSync : MonoBehaviour
{
    // 0: Body, 1: Camera, etc. Set this in the Unity Inspector.
    public int viewId = 0;
    public bool isMine = false;

    // Sync options - set in inspector. Camera might only need to sync rotation.
    public bool syncPosition = true;
    public bool syncRotation = true;

    private NetworkManager networkManager;
    private string objectId;

    // Interpolation targets
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float smoothingFactor = 15.0f;

    // Send rate
    private float sendInterval = 0.05f; // 20 times per second
    private float nextSendTime;

    void Start()
    {
        networkManager = NetworkManager.Instance;
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    public void Initialize(string id, bool isOwner)
    {
        this.objectId = id;
        this.isMine = isOwner;
    }

    void Update()
    {
        if (isMine)
        {
            if (Time.time >= nextSendTime)
            {
                SendTransform();
                nextSendTime = Time.time + sendInterval;
            }
        }
        else
        {
            if (syncPosition) transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothingFactor);
            if (syncRotation) transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothingFactor);
        }
    }

    private void SendTransform()
    {
        var data = new
        {
            type = "transform_update",
            player_id = this.objectId,
            view_id = this.viewId, // Include the viewId
            position = new { x = transform.position.x, y = transform.position.y, z = transform.position.z },
            rotation = new { x = transform.rotation.x, y = transform.rotation.y, z = transform.rotation.z, w = transform.rotation.w }
        };

        string jsonMessage = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        networkManager.SendUdpMessage(jsonMessage);
    }

    public void OnTransformReceived(Vector3 position, Quaternion rotation)
    {
        if (!isMine)
        {
            if (syncPosition) targetPosition = position;
            if (syncRotation) targetRotation = rotation;
        }
    }
}
