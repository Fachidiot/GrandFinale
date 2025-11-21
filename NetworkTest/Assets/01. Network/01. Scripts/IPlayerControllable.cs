using UnityEngine;

public interface IPlayerControllable
{
    GameObject gameObject { get; }
    Transform transform { get; }
    T GetComponent<T>();
    T GetComponentInChildren<T>();
}
