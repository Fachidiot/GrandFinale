using UnityEngine;

public class NetworkMonster : MonoBehaviour
{
    public ushort MonsterId { get; private set; }

    public void Initialize(ushort id)
    {
        MonsterId = id;
    }
}
