using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SFB_MinotaurDemo : MonoBehaviour {

    public Animator animator;
	
    public Toggle[] lowPolyToggles;
    public void LowPolyRandom()
    {
        foreach (var toggle in lowPolyToggles)
        {
            toggle.isOn = Random.Range(0, 2) == 1;
        }
    }
    
    public void SetLocomotion(float value)
    {
        animator.SetFloat("Locomotion", value);
    }
}
