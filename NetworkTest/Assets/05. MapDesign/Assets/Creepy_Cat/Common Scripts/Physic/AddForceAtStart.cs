// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;

namespace creepycat.scifikitvol4
{
    // A class to apply a force to a direction at start
    public class AddForceAtStart : MonoBehaviour
    {
        public float x = 2.0f;
        public float y = 0.0f;
        public float z = 0.0f;

        public Rigidbody rb;

        void Start(){
            rb = GetComponent<Rigidbody>();
            rb.AddForce(x, y, z, ForceMode.Impulse);
        }
    }

}