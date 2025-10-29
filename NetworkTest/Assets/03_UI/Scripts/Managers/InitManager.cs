using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitManager : MonoBehaviour
{
    [SerializeField] private GameObject[] enableObjs;
    [SerializeField] private GameObject[] disableObjs;

    void Start()
    {
        foreach (GameObject obj in enableObjs)
            obj.SetActive(true);
        foreach (GameObject obj in disableObjs)
            obj.SetActive(false);
    }
}
