// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Uween;

namespace creepycat.scifikitvol4
{

    public class ContainerOpen : MonoBehaviour
    {
        public GameObject DoorLeft;
        public GameObject DoorRight;
        public GameObject DoorButton;

        public AudioClip DoorSound;

        public float moveTimeA = 2.0f;
        private bool SwitchAnimLeft = false;
        
        private float rotateMax = 90f;
        
        private Renderer buttonRenderer;

        private AudioSource audioSource;

        private bool AnimationFlagA = false;

        void Start(){
            buttonRenderer = DoorButton.GetComponent<Renderer>();
            audioSource = GetComponent<AudioSource>();

            buttonRenderer.material.SetColor("_EmissionColor", Color.white * 1.5f);
        }

        void EndAnimationFlagA(){
            AnimationFlagA = false;
        }


        // Update is called once per frame    
        void Update(){

            // If mouse click
            if (Input.GetMouseButtonDown(0))
            {
                // Get the gameobject clicked
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // If something clicked
                if (Physics.Raycast(ray, out hit))
                {

                    // If it's my button
                    if (hit.transform == DoorButton.transform)
                    {

                        if (AnimationFlagA == false)
                        {
                            SwitchAnimLeft = !SwitchAnimLeft;
                            AnimationFlagA = true;

                            // Anim switch var
                            switch (SwitchAnimLeft)
                            {
                                // If no we launch all the things needed
                                case false:
                                    TweenRY.Add(DoorLeft, moveTimeA, 0).From(rotateMax).EaseInOutCubic().Then(EndAnimationFlagA);
                                    TweenRY.Add(DoorRight, moveTimeA, 0).From(-rotateMax).EaseInOutCubic().Then(EndAnimationFlagA);

                                    buttonRenderer.material.SetColor("_EmissionColor", Color.white * 1.5f);

                                    audioSource.PlayOneShot(DoorSound, 1.0F);
                                    break;

                                case true:
                                    TweenRY.Add(DoorLeft, moveTimeA, rotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlagA);
                                    TweenRY.Add(DoorRight, moveTimeA, -rotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlagA);

                                    buttonRenderer.material.SetColor("_EmissionColor", Color.white / 3);

                                    audioSource.PlayOneShot(DoorSound, 1.0F);
                                    break;
                            }
                        }
                    }

                }

            }
        }
    }
}
