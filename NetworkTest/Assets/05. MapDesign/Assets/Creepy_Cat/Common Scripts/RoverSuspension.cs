// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (you'll be credited in the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;
using System.Collections;

namespace creepycat.scifikitvol4{

    [ExecuteInEditMode()]
    public class RoverSuspension : MonoBehaviour
    {
        [Range(0, 20)]
        [Tooltip("Natural frequency of the suspension springs. Describes bounciness of the suspension.")]
        public float naturalFrequency = 10;

        [Range(0, 3)]
        [Tooltip("Damping ratio of the suspension springs. Describes how fast the spring returns back after a bounce. ")]
        public float dampingRatio = 0.8f;

        [Range(-1, 1)]
        [Tooltip("The distance along the Y axis the suspension forces application point is offset below the center of mass")]
        public float forceShift = 0.03f;

        [Tooltip("Adjust the length of the suspension springs according to the natural frequency and damping ratio. When off, can cause unrealistic suspension bounce.")]
        public bool setSuspensionDistance = true;

        void Update()
        {
            // work out the stiffness and damper parameters based on the better spring model
            foreach (WheelCollider wc in GetComponentsInChildren<WheelCollider>())
            {
                JointSpring spring = wc.suspensionSpring;

                spring.spring = Mathf.Pow(Mathf.Sqrt(wc.sprungMass) * naturalFrequency, 2);
                spring.damper = 2 * dampingRatio * Mathf.Sqrt(spring.spring * wc.sprungMass);

                wc.suspensionSpring = spring;

                Vector3 wheelRelativeBody = transform.InverseTransformPoint(wc.transform.position);
                float distance = GetComponent<Rigidbody>().centerOfMass.y - wheelRelativeBody.y + wc.radius;

                wc.forceAppPointDistance = distance - forceShift;

                // the following line makes sure the spring force at maximum droop is exactly zero
                if (spring.targetPosition > 0 && setSuspensionDistance)
                    wc.suspensionDistance = wc.sprungMass * Physics.gravity.magnitude / (spring.targetPosition * spring.spring);
            }
        }

        // uncomment OnGUI to observe how parameters change

        /*
            public void OnGUI()
            {
                foreach (WheelCollider wc in GetComponentsInChildren<WheelCollider>()) {
                    GUILayout.Label (string.Format("{0} sprung: {1}, k: {2}, d: {3}", wc.name, wc.sprungMass, wc.suspensionSpring.spring, wc.suspensionSpring.damper));
                }

                var rb = GetComponent<Rigidbody> ();

                GUILayout.Label ("Inertia: " + rb.inertiaTensor);
                GUILayout.Label ("Mass: " + rb.mass);
                GUILayout.Label ("Center: " + rb.centerOfMass);
            }
        */

    }

}