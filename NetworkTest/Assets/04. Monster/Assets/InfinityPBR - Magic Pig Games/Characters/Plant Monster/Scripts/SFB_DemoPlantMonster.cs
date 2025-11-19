using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_DemoPlantMonster : MonoBehaviour {

	public Animator animator;
	public ParticleSystem[] gravityParticle;
	public GameObject iceBallPrefab;
	public float iceBallForceY = 500.0f;
	public float iceBallForceSide = 50.0f;
	public float iceBallForceSideMin = 25.0f;
	public Transform magicSpawnPos;
	public GameObject puffParticle;

	// Use this for initialization
	void Start () {
		animator = GetComponent<Animator> ();
	}
	
	public void UpdateLocomotion(float newValue){
		animator.SetFloat ("locomotion", newValue);
	}

	public void CastingWarmup(int start){
		for (int i = 0; i < gravityParticle.Length; i++){
			if (start == 1){
				gravityParticle[i].Play();
			}else{
				gravityParticle[i].Stop();
			}
		}
	}

	public void IceBall(){
		GameObject newIceBall = Instantiate(iceBallPrefab, magicSpawnPos.position, Quaternion.identity);
		newIceBall.transform.LookAt(newIceBall.transform.position + Vector3.up);
		newIceBall.GetComponent<Rigidbody>().AddForce(new Vector3(0, iceBallForceY, 0));
		NudgeBall(newIceBall);
	}

	public void NudgeBall(GameObject newIceBall){
		float randomX = Random.Range(-iceBallForceSide, iceBallForceSide);
		float randomZ = Random.Range(-iceBallForceSide, iceBallForceSide);
		newIceBall.GetComponent<Rigidbody>().AddForce(new Vector3(randomX, 0, randomZ));
	}

	public void Puff(){
		GameObject newPuff = Instantiate (puffParticle, magicSpawnPos.position, Quaternion.identity);
		Destroy (newPuff, 5.0f);
	}
}
