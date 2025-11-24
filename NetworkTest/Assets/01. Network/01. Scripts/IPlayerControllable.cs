using UnityEngine;

public interface IPlayerControllable
{
    bool IsMine { get; }
    GameObject gameObject { get; }
    Transform transform { get; }
    T GetComponent<T>();
    T GetComponentInChildren<T>();
}
