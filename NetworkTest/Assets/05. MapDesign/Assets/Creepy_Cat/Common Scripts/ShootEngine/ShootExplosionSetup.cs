// Code by Unity (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;
using System.Collections;

namespace creepycat.scifikitvol4
{

    // Applies an explosion force to all nearby rigidbodies
    public class ShootExplosionSetup : MonoBehaviour
    {
        [Tooltip("Explosion Setup")]
        public float radius = 5.0F;
        public float power = 10.0F;

        void Start()
        {
            Vector3 explosionPos = transform.position;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);

            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                    rb.AddExplosionForce(Random.Range(power, power/1.5f), explosionPos, radius, 3.0F);
            }
        }
    }

}