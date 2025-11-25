// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (you'll be credited in the updates) 
// black.creepy.cat@gmail.com 

using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections;

namespace creepycat.scifikitvol4 
{
    // A simple crappy addon for the camera mouse look, it can be better for sure! Given for example...
    // Bug: On terrain i don't know why the landing sound repeat in loop sometime, maybe a 
    // problem with the landing raycast? If anyone can help...
    class SuperMouseLookFpsCamera : MonoBehaviour
    {

        public Transform LookTransform;

        [Header("")]
        [Tooltip("Camera acceleration time and speed")]
        public float Acceleration = 2.0f;
        public float AccelTime = 2.5f;

        [Header("")]
        [Tooltip("RigidBody Setup")]
        public float RotationRate = 0.15f;
        public float Velocity = 2.0f;
        public float GroundControl = 1.0f;
        public float AirControl = 0.2f;

        [Header("")]
        [Tooltip("Keyboard Setup")]
        public KeyCode fwdKey = KeyCode.W;
	    public KeyCode leftKey = KeyCode.A;
	    public KeyCode backKey = KeyCode.S;
        public KeyCode rightKey = KeyCode.D;
        public KeyCode JumpKey = KeyCode.Space;
        public KeyCode sprintKey = KeyCode.LeftShift;

        [Header("")]
        [Tooltip("Jump Setup")]
        public float JumpVelocity = 5;
        public float GroundHeight = 1.1f;

        [Header("")]
        [Tooltip("Drop the main camera here")]
        public Transform CameraTransform;
        public float walkingBobbingSpeed = 14f;
        public float bobbingAmount = 0.05f;

        [Header("")]
        [Tooltip("Select the FPS camera sounds")]
        public AudioClip JumpSound;
        public AudioClip LandSound;
        public AudioClip StepSound;


        // For cat only...
        private Rigidbody bodyObj;
        private AudioSource audioSource;

        private float forwardZ = 0.0f;
        private float strafeX = 0.0f;
        private float accelValue = 0.0f;

        float defaultCameraPosY = 0;
        float bobbingTimer = 0;

        private bool GoJump;
        private bool IsMoving;
        private bool IsJumping;
        private bool IsRunning;
        private bool IsLanding;
        private bool Grounded;

        private float FootstepDelayTime = 0.6f;
 
        void Start(){
            bodyObj = GetComponent<Rigidbody>();
            bodyObj.freezeRotation = true;
            bodyObj.useGravity = false;

            defaultCameraPosY = CameraTransform.localPosition.y;
            audioSource = GetComponent<AudioSource>();

            // Sound management by coroutine
            StartCoroutine(LaunchFootstepSound());
            StartCoroutine(LaunchLandSound());
        }

        void Update(){
            GoJump = GoJump || Input.GetKey(JumpKey);

            if(Input.GetKeyDown(JumpKey) && IsJumping == false){ 
                PlayJumpSound();
            }    

            BobbingUpdate();
        }

        // Play the jump sound
        private void PlayJumpSound(){
            audioSource.pitch = Random.Range(1.0f, 1.1f);
            audioSource.PlayOneShot(JumpSound);
        }

        // Play the land sound
        private void PlayLandSound(){
            audioSource.pitch = Random.Range(1.0f, 1.3f);
            audioSource.PlayOneShot(LandSound);
        }

         // Play the step sound
        private void PlayStepSound(){
             audioSource.pitch = Random.Range(0.7f, 1.0f); // I change the pitch for each step
             audioSource.PlayOneShot(StepSound);
        }

        // Updating head bobbing
        void BobbingUpdate(){

            // tmp value for lerping
            float tmpBobbing = bobbingAmount;
            float tmpBobbingSpeed = walkingBobbingSpeed;

            if (IsMoving == true && IsJumping == false){

                if (IsRunning == true){
                    tmpBobbing = (bobbingAmount * 1.4f);
                    tmpBobbingSpeed = (walkingBobbingSpeed * 1.4f);
                }else{
                    tmpBobbing = Mathf.Lerp(tmpBobbing, bobbingAmount, 2.5f * Time.deltaTime);
                    tmpBobbingSpeed = Mathf.Lerp(tmpBobbingSpeed, walkingBobbingSpeed, 2.5f * Time.deltaTime);
                }

                bobbingTimer += Time.deltaTime * tmpBobbingSpeed;
                CameraTransform.localPosition = new Vector3(CameraTransform.localPosition.x, defaultCameraPosY + Mathf.Sin(bobbingTimer) * tmpBobbing, CameraTransform.localPosition.z);
            }else{
               bobbingTimer = 0;
               CameraTransform.localPosition = new Vector3(CameraTransform.localPosition.x, Mathf.Lerp(CameraTransform.localPosition.y, defaultCameraPosY, Time.deltaTime * tmpBobbingSpeed), CameraTransform.localPosition.z);
            }

        }

