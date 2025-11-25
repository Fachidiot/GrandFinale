// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Example of the famous DISPLAY OPTIMIZATION!!! (Sigh...)
public class AutomaticLightOptimizer : MonoBehaviour
{

    // Light list and gameobject illum to switch
    [SerializeField]
    public Light[] lightList;

    [SerializeField]
    public GameObject[] emissiveList;


    public float fadeTime = 1.2f;
    public float lightPower = 1.5f;

    private bool SwitchAnim = true;
    private float newIntensity;

    private Renderer emissiveRenderer;
    private Color tmpColor;

    // Start is called before the first frame update
    void Start(){
        if (emissiveList.Length>0){
            emissiveRenderer = emissiveList[0].GetComponent<Renderer>();
            tmpColor= emissiveRenderer.material.GetColor("_EmissionColor");
            illumValueDec();
        }

        for (int i = 0; i < lightList.Length; i++){
            lightList[i].enabled = false;
            lightList[i].intensity = 0.0f;
        }

    }

    // Update is called once per frame
    void Update(){
         LightFading(); 
    }

    // Fading light procedures
    void LightFading()
    {
        if (SwitchAnim == false) newIntensity = lightPower;
        if (SwitchAnim == true) newIntensity = 0.0f;

        for (int i = 0; i < lightList.Length; i++){
            lightList[i].intensity = Mathf.Lerp(lightList[i].intensity, newIntensity, Time.deltaTime * fadeTime);

            if (lightList[i].intensity <= 0.01f){
                lightList[i].enabled = false;
            }else{
                lightList[i].enabled = true;
            }
        }
    }

    void OnTriggerEnter(Collider other){

        if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("MainCamera") ) {
            SwitchAnim = false;
            illumValueInc();
        }


    }

    void OnTriggerExit(Collider other){

        if (other.gameObject.CompareTag("Player")  || other.gameObject.CompareTag("MainCamera") ) {
           SwitchAnim = true;
           illumValueDec();
        }

    }

    void illumValueInc(){
 
        if (emissiveList.Length>0){
            for (int i = 0; i < emissiveList.Length; i++){
                emissiveRenderer = emissiveList[i].GetComponent<Renderer>();
                emissiveRenderer.material.SetColor("_EmissionColor", tmpColor * 1.5f);
            }
        }
    }

    void illumValueDec(){

        if (emissiveList.Length > 0){
            for (int i = 0; i < emissiveList.Length; i++){
                emissiveRenderer = emissiveList[i].GetComponent<Renderer>();
                emissiveRenderer.material.SetColor("_EmissionColor", Color.black);
            }
        }

    }


}
