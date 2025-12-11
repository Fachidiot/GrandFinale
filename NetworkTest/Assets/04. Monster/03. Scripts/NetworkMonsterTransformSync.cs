using System.IO;
using UnityEngine;

public class NetworkMonsterTransformSync : MonoBehaviour
{
    // --- Data structure for network transport ---
    public struct MonsterTransformData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    // --- Interpolation targets for clients ---
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float positionSmoothingFactor = 10.0f;
    private float rotationSmoothingFactor = 10.0f;

    // --- Caching for performance ---
    private Transform myTransform;
    private bool isHost;

    void Awake()
    {
        myTransform = transform;
        targetPosition = myTransform.position;
        targetRotation = myTransform.rotation;
    }

    void Start()
    {
        // Determine if this instance is on the host.
        // This check assumes the NetworkManager is available when this component starts.
        isHost = NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.Host;
    }

    void FixedUpdate()
    {
        // Clients smoothly interpolate to the target transform received from the host.
        // Using FixedUpdate for frame-rate independent smoothing.
        if (!isHost)
        {
            myTransform.position = Vector3.Lerp(myTransform.position, targetPosition, Time.fixedDeltaTime * positionSmoothingFactor);
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothingFactor);
        }
    }

    /// <summary>
    /// [HOST-SIDE] Gathers the current transform state into a data packet.
    /// This should be called by a network manager script at a regular interval.
    /// </summary>
    public MonsterTransformData GetTransformData()
    {
        return new MonsterTransformData
        {
            position = myTransform.position,
            rotation = myTransform.rotation
        };
    }

    /// <summary>
    /// [CLIENT-SIDE] Receives transform data from the host and sets it as the interpolation target.
    /// This should be called by a network manager script when it receives an update.
    /// </summary>
    public void OnTransformDataReceived(MonsterTransformData data)
    {
        if (!isHost)
        {
            targetPosition = data.position;
            targetRotation = data.rotation;
        }
    }

    #region Serialization Helpers

    /// <summary>
    /// [HOST-SIDE] Serializes the MonsterTransformData struct into a byte array for network transport.
    /// </summary>
    public static byte[] Serialize(MonsterTransformData data)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(data.position.x);
            writer.Write(data.position.y);
            writer.Write(data.position.z);

            writer.Write(data.rotation.x);
            writer.Write(data.rotation.y);
            writer.Write(data.rotation.z);
            writer.Write(data.rotation.w);

            return stream.ToArray();
        }
    }

    /// <summary>
    /// [CLIENT-SIDE] Deserializes a byte array back into a MonsterTransformData struct.
    /// </summary>
    public static MonsterTransformData Deserialize(byte[] bytes)
    {
        var data = new MonsterTransformData();
        if (bytes == null || bytes.Length == 0) return data;

        using (MemoryStream stream = new MemoryStream(bytes))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            float posX = reader.ReadSingle();
            float posY = reader.ReadSingle();
            float posZ = reader.ReadSingle();
            data.position = new Vector3(posX, posY, posZ);

            float rotX = reader.ReadSingle();
            float rotY = reader.ReadSingle();
            float rotZ = reader.ReadSingle();
            float rotW = reader.ReadSingle();
            data.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
        }
        return data;
    }

    #endregion
}