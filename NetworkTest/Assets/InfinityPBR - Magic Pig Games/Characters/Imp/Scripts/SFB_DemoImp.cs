using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SFB_DemoImp : MonoBehaviour {

	private Animator animator;
	public Toggle[] bodyPartToggles;

	// Use this for initialization
	void Start () {
		animator = GetComponent<Animator> ();
	}
	
	public void Locomotion(float newValue){
		animator.SetFloat("locomotion", newValue);
	}

	public void Turning(float newValue){
		animator.SetFloat("turning", newValue);
	}

	public void RandomizeBodyParts()
	{
		foreach (var toggle in bodyPartToggles)
		{
			toggle.isOn = Random.Range(0, 2) == 1;
		}
	}
}
