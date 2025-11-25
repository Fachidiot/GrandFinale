using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace creepycat.scifikitvol4
{

    public class MineProximity : MonoBehaviour
    {
        public GameObject MineObject;
        public GameObject ExplosionEmitter;

        public GameObject GreenFlashLight;
        public AudioClip GreenAudio;

        public GameObject RedFlashLight;
        public AudioClip RedAudio;

        private AudioSource audioData;
        private bool Activator = false;

        // Start is called before the first frame update
        void Start()
        {
            GreenFlashLight.SetActive(true);
            RedFlashLight.SetActive(false);    
            audioData = GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {

            if (audioData.isPlaying == false && Activator == true)
            {
                ExplosionEmitter = Instantiate(ExplosionEmitter, transform.position, transform.rotation) as GameObject;
                Destroy(MineObject);
            }
        
        }


        private void OnTriggerEnter(Collider other)
        {
            // If the player is in the zone
            if (other.gameObject.CompareTag("Player")) {

                GreenFlashLight.SetActive(false);
                RedFlashLight.SetActive(true);
                audioData.PlayOneShot(RedAudio);
                audioData.PlayOneShot(GreenAudio);
                Activator = true;
            
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // If the player is out the zone
            if (other.gameObject.CompareTag("Player")) {

                GreenFlashLight.SetActive(true);
                RedFlashLight.SetActive(false);    
                audioData.PlayOneShot(GreenAudio);
                Activator = false;

            }
        }


    }

}