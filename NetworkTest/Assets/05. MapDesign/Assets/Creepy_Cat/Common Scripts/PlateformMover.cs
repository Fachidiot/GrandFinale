// Code by Unity (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Uween;

namespace creepycat.scifikitvol4
{
    // Test for plateform elevator
    public class PlateformMover : MonoBehaviour {
        [Header("Camera Controller")]
        public GameObject Player;
 
        [Header("Elevator Travel Setup")]
        public Transform StartPoint;
        public Transform EndPoint;
        public float TravelTime = 10.0f;

        [Header("Elevator Button Setup")]
        public GameObject elevatorButtonUp;
        public GameObject elevatorButtonDown;  
        public GameObject elevatorCallUp; 
        public GameObject elevatorCallDown; 
        [Header("")]
        public AudioClip clickSound;
        public AudioClip elevatorSound;
        public AudioClip startTravelSound;
        public AudioClip endTravelSound;

        // For Cat Only
        private AudioSource audioSource;

        private EasyTimer movetimer = new EasyTimer();
        private bool moveswitch = false;

        private bool elevatorTop=false;
    
        // Get components
        void Start(){
            audioSource = GetComponent<AudioSource>();
        }
    
        // Simply parent the camera controler to make the platform...
        void OnTriggerEnter(Collider other){
            Player.transform.parent = transform;
        }

        void OnTriggerExit(Collider other){
            Player.transform.parent = null;
        }

        void MoveElevatorUp(){
            audioSource.PlayOneShot(clickSound);
            audioSource.PlayOneShot(startTravelSound);

            //Debug.Log("Button Elevator Clicked");
            TweenXYZ.Add(transform.gameObject, TravelTime, EndPoint.transform.localPosition).EaseInOutCubic();
            movetimer.SetNewDuration(TravelTime);

            elevatorTop = true;
            moveswitch = true;
        }

        void MoveElevatorDown(){
            audioSource.PlayOneShot(clickSound);
            audioSource.PlayOneShot(startTravelSound);

            //Debug.Log("Button Elevator Clicked");
            TweenXYZ.Add(transform.gameObject, TravelTime, StartPoint.transform.localPosition).EaseInOutCubic();
            movetimer.SetNewDuration(TravelTime);

            elevatorTop = false;
            moveswitch = true;
        }

        void OnButtonClick(){

            if (moveswitch == false){

                if (Input.GetMouseButtonDown(0)){

                    // Get the gameobject clicked
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    // If something clicked
                    if (Physics.Raycast(ray, out hit))
                    {

                        // If it's my button
                        if (hit.transform == elevatorCallUp.transform && elevatorTop == false){
                            MoveElevatorUp();
                        }

                        // If it's my button
                        if (hit.transform == elevatorCallDown.transform && elevatorTop == true){
                            MoveElevatorDown();
                        }

                        // If it's my button
                        if (hit.transform == elevatorButtonUp.transform && elevatorTop == false){
                            MoveElevatorUp();
                        }

                        // If it's my button
                        if (hit.transform == elevatorButtonDown.transform && elevatorTop == true){
                            MoveElevatorDown();
                        }

                    }
                }

            }else{
                audioSource.clip = elevatorSound;

                if(audioSource.isPlaying==false) audioSource.Play();
            }
        }

        void Update () {
            OnButtonClick();
        }

        void FixedUpdate () {

		    if (movetimer.IsDone){

                if (moveswitch == true){
                    //Debug.Log("Time A off");
                    moveswitch = false;
                    audioSource.Stop();
                    audioSource.PlayOneShot(endTravelSound);

                    
                }

	        }


        }

    }

}

/*
using UnityEngine;
using System.Collections;
 
public class CustomInterpolation : MonoBehaviour
{
    public AnimationCurve curve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
    public Transform start;
    public Transform end;
    public float duration = 1.0f;
    float t;
    // Use this for initialization
    void Start ()
    {
        t = 0.0f;
    }
 
    // Update is called once per frame
    void Update ()
    {
        t += Time.deltaTime;
        float s = t / duration;
        transform.position = Vector3.Lerp(start.position, end.position, curve.Evaluate(s));
    }
}
*/
