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

    public class SelectScreenPicture : MonoBehaviour
    {

        [SerializeField]
        public GameObject[] buttonList;

        [SerializeField]
        public Texture[] textureList;

        [SerializeField]
        public Color[] lightColorList;
        public Light lightScreen;

        public GameObject screenObjectA;

        private Renderer screenRenderer;
        private Renderer emissiveRenderer;

        // Start is called before the first frame update
        void Start(){
            screenRenderer = screenObjectA.GetComponent<Renderer>();
            lightScreen.color = lightColorList[0];
        }

        // Update is called once per frame
        void Update(){

            // If mouse click
            if (Input.GetMouseButtonDown(0)){

                // Get the gameobject clicked
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // If something clicked
                if (Physics.Raycast(ray, out hit)){

                    // I shutdown the button pushed, and i change the screen bitmap
                    for (int i = 0; i < buttonList.Length; i++)
                    {

                        if (hit.transform == buttonList[i].transform)
                        {
                            emissiveRenderer = hit.transform.GetComponent<Renderer>();
                            emissiveRenderer.material.SetColor("_EmissionColor", Color.white / 3f);

                            screenRenderer.material.SetTexture("_MainTex", textureList[i]);
                            screenRenderer.material.SetTexture("_EmissionMap", textureList[i]);

                            // And the light
                            lightScreen.color = lightColorList[i];
                        }

                    }
                }

            }else{

                // If mouse up i redraw the correct button illum
                if (Input.GetMouseButtonUp(0)){
                    for (int i = 0; i < buttonList.Length; i++)
                    {

                        emissiveRenderer = buttonList[i].GetComponent<Renderer>();
                        emissiveRenderer.material.SetColor("_EmissionColor", Color.white * 1.5f);
                    }
                }


            }
        }
    }

}