        void FixedUpdate(){

            // Cast a ray towards the ground to see if is Grounded
            Grounded = Physics.Raycast(transform.position, Physics.gravity.normalized, GroundHeight);

            if (Grounded == true){
                IsJumping=false;
            }else{
                IsJumping = true;
                IsLanding = true;
            }

            // Rotate the body 
            Vector3 gravityForward = Vector3.Cross(Physics.gravity, transform.right);
            Quaternion targetRotation = Quaternion.LookRotation(gravityForward, -Physics.gravity);
            bodyObj.rotation = Quaternion.Lerp(bodyObj.rotation, targetRotation, RotationRate);

            // Add velocity change for movement 
            Vector3 forward = Vector3.Cross(transform.up, -LookTransform.right).normalized;
            Vector3 right = Vector3.Cross(transform.up, LookTransform.forward).normalized;

            IsMoving = false;
            IsRunning = false;
            
            // KEyboard stuff
            if (Input.GetKey(fwdKey)){
                forwardZ = Mathf.Lerp(forwardZ, accelValue, Time.deltaTime * AccelTime);
                IsMoving = true;

            }else if(Input.GetKey(backKey)){
                forwardZ = Mathf.Lerp(forwardZ, -accelValue, Time.deltaTime * AccelTime);
                IsMoving = true;
            }else{
                forwardZ = Mathf.Lerp(forwardZ, 0.0f, Time.deltaTime * AccelTime);
            }

            if (Input.GetKey(rightKey)){
                strafeX = Mathf.Lerp(strafeX, accelValue, Time.deltaTime * AccelTime);
                IsMoving = true;

            }else if(Input.GetKey(leftKey)){
                strafeX = Mathf.Lerp(strafeX, -accelValue, Time.deltaTime * AccelTime);
                IsMoving = true;
            }else{
                strafeX = Mathf.Lerp(strafeX, 0.0f, Time.deltaTime * AccelTime);
            }

            if (Input.GetKey(sprintKey)){
                accelValue = Mathf.Lerp(accelValue, Acceleration*2, Time.deltaTime * 50.3f);
                
                IsRunning = true;
            }else{
                accelValue = Mathf.Lerp(accelValue, Acceleration, Time.deltaTime * 50.3f);
            }

            // Computing...
            Vector3 targetVelocity = (forward * forwardZ + right * strafeX) * Velocity;
            Vector3 localVelocity = transform.InverseTransformDirection(bodyObj.velocity);
            Vector3 velocityChange = transform.InverseTransformDirection(targetVelocity) - localVelocity;

            // The vertical component is either removed or set to result in the absolute GoJump velocity
            velocityChange = Vector3.ClampMagnitude(velocityChange, Grounded ? GroundControl : AirControl);
            velocityChange.y = GoJump && Grounded ? -localVelocity.y + JumpVelocity : 0;
            velocityChange = transform.TransformDirection(velocityChange);
            bodyObj.AddForce(velocityChange, ForceMode.VelocityChange);

            // Add gravity
            bodyObj.AddForce(Physics.gravity * bodyObj.mass);

            GoJump = false;
        }



        // Coroutine to play the step sound
        IEnumerator LaunchFootstepSound(){
            while(true){
                float tmpSpeed=FootstepDelayTime;

                if (IsMoving == true && IsJumping == false){
                    if(IsRunning == true) tmpSpeed=tmpSpeed/1.65f;

                    PlayStepSound();
                }

                yield return new WaitForSeconds(tmpSpeed);
            }
        }


        // Coroutine to play the land sound one time
        IEnumerator LaunchLandSound(){
            while(true){
                if (IsLanding == true && Grounded==true){
                    PlayLandSound();
                    IsLanding = false;
                }

               yield return null;
            }
        }




    }

}