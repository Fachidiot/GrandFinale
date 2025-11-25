// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;
using System.Collections;

namespace creepycat.scifikitvol4
{
    // A component to add to make the emitter light flashing
    public class ShootFlashLight : MonoBehaviour {
	    private bool FirstTimeFlag = false;
        private Light Component;

	    void Start () 	{
		    FirstTimeFlag = true;

            // I change the light comp intensity
            Component = gameObject.GetComponent<Light>();
            Component.intensity = 5;
	    }
	
	    void Update () 	{
		    if (FirstTimeFlag){
                Component.intensity = Mathf.Lerp(Component.intensity, 0.0f, Time.deltaTime * 3.0f);

                // If intensity 0 or <0 i delete the gameobject
                if (Component.intensity <= 0.01f){
				    Destroy(gameObject,2f);
			    }
            }

	    }
	
    }

}