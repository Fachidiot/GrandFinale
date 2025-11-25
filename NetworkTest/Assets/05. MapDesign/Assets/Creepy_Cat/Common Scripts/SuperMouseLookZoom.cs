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

    // A class to zoom smoothly with the right mouse button a camera
    public class SuperMouseLookZoom : MonoBehaviour
    {
        public Camera CameraObject;
        [Header("")]
        public float zoomMultiplier = 2;
        public float defaultFov = 90;
        public float zoomDuration = 2;
        [Header("")]
        public KeyCode zoomKey = KeyCode.Mouse2;

        void ZoomCamera(float target){
            float angle = Mathf.Abs((defaultFov / zoomMultiplier) - defaultFov);
            CameraObject.fieldOfView = Mathf.MoveTowards(CameraObject.fieldOfView, target, angle / zoomDuration * Time.deltaTime);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(zoomKey)){
              ZoomCamera(defaultFov / zoomMultiplier);
            }
            else if (CameraObject.fieldOfView != defaultFov)
            {
              ZoomCamera(defaultFov);
            }
        }
    }

}

