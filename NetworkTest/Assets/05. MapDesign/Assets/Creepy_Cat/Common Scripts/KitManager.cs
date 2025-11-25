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
    // A classe to manage some aspects of the kit
    public class KitManager : MonoBehaviour{

    [Header("")]
    [Tooltip("World setup")]
    public float worldGravity = -11.0f; // Default gravity
    public bool useZeroGravity = false;

    [Header("")]
    [Tooltip("Select the gravity zero key")]
    public KeyCode switchGravity = KeyCode.G;

    [Header("")]
    [Tooltip("Time setup")]
    public float slowMotionTimeScale = 0.2f;
    public bool useSlowMotion = false;

    [Header("")]
    [Tooltip("Select the slow motion key")]
    public KeyCode switchSlowMotion = KeyCode.A;

    [Header("")]
    [Tooltip("Select the gravity switch sounds")]
    public bool PlayGravitySound = true;
    public AudioClip GravitySwitchSound;
    public AudioClip GravityOnSound;
    public AudioClip GravityOffSound;
    
    // For cat only
    private AudioSource audioSource;

    [System.Serializable]
    public class AudioSourceData
    {
        public AudioSource audioSource;
        public float defaultPitch;
    }

    AudioSourceData[] audioSources;

    // Start is called before the first frame update
    void Start(){
        audioSource = GetComponent<AudioSource>();
        ChangeGravity();
        InitSlowMotion();
    }

    // Init the world gravity
    void ChangeGravity(){
        if(useZeroGravity == false){
            Physics.gravity = new Vector3(0, worldGravity, 0);

            if (PlayGravitySound == true){
                audioSource.PlayOneShot(GravityOffSound);
                audioSource.PlayOneShot(GravitySwitchSound); 
            }
        }else{
            Physics.gravity = new Vector3(0, 0, 0);
            
            if (PlayGravitySound == true){
                audioSource.PlayOneShot(GravityOnSound);
                audioSource.PlayOneShot(GravitySwitchSound);
            }
        }
    }
    
       //Find all AudioSources in the Scene and save their default pitch values
    void InitSlowMotion(){
        
        AudioSource[] audios = FindObjectsOfType<AudioSource>();
        audioSources = new AudioSourceData[audios.Length];

        for (int i = 0; i < audios.Length; i++)
        {
            AudioSourceData tmpData = new AudioSourceData();
            tmpData.audioSource = audios[i];
            tmpData.defaultPitch = audios[i].pitch;
            audioSources[i] = tmpData;
        }

        ChangeSlowMotion(useSlowMotion);
    }

    void ChangeSlowMotion(bool enabled){
        Time.timeScale = enabled ? slowMotionTimeScale : 1;
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i].audioSource)
            {
                audioSources[i].audioSource.pitch = audioSources[i].defaultPitch * Time.timeScale;
            }
        }
    }

    // Update is called once per frame
    void Update(){

       // If you want to play with gravity :)
       if (Input.GetKeyDown(switchGravity)){
            useZeroGravity=!useZeroGravity;
            ChangeGravity();
       }

       // If you want to play with slowmotion :)
       if (Input.GetKeyDown(switchSlowMotion)){
            useSlowMotion=!useSlowMotion;
            ChangeSlowMotion(useSlowMotion);
       }

       // Stuff about cursor...
       Cursor.lockState = CursorLockMode.Locked;
       Cursor.visible = false;
    }
}

}