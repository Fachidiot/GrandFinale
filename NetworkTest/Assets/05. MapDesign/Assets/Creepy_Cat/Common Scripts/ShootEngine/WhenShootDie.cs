// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace creepycat.scifikitvol4
{
    // Phase 3: This class is called when a laser shoot hit something, i instanciate this class at position 
    // And i launch the explosion emitter, next i kill myself! The advantage is that you can
    // define a different explosion by object (small for one, big for another etc)
    public class WhenShootDie : MonoBehaviour {

        public GameObject ExplosionEmitter;
        public GameObject ObjectToKill;

        [HideInInspector]
        public bool YesKillMe = false;

        // If kill me activated by the class GetShootImpact
        void Update(){
            if (YesKillMe == true){
                KillGameObject();
            }      
        }

	    // Explode instanciate and source object kill
	    void KillGameObject () {
            ExplosionEmitter = Instantiate(ExplosionEmitter, transform.position, transform.rotation) as GameObject;
            Destroy(ObjectToKill);
	    }
    }

}