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

namespace creepycat.scifikitvol4
{

    public class LightSwitch : MonoBehaviour
    {
        // Light list and gameobject illum to switch
        [SerializeField]
        public Light[] lightList;

        [SerializeField]
        public GameObject[] emissiveList;

        [Header("")]
        // Clicked button object
        public GameObject lightButton;

        [Header("")]
        // Clicked button sound
        public AudioClip lightSound;

        // What mouse button to activate lights
        public enum WhatButton { Left, Right, Wheel };
        public WhatButton mouseButton;
        private KeyCode tmpKeyCode=KeyCode.Mouse0;

        [Header("")]
        public float fadeTime = 1.2f;
        public bool StartLightOn = true;
        
        private bool SwitchAnim = false;

        private float newIntensity;
        private float emissionIntensity = 1.0f;

        private Renderer buttonRenderer;
        private Renderer emissiveRenderer;

        private AudioSource audioSource;
        private Color tmpColor;

        void Start()
        {
            buttonRenderer = lightButton.GetComponent<Renderer>();
            audioSource = GetComponent<AudioSource>();

            // I get the first emissive list Color to make the color change
            emissiveRenderer = emissiveList[0].GetComponent<Renderer>();
            tmpColor= emissiveRenderer.material.GetColor("_EmissionColor");

            // I get the first light intensity to apply it to the others
            emissionIntensity = lightList[0].intensity;

            // I get the renderer for the button
            buttonRenderer.material.SetColor("_EmissionColor", Color.white * 1.5f);

            // i switch the light off at start (not sure that all to do..)
            if (StartLightOn == false)
            {
                SwitchAnim = true;
                LightFading();
                illumValueDec();
            }
        }

        // Fading light/illum procedures
        void LightFading()
        {
            if (SwitchAnim == false) newIntensity = emissionIntensity;
            if (SwitchAnim == true) newIntensity = 0.0f;

            for (int i = 0; i < lightList.Length; i++)
            {
                lightList[i].intensity = Mathf.Lerp(lightList[i].intensity, newIntensity, Time.deltaTime * fadeTime);
            }
        }

        void illumValueInc()
        {
            buttonRenderer.material.SetColor("_EmissionColor", Color.white * 1.5f);

            for (int i = 0; i < emissiveList.Length; i++)
            {
                emissiveRenderer = emissiveList[i].GetComponent<Renderer>();
                emissiveRenderer.material.SetColor("_EmissionColor", tmpColor * 1.5f);
            }
        }

        void illumValueDec()
        {
            buttonRenderer.material.SetColor("_EmissionColor", Color.white / 3);

            for (int i = 0; i < emissiveList.Length; i++)
            {
                emissiveRenderer = emissiveList[i].GetComponent<Renderer>();
                emissiveRenderer.material.SetColor("_EmissionColor", tmpColor / 3.0f);
            }
        }

        // Update is called once per frame    
        void Update()
        {

            // Fading light
            LightFading();
            
            // Enum for mouse button test
            switch(mouseButton){
            case WhatButton.Left:
                tmpKeyCode=KeyCode.Mouse0;
                break;
            case WhatButton.Right:
                tmpKeyCode=KeyCode.Mouse1;
                break;
            case WhatButton.Wheel:
                tmpKeyCode=KeyCode.Mouse2;
                break;
            }

            // If mouse click
            if (Input.GetKeyDown(tmpKeyCode)){

                // Get the gameobject clicked
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // If something clicked
                if (Physics.Raycast(ray, out hit))
                {

                    // If it's my button
                    if (hit.transform == lightButton.transform)
                    {
                        SwitchAnim = !SwitchAnim;

                        // Anim switch var
                        switch (SwitchAnim)
                        {
                            // If no we launch all the things needed
                            case false:

                                illumValueInc();

                                audioSource.PlayOneShot(lightSound, 1.0F);
                                break;

                            case true:

                                illumValueDec();

                                audioSource.PlayOneShot(lightSound, 1.0F);
                                break;
                        }

                    }

                }
            }
        }

    }

}