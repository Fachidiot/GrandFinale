using UnityEngine;

public class SinglePlayer : MonoBehaviour, IPlayerControllable
{
    // IPlayerControllable implementation
    public bool IsMine => true; // Single player is always the local player
    public new GameObject gameObject => base.gameObject;
    public new Transform transform => base.transform;

    public new T GetComponent<T>()
    {
        return base.GetComponent<T>();
    }

    public new T GetComponentInChildren<T>()
    {
        return base.GetComponentInChildren<T>();
    }
}
