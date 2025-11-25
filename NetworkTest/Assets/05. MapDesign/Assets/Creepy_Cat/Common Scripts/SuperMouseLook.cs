// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (you'll be credited in the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;

namespace creepycat.scifikitvol4
{
    public class SuperMouseLook : MonoBehaviour
    {

        public KeyCode fwdKey = KeyCode.W;
	    public KeyCode leftKey = KeyCode.A;
	    public KeyCode backKey = KeyCode.S;
	    public KeyCode rightKey = KeyCode.D;

        [Header("")]
        public KeyCode upKey = KeyCode.A;
	    public KeyCode downKey = KeyCode.E;

        [Header("")]
        public bool RightMouseToForward=false;
        public bool LockAndJustRotate=false;
        public bool ClampPitchRotation=false;
        public bool ClampYawRotation=false;

        class CameraState
        {
            public float yaw;
            public float pitch;
            public float roll;
            public float x;
            public float y;
            public float z;

            public void SetFromTransform(Transform t)

            {
                pitch = t.eulerAngles.x;
                yaw = t.eulerAngles.y;
                roll = t.eulerAngles.z;

                x = t.position.x;
                y = t.position.y;
                z = t.position.z;
            }

            public void Translate(Vector3 translation)
            {
                Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;

                x += rotatedTranslation.x;
                y += rotatedTranslation.y;
                z += rotatedTranslation.z;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
            {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);
                
                x = Mathf.Lerp(x, target.x, positionLerpPct);
                y = Mathf.Lerp(y, target.y, positionLerpPct);
                z = Mathf.Lerp(z, target.z, positionLerpPct);
            }

            public void UpdateTransform(Transform t,bool locked)
            {
                t.eulerAngles = new Vector3(pitch, yaw, roll);

                if (locked == false){
                    t.position = new Vector3(x, y, z);
                }
            }
        }
        
        CameraState m_TargetCameraState = new CameraState();
        CameraState m_InterpolatingCameraState = new CameraState();

        [Header("Movement Settings")]
        [Tooltip("Exponential boost factor on translation")]
        public float boost = 1.5f;

        [Tooltip("Time it takes to interpolate camera"), Range(0.001f, 50f)]
        public float positionLerpTime = 4.2f;

        [Header("Rotation Settings")]
        [Tooltip("Change in mouse rotation.")]
        public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        [Tooltip("Time it takes to interpolate camera rotation."), Range(0.001f, 10f)]
        public float rotationLerpTime = 4.2f;

        [Tooltip("Invert our Y axis for mouse input to rotation.")]
        public bool invertY = false;

        void OnEnable(){
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
        }

        Vector3 GetInputTranslationDirection(){
            Vector3 direction = new Vector3();

            if (LockAndJustRotate == false){

                if (Input.GetMouseButton(1) & RightMouseToForward == true)
                {
                    direction += Vector3.forward;
                }

                if (Input.GetKey(fwdKey))
                {
                    direction += Vector3.forward;
                }
                if (Input.GetKey(backKey))
                {
                    direction += Vector3.back;
                }
                if (Input.GetKey(leftKey))
                {
                    direction += Vector3.left;
                }
                if (Input.GetKey(rightKey))
                {
                    direction += Vector3.right;
                }
                if (Input.GetKey(downKey))
                {
                    direction += Vector3.down;
                }
                if (Input.GetKey(upKey))
                {
                    direction += Vector3.up;
                }

            }

            return direction;
        }
        


        void Update()
        {
            // Exit Sample  
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();

				#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false; 
				#endif
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1)); 
            var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

            m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
            m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;

            // If camera pitch clamp
            if(ClampPitchRotation){ 
                m_TargetCameraState.pitch = Mathf.Clamp(m_TargetCameraState.pitch, -90f, 90f);
            }

            // If camera yaw clamp
            if(ClampYawRotation){ 
                m_TargetCameraState.yaw = Mathf.Clamp(m_TargetCameraState.yaw, -120f, 120f);
            }

            

            // Translation
            var translation = GetInputTranslationDirection() * Time.deltaTime;

            // Use shift to speed up
            if (Input.GetKey(KeyCode.LeftShift)){
                translation *= 10.0f;
            }

            // Use ctrl to speed down
            if (Input.GetKey(KeyCode.LeftControl)){
                translation /= 2.0f;
            }

            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            boost += Input.mouseScrollDelta.y * 0.2f;
            translation *= Mathf.Pow(2.0f, boost);

            m_TargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);

            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);
            m_InterpolatingCameraState.UpdateTransform(transform, LockAndJustRotate);
        }
    }

}
