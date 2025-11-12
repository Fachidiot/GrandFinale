using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastDebugManager : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Instance)
            gameObject.SetActive(false);
    }
}
