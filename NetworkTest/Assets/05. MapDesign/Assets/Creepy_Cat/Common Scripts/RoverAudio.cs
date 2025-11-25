// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (you'll be credited in the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;
using System.Collections;

namespace creepycat.scifikitvol4{

    public class RoverAudio : MonoBehaviour{
        public AudioSource jetSound;

        [Range(-3, 3)]
        public float LowPitch = .3f;

        [Range(-3, 3)]
        public float HighPitch = 1.5f;
    
        [Range(0.0f, 5.0f)]
        public float SpeedToRevs = 0.1f;

        private float jetPitch;
        private Rigidbody carRigidbody;

        void Awake(){
            carRigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            Vector3 myVelocity = carRigidbody.velocity;
            float forwardSpeed = transform.InverseTransformDirection(carRigidbody.velocity).z;
            float engineRevs = Mathf.Abs(forwardSpeed) * SpeedToRevs;
            jetSound.pitch = Mathf.Clamp(engineRevs, LowPitch, HighPitch);
        }

    }

}