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

    // Class to change a 3D Text button text randomly
    public class LedButtonRandText : MonoBehaviour
    {
        public float Timer = 2.0f;
        public int RandHigh = 999;
        public int RandLow = 100;

        public string PrevTxt="";
        public string EndTxt="";

        public bool blackText=false;
        public bool DisplayPercent=true;

        public TextMesh textObject;
        private float Memory = 0;

        private string Formula="";

        private void Start(){
            Memory = Timer;
            ChangeText();
        }

        void ChangeText(){

            if(DisplayPercent == true){
                Formula = (Random.Range(RandLow, RandHigh).ToString ());
            }else{
                Formula = "";
            }

            if(blackText == false){
                textObject.text = PrevTxt+"<color=orange>"+ Formula +"</color>"+EndTxt;
            }else{
                textObject.text = PrevTxt+"<color=black>"+ Formula +"</color>"+EndTxt;
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

