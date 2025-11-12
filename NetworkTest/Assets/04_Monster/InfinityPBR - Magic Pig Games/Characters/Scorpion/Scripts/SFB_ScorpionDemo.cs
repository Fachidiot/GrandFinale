using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SFB_ScorpionDemo : MonoBehaviour {

    public Animator anim;
    public Renderer[] bodyObjects;
    

	// Use this for initialization
	void Start () {
        anim = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Locomotion(float value)
    {
        anim.SetFloat("Locomotion", value);
    }

    public void Idle(float value)
    {
        anim.SetFloat("Idle", value);
    }

    public void SetMaterial(Material mat)
    {
        for (int i = 0; i < bodyObjects.Length; i++)
        {
            bodyObjects[i].sharedMaterial = mat;
        }
    }
}
