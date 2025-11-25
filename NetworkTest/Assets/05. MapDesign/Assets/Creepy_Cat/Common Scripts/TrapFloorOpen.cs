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

    public class TrapFloorOpen : MonoBehaviour
    {
        // Light list and gameobject illum to switch
        [SerializeField]
        public GameObject[] emissiveList;

        public GameObject DoorLeftA;
        public GameObject DoorRightA;

        public GameObject DoorButtonA;
        public GameObject DoorButtonB;

        public Light SpotLightA;
        public float fadeTime = 1.2f;

        public AudioClip DoorSound;

        public float moveTimeA = 2.0f;
        public float emissionIntensity = 3.0f;
        public float moveMax = 0.6f;


        private bool SwitchAnimLeft = false;
        private float newIntensityA = 0.0f;

        private Renderer buttonRendererA;
        private Renderer buttonRendererB;

        private AudioSource audioSource;

        private bool AnimationFlag = false;
        private Renderer buttonRenderer;
        private Renderer emissiveRenderer;

        void Start(){
            buttonRendererA = DoorButtonA.GetComponent<Renderer>();
            buttonRendererB = DoorButtonB.GetComponent<Renderer>();

            buttonRendererA.sharedMaterial.SetColor("_EmissionColor", Color.white * 1.5f);
            buttonRendererB.sharedMaterial.SetColor("_EmissionColor", Color.white * 1.5f);

            audioSource = GetComponent<AudioSource>();

            // Switch off the illum light decals
            for (int i = 0; i < emissiveList.Length; i++){
                emissiveRenderer = emissiveList[i].GetComponent<Renderer>();
                emissiveRenderer.material.SetColor("_EmissionColor", Color.white / 3.0f);
            }
        }

        // Function for end anim flag
        void EndAnimationFlag(){
            AnimationFlag = false;
        }

        // Fading light/illum procedures
        void LightFading(){
            if (SwitchAnimLeft == true) newIntensityA = emissionIntensity;
            if (SwitchAnimLeft == false) newIntensityA = 0.0f;

            SpotLightA.intensity = Mathf.Lerp(SpotLightA.intensity, newIntensityA, Time.deltaTime * fadeTime);
        }

        void illumValueInc(){
            buttonRendererA.sharedMaterial.SetColor("_EmissionColor", Color.white / 3.0f);
            buttonRendererB.sharedMaterial.SetColor("_EmissionColor", Color.white /3.0f);

            // If the light decals render do not follow the color you select in the editor/runtime:
            // Un-comment those lines (switch them) one time => and run (one time) the project => 
            // Next => Reswitch the lines comments...
            //
            // Why?: It's a problem with editor mode instanciated material (a pain to find it!!)
            // Else?: If i let the line switched there is a serie of red line just when you stop the
            //        project... don't panic, it's a red "warning one shoot" :) But i prefer 
            //        hide it for beginners...

            for (int i = 0; i < emissiveList.Length; i++){
                emissiveRenderer = emissiveList[i].GetComponent<Renderer>();
                //emissiveRenderer.sharedMaterial.SetColor("_EmissionColor", Color.white * 1.5f);
                emissiveRenderer.material.SetColor("_EmissionColor", Color.white * 1.5f);
            }
        }

        void illumValueDec(){
            buttonRendererA.sharedMaterial.SetColor("_EmissionColor", Color.white * 1.5f);
            buttonRendererB.sharedMaterial.SetColor("_EmissionColor", Color.white * 1.5f);

            // If the light decals render do not follow the color you select in the editor/runtime:
            // Un-comment those lines (switch them) one time => and run (one time) the project => 
            // Next => Reswitch the lines comments...
            //
            // Why?: It's a problem with editor mode instanciated material (a pain to find it!!)
            // Else?: If i let the line switched there is a serie of red line just when you stop the
            //        project... don't panic, it's a red "warning one shoot" :) But i prefer 
            //        hide it for beginners...

            for (int i = 0; i < emissiveList.Length; i++){
                emissiveRenderer = emissiveList[i].GetComponent<Renderer>();
                //emissiveRenderer.sharedMaterial.SetColor("_EmissionColor", Color.white / 3.0f);
                emissiveRenderer.material.SetColor("_EmissionColor", Color.white / 3.0f);
            }
        }


        // Update is called once per frame    
        void Update()
        {

            LightFading();

            // If mouse click
            if (Input.GetMouseButtonDown(0)){
                // Get the gameobject clicked
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // If something clicked
                if (Physics.Raycast(ray, out hit)){

                    // If it's my buttons
                    if (hit.transform == DoorButtonA.transform || hit.transform == DoorButtonB.transform)
                    {
                        if (AnimationFlag == false)
                        {
                            SwitchAnimLeft = !SwitchAnimLeft;
                            AnimationFlag = true;

                            // Anim switch var
                            switch (SwitchAnimLeft)
                            {
                                // If no we launch all the things needed
                                case false:
                                    TweenZ.Add(DoorLeftA, moveTimeA, moveMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
                                    TweenZ.Add(DoorRightA, moveTimeA, -moveMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);

                                    audioSource.PlayOneShot(DoorSound, 1.0F);
                                    illumValueDec();

                                    break;

                                case true:
                                    TweenZ.Add(DoorLeftA, moveTimeA, -moveMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
                                    TweenZ.Add(DoorRightA, moveTimeA, moveMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);

                                    audioSource.PlayOneShot(DoorSound, 1.0F);
                                    illumValueInc();

                                    break;
                            }
                        }
                    }

                }
            }
        }

    }
}