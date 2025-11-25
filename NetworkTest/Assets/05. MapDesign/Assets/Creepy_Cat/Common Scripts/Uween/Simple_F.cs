using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simple_F : MonoBehaviour
{
    public Light lightObj;

    void Start()
    {
        
    }



    void Update()
    {

  lightObj.intensity = Mathf.Lerp(lightObj.intensity, Random.Range(1.1f, 6.0f), Time.deltaTime * 1.1f);
    }
}
