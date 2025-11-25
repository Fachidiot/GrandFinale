// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Example of the famous DISPLAY OPTIMIZATION!!! (Sigh...) 
// Help : https://docs.unity3d.com/Manual/FrustumSizeAtDistance.html
namespace creepycat.scifikitvol4 {

    public class AutomaticObjectOptimizer : MonoBehaviour{

        [SerializeField]
        public GameObject[] objectList;

        public enum colorsEnum{red,green, blue, magenta, yellow, cyan, white };
        public colorsEnum gizmoColor;

        private bool SwitchObject = false;
        private Renderer objectRenderer;

        // Start is called before the first frame update
        void Start(){

            for (int i = 0; i < objectList.Length; i++){
                if (objectList[i]){
                    objectList[i].SetActive( false );
                }
            }

        }

        // Update is called once per frame
        void Update(){
            HideObject(); 
        }

        // Draw a yellow cube at the transform position
        void OnDrawGizmosSelected(){
            // Color gizmo selector
            switch(gizmoColor) {
            case colorsEnum.red:
                Gizmos.color = Color.red;
                break;
            case colorsEnum.green:
                Gizmos.color = Color.green;
                break;
            case colorsEnum.blue:
                Gizmos.color = Color.blue;
                break;
            case colorsEnum.magenta:
                Gizmos.color = Color.magenta;
                break;
            case colorsEnum.yellow:
                Gizmos.color = Color.yellow;
                break;
            case colorsEnum.cyan:
                Gizmos.color = Color.cyan;
                break;
            case colorsEnum.white:
                Gizmos.color = Color.white;
                break;
            }

            for (int i = 0; i < objectList.Length; i++){
                if(objectList[i].activeInHierarchy == true){

                    Gizmos.DrawWireSphere(objectList[i].transform.position, 0.5f );
                    Gizmos.DrawIcon(objectList[i].transform.position, "CreepyGizmoIcon.png", true);
                }

            }
        }


        // Switch view procedure
        void HideObject(){
            if (SwitchObject == false){
                for (int i = 0; i < objectList.Length; i++){
                        if (objectList[i]){
                        objectList[i].SetActive( false );
                        }
                }
            }

            if (SwitchObject == true){
                for (int i = 0; i < objectList.Length; i++){
                    if (objectList[i]){
                        objectList[i].SetActive( true );
                    }
                }
            }
        }

        void OnTriggerEnter(Collider other){

            if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("MainCamera") ) {
                SwitchObject = true;
            }
        }

        void OnTriggerExit(Collider other){

            if (other.gameObject.CompareTag("Player")  || other.gameObject.CompareTag("MainCamera") ) {
                SwitchObject = false;
            }
        }


    }

}
