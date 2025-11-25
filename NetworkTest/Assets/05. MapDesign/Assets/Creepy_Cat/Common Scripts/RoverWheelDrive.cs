// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (you'll be credited in the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;
using System.Collections;

namespace creepycat.scifikitvol4{

    public class RoverWheelDrive : MonoBehaviour {

	    private WheelCollider[] wheels;

        [Tooltip("Maximum steering angle of the wheels")]
        [Range(0, 90)]
	    public float steerMax = 30f;

        [Tooltip("Maximum torque applied to the driving wheels")]
        [Range(0, 1000)]
	    public float motorMax = 300f;

        public GameObject wheelShape;

        public GameObject FireStopIllum;
        public GameObject FireStopLightLeft;
        public GameObject FireStopLightRight;

        // Private vars
        private float steer = 0f;
        private float motor = 0f;

        private Renderer FireStopRenderer;


	    // Find all the WheelColliders in the hierarchy
	    public void Start(){
		    wheels = GetComponentsInChildren<WheelCollider>();

		    for (int i = 0; i < wheels.Length; ++i) 
		    {
			    var wheel = wheels [i];

			    // create wheel shapes only when needed
			    if (wheelShape != null)
			    {
				    var ws = GameObject.Instantiate (wheelShape);
				    ws.transform.parent = wheel.transform;
			    }
		    }

            FireStopRenderer = FireStopIllum.GetComponent<Renderer>();
            FireStopRenderer.material.SetColor("_EmissionColor", Color.white * 0.0f);
	    }



	    // simple approach to updating wheels
	    public void Update()
	    {
		    steer = steerMax * Input.GetAxis("Horizontal");
            motor  = motorMax * Input.GetAxis("Vertical");

            // Fire stop light / decal setup
            if (Input.GetAxis("Vertical")<0.0f){
                FireStopRenderer.material.SetColor("_EmissionColor", Color.white * 1.0f);
                FireStopLightLeft.SetActive(true);
                FireStopLightRight.SetActive(true);
            }else{
                FireStopRenderer.material.SetColor("_EmissionColor", Color.white * 0.0f);
                FireStopLightLeft.SetActive(false);
                FireStopLightRight.SetActive(false);
            }

		    foreach (WheelCollider wheel in wheels)
		    {
			    // a simple car where front wheels steer while rear ones drive
			    if (wheel.transform.localPosition.z > 0)
                    wheel.steerAngle = steer;

			    if (wheel.transform.localPosition.z < 0)
                    wheel.motorTorque =  motor;


			    // Positionning wheels

			    if (wheelShape) 
			    {
				    Quaternion q;
				    Vector3 p;
				    wheel.GetWorldPose (out p, out q);

				    // assume that the only child of the wheelcollider is the wheel shape
				    Transform shapeTransform = wheel.transform.GetChild (0);
				    shapeTransform.position = p;
				    shapeTransform.rotation = q;
			    }

                // Detect ground hit or not
                WheelHit hit;

                if (wheel.GetGroundHit(out hit) == false){
                    //Debug.Log("Car quit floor");
                }

		    }
	    }
    }

}