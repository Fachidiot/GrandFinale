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

    public class WindowOpen : MonoBehaviour
    {
        public GameObject DoorLeftA;

        [SerializeField]
        public GameObject[] emissiveList;

        public GameObject DoorButtonA;

        public Light SpotLightA;

        public AudioClip DoorSound;

        public float moveTimeA = 2.0f;
        public float emissionIntensity = 3.0f;
        public float moveMax = 0.6f;

        private bool SwitchAnimLeft = false;
        private float newIntensityA = 0.0f;

        private Renderer buttonRendererA;
        private Renderer buttonRendererB;
        private Renderer emissiveRenderer;
        private AudioSource audioSource;

        private bool AnimationFlag = false;

        void Start()
        {
            buttonRendererA = DoorButtonA.GetComponent<Renderer>();
            audioSource = GetComponent<AudioSource>();

            illumValueDec();
        }

        void EndAnimationFlag()
        {
            AnimationFlag = false;
        }

        // Fading light/illum procedures
        void LightFading()
        {
            if (SwitchAnimLeft == true) newIntensityA = emissionIntensity;
            if (SwitchAnimLeft == false) newIntensityA = 0.0f;

            SpotLightA.intensity = Mathf.Lerp(SpotLightA.intensity, newIntensityA, Time.deltaTime * 0.4f);
        }

        void illumValueInc()
        {
            buttonRendererA.material.SetColor("_EmissionColor", Color.white / 3.0f);

            for (int i = 0; i < emissiveList.Length; i++)
            {
                emissiveRenderer = emissiveList[i].GetComponent<Renderer>();
                emissiveRenderer.material.SetColor("_EmissionColor", Color.white * 1.5f);
            }
        }

        void illumValueDec()
        {
            buttonRendererA.material.SetColor("_EmissionColor", Color.white * 1.5f);

            for (int i = 0; i < emissiveList.Length; i++)
            {
                emissiveRenderer = emissiveList[i].GetComponent<Renderer>();
                emissiveRenderer.material.SetColor("_EmissionColor", Color.white / 3.0f);
            }
        }

        // Update is called once per frame    
        void Update()
        {

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
                    if (hit.transform == DoorButtonA.transform)
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
                                    TweenX.Add(DoorLeftA, moveTimeA, -moveMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);

                                    illumValueDec();

                                    audioSource.PlayOneShot(DoorSound, 1.0F);
                                    break;

                                case true:
                                    TweenX.Add(DoorLeftA, moveTimeA, moveMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);

                                    illumValueInc();

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