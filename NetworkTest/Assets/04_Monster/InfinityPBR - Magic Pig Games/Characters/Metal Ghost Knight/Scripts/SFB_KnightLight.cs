using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_KnightLight : MonoBehaviour {

    public Light light;
    public float counter = 0f;
    public float fadeStart = 1.5f;

    void OnEnable () {
        light = GetComponent<Light>();
        light.intensity = 1.1f;
        counter = 0f;
        transform.parent.localPosition = new Vector3(0.576f, 3.07f, 0.55f);
	}
	
	void Update()
    {
        counter += Time.deltaTime;
        if (counter > fadeStart)
        {
            light.intensity -= Time.deltaTime;
        }
    }
}
