using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_SpiderDemo : MonoBehaviour {

	public Animator animator;
	public ParticleSystem particle;

	public void UpdateLocomotion(float newValue){
		animator.SetFloat ("locomotion", newValue);
	}

	public void UpdateTurning(float newValue){
		animator.SetFloat("turning", newValue);
	}

	public void StartCast(){
		particle.Play();
	}

	public void StopCast(){
		particle.Stop();
	}
}
