using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastDebugManager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this);
        if (NetworkManager.Instance)
            gameObject.SetActive(false);
    }
}
