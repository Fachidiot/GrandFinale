// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 public class Teleporter : MonoBehaviour { 
     public GameObject Departure;
     public GameObject Destination;
     [Header("")]
     public GameObject Player;
     [Header("")]
     public AudioClip TeleportSound;
    
     private Collider ColliderA;
     private Collider ColliderB;
     private AudioSource audioSource;

     private bool IsTeleported = false;

     void Start () {
        ColliderA = Departure.GetComponent<Collider>();
        ColliderB = Destination.GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
     }
     
     // Teleport to the new position
     public void OnTriggerEnter(Collider other){ 
        //Debug.Log("Enter!");

        // Go teleport
        if(IsTeleported == false){
            IsTeleported = true;
            audioSource.PlayOneShot(TeleportSound);
            Player.transform.position = Destination.transform.position;
        }

        // I just teleported
        if(IsTeleported == true){
            //Debug.Log("Arrival!");
        }

     }

     // Reset the teleport flag
     public void OnTriggerExit(Collider other){ 
        if(IsTeleported == true){
          IsTeleported = false;
        }
     }

 }

