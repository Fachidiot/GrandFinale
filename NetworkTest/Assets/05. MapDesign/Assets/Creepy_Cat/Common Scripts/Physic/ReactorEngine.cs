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

    // Just for fun class to help you to create some reactor with particle afterburner off / on
    public class ReactorEngine : MonoBehaviour
    {
        Rigidbody m_Rigidbody;
        public float ThrustMax = 20f;
        public ParticleSystem ParticleAfterburner;
        public KeyCode thrustKey = KeyCode.T;

        public enum controlSelection{
            SelectKey,
            Automatic,
        }

        public controlSelection selectMethod;

        void Start()
        {
            //Fetch the Rigidbody from the GameObject with this script attached
            m_Rigidbody = GetComponent<Rigidbody>();
            ParticleAfterburner.Stop();
        }

        void FixedUpdate()
        {
            switch(selectMethod){
                case controlSelection.SelectKey:
                    if (Input.GetKey(thrustKey)){
                        m_Rigidbody.AddForce(transform.up * ThrustMax);
                        ParticleAfterburner.Play();
                    }else{
                        ParticleAfterburner.Stop();
                    }

                    break;

                case controlSelection.Automatic:
                    m_Rigidbody.AddForce(transform.up * ThrustMax);
                    ParticleAfterburner.Play();
                    break;
            } 

        }

    }

}