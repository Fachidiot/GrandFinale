// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;
using System.Collections;

namespace creepycat.scifikitvol4
{
    // Phase 1: This class is called when to launch a projectile from a gameobject
    public class ShootLaunchFrom : MonoBehaviour 
    {
        RaycastHit hit;
        public GameObject[] laserShoot;
        public Transform spawnPosition;

        public enum WhatButton { Left, Right, Wheel };
        public WhatButton mouseButton;

        public Collider SourceCollider;

        [Header("")]
        public AudioClip FireSound;

        [HideInInspector]
        public int currentLaser = 0;
	    public float speed = 1000;

        private KeyCode tmpKeyCode=KeyCode.Mouse0;
        private AudioSource audioSource;


        void Start(){
            audioSource = GetComponent<AudioSource>();
        }

	    void Update() 	{

           switch(mouseButton){
           case WhatButton.Left:
                tmpKeyCode=KeyCode.Mouse0;
                break;
           case WhatButton.Right:
                tmpKeyCode=KeyCode.Mouse1;
                break;
           case WhatButton.Wheel:
                tmpKeyCode=KeyCode.Mouse2;
                break;
            }

            if (Input.GetKeyDown(tmpKeyCode)){
                if (FireSound!=null) audioSource.PlayOneShot(FireSound);

                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity)){
                    GameObject projectile = Instantiate(laserShoot[currentLaser], spawnPosition.position, Quaternion.identity) as GameObject;
                    Physics.IgnoreCollision(projectile.GetComponent<Collider>(), SourceCollider.GetComponent<Collider>());

                    projectile.transform.LookAt(hit.point);
                    projectile.GetComponent<Rigidbody>().AddForce(projectile.transform.forward * speed);
                    projectile.GetComponent<ShootHitSomething>().collisionNormal = hit.normal;
                }  
            }

            //Debug.DrawRay(Camera.main.ScreenPointToRay(Input.mousePosition).origin, Camera.main.ScreenPointToRay(Input.mousePosition).direction*200, Color.green);
	    }

    }

}