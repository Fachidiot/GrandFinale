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

    public class ObjectTowardOnlyYaw : MonoBehaviour
    {
	    public Transform targetGameobject;
        public float turretDegreesPerSecond = 45.0f;

        private Quaternion qTurret;
	    private Transform trans;

	    void Start(){
		    trans = transform;

            if (targetGameobject==null){
                    targetGameobject = Camera.main.transform;
            }
	    }

	    void Update(){
		    //Debug.DrawRay(gunGameobject.position, gunGameobject.fozrward * 20.0f);

		    float distanceToPlane = Vector3.Dot(trans.up, targetGameobject.position - trans.position);
		    Vector3 planePoint = targetGameobject.position - trans.up * distanceToPlane;

		    qTurret = Quaternion.LookRotation(planePoint - trans.position, transform.up);
		    transform.rotation = Quaternion.RotateTowards(transform.rotation, qTurret, turretDegreesPerSecond * Time.deltaTime);
	    }

    }

}
