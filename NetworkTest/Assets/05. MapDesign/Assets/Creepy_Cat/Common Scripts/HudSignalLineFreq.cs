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

    // Class to change a gameobject scale randomly
    public class HudSignalLineFreq : MonoBehaviour
    {
        public float Timer = 2.0f;
        public float minScale = 1.0f;
        public float maxScale = 1.5f;

        private Vector3 scaleChange;

        private float Memory = 0;


        private void Start(){
            Memory = Timer;
        }

        void ChangeScale()
        {
          scaleChange = new Vector3(transform.localScale.x ,transform.localScale.y, Random.Range(minScale, maxScale));

            transform.localScale=scaleChange;
        }

        void Update(){

         if(Timer>0){         
            Timer -= Time.deltaTime;     
         }     

          if(Timer < 0){         
            ChangeScale();
            Timer = Memory;
          } 
        }

    }
}

