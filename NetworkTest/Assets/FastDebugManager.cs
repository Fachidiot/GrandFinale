using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastDebugManager : MonoBehaviour
{
    void Awake()
    {
        if (NetworkManager.Instance)
            gameObject.SetActive(false);
    }
}
