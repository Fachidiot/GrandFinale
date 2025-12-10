using UnityEngine;

public interface IPlayerControllable
{
    string Id { get; }
    bool IsMine { get; }
    GameObject gameObject { get; }
    Transform transform { get; }

    void Die();

    T GetComponent<T>();
    T GetComponentInChildren<T>();
}
