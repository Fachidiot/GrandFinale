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

    public class CrateOpen : MonoBehaviour
    {
        public GameObject CrateTop;
        public GameObject CrateButton;
 
         [SerializeField]
        public Light[] lightList;

        public AudioClip CrateSound;

        public float OpenTime = 2.0f;

        public float EmissionIntensity = 1.2f;
        
        private  float fadeTime = 1.2f;
        private bool SwitchAnim = false;
        private bool SwitchLight = false;
        private float RotateMax = -90f;

        private Renderer ButtonRenderer;
        private AudioSource AudioSource;

        private float newIntensity;
        private bool AnimationFlag = false;

        void Start(){
            ButtonRenderer = CrateButton.GetComponent<Renderer>();
            AudioSource = GetComponent<AudioSource>();

            // 1.9 optimization
            //LightFading();

            for (int i = 0; i < lightList.Length; i++){
                lightList[i].enabled = false;
                lightList[i].intensity = 0.0f;
            }
        }

        void EndAnimationFlag(){
            AnimationFlag = false;
        }

        // 1.9 optimization
        void OnTriggerEnter(Collider other){
            if (other.gameObject.CompareTag("Player")  || other.gameObject.CompareTag("MainCamera") ) {

                if (SwitchAnim == false){
                SwitchLight = true;
                }

            }
        }

        void OnTriggerExit(Collider other){
            if (other.gameObject.CompareTag("Player")  || other.gameObject.CompareTag("MainCamera") ) {
                SwitchLight = false;
            }
        }

        // 1.9 optimization
        void LightFading()
        {
            if (SwitchLight == true) newIntensity = 1.5f;
            if (SwitchLight == false) newIntensity = 0.0f;

            for (int i = 0; i < lightList.Length; i++){
                lightList[i].intensity = Mathf.Lerp(lightList[i].intensity, newIntensity, Time.deltaTime * fadeTime);

                if (lightList[i].intensity <= 0.01f){
                    lightList[i].enabled = false;
                }else{
                    lightList[i].enabled = true;
                }
            }
        }

        // Update is called once per frame    
        void Update(){
            LightFading();

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
                    if (hit.transform == CrateButton.transform)
                    {
                        if (AnimationFlag == false)
                        {
                            SwitchAnim = !SwitchAnim;
                            AnimationFlag = true;

                            // Anim switch var
                            switch (SwitchAnim)
                            {
                                // If no we launch all the things needed
                                case false:
                                    TweenRX.Add(CrateTop, OpenTime, 0).From(RotateMax).EaseInOutCubic().Then(EndAnimationFlag);
                                    ButtonRenderer.material.SetColor("_EmissionColor", Color.white * EmissionIntensity);

                                    SwitchLight = true; // 1.9 optimization
                                    AudioSource.PlayOneShot(CrateSound, 1.0F);
                                    break;

                                case true:
                                    TweenRX.Add(CrateTop, OpenTime, RotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
                                    ButtonRenderer.material.SetColor("_EmissionColor", Color.red * EmissionIntensity);

                                    SwitchLight = false; // 1.9 optimization
                                    AudioSource.PlayOneShot(CrateSound, 1.0F);
                                    break;
                            }
                        }
                    }

                }
            }
        }

    }
}