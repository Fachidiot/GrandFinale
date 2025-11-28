using UnityEngine;

public interface IPlayerControllable
{
    bool IsMine { get; }
    GameObject gameObject { get; }
    Transform transform { get; }

    void Die();

    T GetComponent<T>();
    T GetComponentInChildren<T>();
}
