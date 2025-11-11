using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShurikenMoveForward : MonoBehaviour {

    public float speed = 8.0f;
    public float rotationSpeed = 1.0f;
    public GameObject shuriken;
	
	// Update is called once per frame
	void Update () {
        transform.Translate(Vector3.forward * Time.deltaTime * speed);

        shuriken.transform.Rotate(rotationSpeed, 0, 0, Space.Self);
        //Vector3 newRotation = new Vector3(shuriken.transform.localEulerAngles.x + (rotationSpeed * Time.deltaTime),
		//shuriken.transform.localEulerAngles.y, shuriken.transform.localEulerAngles.z);
        //shuriken.transform.localEulerAngles = newRotation;

        //Vector3 rotAngle = new Vector3(shuriken.transform.eulerAngles.x + (rotationSpeed * Time.deltaTime), 0, 0);
        //shuriken.transform.eulerAngles = rotAngle;
        // shuriken.transform.Rotate(rotAngle, shuriken.transform.eulerAngles.y, shuriken.transform.eulerAngles.z, Space.Self);
	}
}
