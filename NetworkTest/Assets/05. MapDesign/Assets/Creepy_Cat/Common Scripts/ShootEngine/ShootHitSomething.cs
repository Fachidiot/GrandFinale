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
    // Phase 3: This class is called when the shoot hit something. Because in reality there is 2
    // "explosion", this class send the first one the impact explosion at the position point, next
    // another class will launch another explosion at the pivot point center of the object to destroy
    public class ShootHitSomething : MonoBehaviour {
    
        public GameObject ShootImpactExplode;
        public GameObject ShootGameObject;
        public GameObject[] laserTrails;

        [HideInInspector]
        public Vector3 collisionNormal; 
        public bool oneTime = false;

	    void Start () {
            ShootGameObject = Instantiate(ShootGameObject, transform.position, transform.rotation) as GameObject;
            ShootGameObject.transform.parent = transform;
	    }

	    void OnCollisionEnter (Collision hit) {

            // If this bool is not used, this damn unity make multiple instances (clone)(clone) etc... Use this to avoid the problem (i waste one day on this shit..).
            if (oneTime == false){

                    // YOU NEED TO DEFINE A DESTRUCTIBLE TAG AND ASSIGN IT TO ALL GAMEOBJECTS YOU WANT DESTROY + A RIGID BODY
                    if (hit.gameObject.tag == "Destructible"){

                        // Poke into the the hitted object and i change the component parameters
                        WhenShootDie yesDie = hit.gameObject.GetComponent<WhenShootDie>();
                        yesDie.YesKillMe = true;
                        yesDie.ObjectToKill = hit.gameObject;

                    }

                    // Instanciate the impact of the shoot and place it on the transform of the ray
                    ShootImpactExplode = Instantiate(ShootImpactExplode, transform.position, Quaternion.FromToRotation(Vector3.up, collisionNormal)) as GameObject;

            
                    // Removing child laser trail
                    foreach (GameObject trail in laserTrails){
                        GameObject curTrail = transform.Find(ShootGameObject.name + "/" + trail.name).gameObject;

                        // If different than null, else debug log error
                        if (curTrail != null){
                            curTrail.transform.parent = null;
                            Destroy(curTrail);
                        }
                    }

                    // Cleaning memory
                    Destroy(ShootGameObject,7f);  //7f
                    Destroy(ShootImpactExplode,7f);   //7f
                    Destroy(gameObject);
                    oneTime = true;

            }

	    }

    }

}