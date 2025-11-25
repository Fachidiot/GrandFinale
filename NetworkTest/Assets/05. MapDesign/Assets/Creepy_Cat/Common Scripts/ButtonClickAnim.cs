// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Uween;

namespace creepycat.scifikitvol4
{

    // Small example class to help you to simulate button click anim
    // For VR? I don't know :)) But i think that simple to replace
    // the Input.GetMouse blabla by any other VR interactivity?
    public class ButtonClickAnim : MonoBehaviour{

        public GameObject ButtonObject;
        public float PushMove = -0.0025f;

        // What mouse button to activate lights
        public enum ButtonAxis { X, Y, Z };
        public ButtonAxis SelectAxis=ButtonAxis.Y;

        private float DefaultY;

        // Get the default button Y height
        private void Start(){
            

            // Enum for mouse button test
            switch(SelectAxis){
            case ButtonAxis.X:
                DefaultY = ButtonObject.transform.localPosition.x;
                break;
            case ButtonAxis.Y:
                DefaultY = ButtonObject.transform.localPosition.y;
                break;
            case ButtonAxis.Z:
                DefaultY = ButtonObject.transform.localPosition.z;
                break;
            }

        }

        // Update is called once per frame
        void Update(){

            // Release button position
            if (Input.GetMouseButtonUp(0)){

                // Enum for mouse button test
                switch(SelectAxis){
                case ButtonAxis.X:
                    TweenX.Add(ButtonObject, 0.1f, DefaultY).EaseInOutSine();
                    break;
                case ButtonAxis.Y:
                    TweenY.Add(ButtonObject, 0.1f, DefaultY).EaseInOutSine();
                    break;
                case ButtonAxis.Z:
                    TweenZ.Add(ButtonObject, 0.1f, DefaultY).EaseInOutSine();
                    break;
                }

            }
            
            // if Button Left Pressed
            if (Input.GetMouseButtonDown(0)){

                // Get the gameobject clicked
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // If something clicked
                if (Physics.Raycast(ray, out hit)){

                    // If it's my button
                    if (hit.transform == ButtonObject.transform){

                        //Debug.Log("Button Clicked");

                        // Enum for mouse button test
                        switch(SelectAxis){
                        case ButtonAxis.X:
                            TweenX.Add(ButtonObject, 0.1f, PushMove).Relative().EaseInOutSine();
                            break;
                        case ButtonAxis.Y:
                            TweenY.Add(ButtonObject, 0.1f, PushMove).Relative().EaseInOutSine();
                            break;
                        case ButtonAxis.Z:
                            TweenZ.Add(ButtonObject, 0.1f, PushMove).Relative().EaseInOutSine();
                            break;
                        }

                    }

                }
            }
        }
    }

}