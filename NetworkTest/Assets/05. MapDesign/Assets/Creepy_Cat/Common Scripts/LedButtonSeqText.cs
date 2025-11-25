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

    // Class to change a 3D Text button text with sequence
    public class LedButtonSeqText : MonoBehaviour
    {
        public float Timer = 2.0f;

        [SerializeField]
        [TextArea]
        public string[] stringList;

        public TextMesh textObject;
        private float Memory = 0;
        private int txtNumber = 0;

        private void Start(){
            Memory = Timer;
            ChangeText();
        }

        void ChangeText(){

            textObject.text=stringList[txtNumber];
            txtNumber = txtNumber+1;

            if(txtNumber >= stringList.Length){
               txtNumber = 0;
            }

        }

        void Update(){

         if(Timer>0){         
            Timer -= Time.deltaTime;     
         }     

        if(Timer < 0){         
            ChangeText();
            Timer = Memory;
          } 
        }

    }
}

