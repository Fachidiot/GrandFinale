using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_GolemsDemo : MonoBehaviour {

	public Animator animator;
	public Material[] materials;
	public Renderer[] meshes;

	// Use this for initialization
	void Start () {
		animator = GetComponent<Animator> ();
	}
	
	public void SetLocomotion(float value){
		animator.SetFloat ("Locomotion", value);
	}

	public void SetTurning(float value){
		animator.SetFloat ("Turning", value);
	}

	public void ChangeMaterial(int value){
		for (int i = 0; i < meshes.Length; i++){
			meshes [i].sharedMaterial = materials [value];
		}
	}
}
