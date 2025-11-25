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
// Help : https://learnopengl.com/Guest-Articles/2021/Scene/Frustum-Culling
// Not perfect but it work for the most of case...

namespace creepycat.scifikitvol4 {

    public class AutomaticObjectFrustrum : MonoBehaviour {
        public Camera displayCamera;
        public float cullingDistance = 100f;

        [SerializeField]
        public List<GameObject> objectList;

        public float visionAngle = 180f; // Angle de vision en degrés

        public enum colorsEnum{red,green, blue, magenta, yellow, cyan, white };
        public colorsEnum gizmoColor;


        private CullingGroup cullingGroup;
        private BoundingSphere[] boundingSpheres;

        private void Start(){

            if (displayCamera == null){
                displayCamera = Camera.main;
            }

            cullingGroup = new CullingGroup();
            cullingGroup.targetCamera = displayCamera;

            boundingSpheres = new BoundingSphere[objectList.Count];

            for (int i = 0; i < objectList.Count; i++){
                GameObject obj = objectList[i];
                boundingSpheres[i] = new BoundingSphere(obj.transform.position, cullingDistance);
            }

            //hell to understand those shit...
            cullingGroup.SetBoundingSpheres(boundingSpheres);
           

            cullingGroup.SetBoundingSphereCount(objectList.Count);
            cullingGroup.SetDistanceReferencePoint(displayCamera.transform);

            cullingGroup.onStateChanged += OnStateChanged;
        }

        private void Update(){
            // Mettre à jour les positions des sphères englobantes
            for (int i = 0; i < objectList.Count; i++){
                boundingSpheres[i].position = objectList[i].transform.position;
            }

            // Mettre à jour les sphères englobantes dans CullingGroup
            cullingGroup.SetBoundingSpheres(boundingSpheres);

            Vector3 cameraPosition = displayCamera.transform.position;
            Vector3 cameraFront = displayCamera.transform.forward; // Vecteur de direction de la caméra

            foreach (GameObject obj in objectList)
            {
                Vector3 directionToObject = obj.transform.position - cameraPosition;
                float dotProduct = Vector3.Dot(cameraFront, directionToObject.normalized);
                float objectAngle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

                if (objectAngle < visionAngle / 2) // Si l'objet est dans l'angle de vision
                {
                    float distanceToCamera = directionToObject.magnitude;

                    if (distanceToCamera > cullingDistance)
                    {
                        obj.SetActive(false);
                    }else{
                        obj.SetActive(true);
                    }
                }else{
                    obj.SetActive(false); // Si l'objet est en dehors de l'angle de vision
                }
            }
        }

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

            // Draw a yellow cube at the transform position
            for (int i = 0; i < objectList.Count; i++){
                if(objectList[i].activeInHierarchy == true){

                    Gizmos.DrawWireCube(objectList[i].transform.position, new Vector3(3.0f,3.0f,3.0f) );
                    Gizmos.DrawIcon(objectList[i].transform.position, "CreepyGizmoIcon.png", true);

                }

            }
        }

        private void OnStateChanged(CullingGroupEvent sphere)
        {
            if (sphere.hasBecomeInvisible)
            {
                objectList[sphere.index].SetActive(false);
            }
            else if (sphere.hasBecomeVisible)
            {
                objectList[sphere.index].SetActive(true);
            }
        }

        private void OnDestroy(){
            cullingGroup.Dispose();
        }

    }
}