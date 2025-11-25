using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace creepycat.scifikitvol4
{
    // Allow you to make some gui element appear when you point a gameobject, see Demo_A
    public class GUIElementOnHover : MonoBehaviour
    {
        public Image guiImage; // Référence à l'objet Image GUI dans la scène Unity

        [SerializeField]
        public List<Text> guiTextList;

        [SerializeField]
        public List<string> stringTextList;

        public float maxDistance = 5.0f; // Distance maximale entre le gameobject et la caméra

        private bool oneTimeShow = false;
        private Camera mainCamera;


        void Start()
        {
            if (guiImage != null)
            {
                guiImage.gameObject.SetActive(false);
            }

            mainCamera = Camera.main;
        }

        void Update()
        {
            // Get the gameobject clicked
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // If something clicked
            if (Physics.Raycast(ray, out hit, maxDistance)) // Utilisez maxDistance ici
            {
                // If it's my gameobject, i test it one time
                if (hit.transform == gameObject.transform & oneTimeShow == false)
                {

                    oneTimeShow = true;
                    //Debug.Log("Pointed object name: "+hit.transform.name);

                    if (guiImage != null)
                    {
                        guiImage.gameObject.SetActive(true);

                        for (int i = 0; i < guiTextList.Count; i++)
                        {
                            guiTextList[i].text = stringTextList[i];
                        }

                    }

                }else{

                    if (oneTimeShow == true & hit.transform != gameObject.transform)
                    {
                        guiImage.gameObject.SetActive(false);
                        oneTimeShow = false;
                    }


                }

            }else{

                guiImage.gameObject.SetActive(false);

            }

        }
    }

}