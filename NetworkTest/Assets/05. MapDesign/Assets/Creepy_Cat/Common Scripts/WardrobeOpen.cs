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

    public class WardrobeOpen : MonoBehaviour
    {
        public GameObject DoorLeftA;
        public GameObject DoorRightA;

        public GameObject DoorLeftB;
        public GameObject DoorRightB;

        public GameObject DoorLeftButton;
        public GameObject DoorRightButton;

        public Light CaseLightLeft;
        public Light CaseLightRight;

        public AudioClip DoorSound;

        public float moveTimeA = 2.0f;

        public float emissionIntensity = 3.0f;

        private bool SwitchAnimLeft = false;
        private bool SwitchAnimRight = false;

        private float rotateMax = 90f;
        private float newIntensityA = 0.0f;
        private float newIntensityB = 0.0f;

        private Renderer buttonRendererLeft;
        private Renderer buttonRendererRight;
        private AudioSource audioSource;

        private bool AnimationFlagA = false;
        private bool AnimationFlagB = false;

        void Start()
        {
            buttonRendererLeft = DoorLeftButton.GetComponent<Renderer>();
            buttonRendererRight = DoorRightButton.GetComponent<Renderer>();
            audioSource = GetComponent<AudioSource>();

            buttonRendererLeft.material.SetColor("_EmissionColor", Color.white * 1.5f);
            buttonRendererRight.material.SetColor("_EmissionColor", Color.white * 1.5f);
        }

        void EndAnimationFlagA()
        {
            AnimationFlagA = false;
        }

        void EndAnimationFlagB()
        {
            AnimationFlagB = false;
        }

        void LightFading()
        {
            if (SwitchAnimLeft == true) newIntensityA = emissionIntensity;
            if (SwitchAnimLeft == false) newIntensityA = 0.0f;

            if (SwitchAnimRight == true) newIntensityB = emissionIntensity;
            if (SwitchAnimRight == false) newIntensityB = 0.0f;

            CaseLightLeft.intensity = Mathf.Lerp(CaseLightLeft.intensity, newIntensityA, Time.deltaTime * 0.2f);
            CaseLightRight.intensity = Mathf.Lerp(CaseLightRight.intensity, newIntensityB, Time.deltaTime * 0.2f);
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
                    if (hit.transform == DoorLeftButton.transform)
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
                                    TweenRY.Add(DoorLeftA, moveTimeA, 0).From(rotateMax).EaseInOutCubic().Then(EndAnimationFlagA);
                                    TweenRY.Add(DoorRightA, moveTimeA, 0).From(-rotateMax).EaseInOutCubic().Then(EndAnimationFlagA);

                                    buttonRendererLeft.material.SetColor("_EmissionColor", Color.white * 1.5f);

                                    audioSource.PlayOneShot(DoorSound, 1.0F);
                                    break;

                                case true:
                                    TweenRY.Add(DoorLeftA, moveTimeA, rotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlagA);
                                    TweenRY.Add(DoorRightA, moveTimeA, -rotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlagA);

                                    buttonRendererLeft.material.SetColor("_EmissionColor", Color.white / 3);

                                    audioSource.PlayOneShot(DoorSound, 1.0F);
                                    break;
                            }
                        }
                    }

                    // If it's my button
                    if (hit.transform == DoorRightButton.transform)
                    {

                        if (AnimationFlagB == false)
                        {
                            SwitchAnimRight = !SwitchAnimRight;
                            AnimationFlagB = true;

                            // Anim switch var
                            switch (SwitchAnimRight)
                            {
                                // If no we launch all the things needed
                                case false:
                                    TweenRY.Add(DoorLeftB, moveTimeA, 0).From(rotateMax).EaseInOutCubic().Then(EndAnimationFlagB);
                                    TweenRY.Add(DoorRightB, moveTimeA, 0).From(-rotateMax).EaseInOutCubic().Then(EndAnimationFlagB);

                                    buttonRendererRight.material.SetColor("_EmissionColor", Color.white * 1.5f);

                                    audioSource.PlayOneShot(DoorSound, 1.0F);
                                    break;

                                case true:
                                    TweenRY.Add(DoorLeftB, moveTimeA, rotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlagB);
                                    TweenRY.Add(DoorRightB, moveTimeA, -rotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlagB);

                                    buttonRendererRight.material.SetColor("_EmissionColor", Color.white / 3);

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
