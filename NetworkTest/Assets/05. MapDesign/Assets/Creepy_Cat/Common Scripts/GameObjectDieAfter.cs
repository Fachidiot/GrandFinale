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

    // Simple class to force a gameobject to be destroyed after x seconds
    public class GameObjectDieAfter : MonoBehaviour
    {
        public float dieAfterSeconds = 3;

        void Update(){
            Destroy(gameObject, dieAfterSeconds);
        }
    }

}