// Code by Creepy Cat (C) 2021/2022
// Modified for network synchronization

using UnityEngine;
using Uween;
using Newtonsoft.Json.Linq;

namespace creepycat.scifikitvol4
{
    public class CrateOpen : MonoBehaviour, IWorldInteractable
    {
        [Header("Crate Objects")]
        public GameObject CrateTop;
        public GameObject CrateButton;
 
        [Header("Visual/Audio Feedback")]
        [SerializeField] private Light[] lightList;
        public AudioClip CrateSound;

        [Header("Animation Settings")]
        public float OpenTime = 2.0f;
        public float EmissionIntensity = 1.2f;
        private float RotateMax = -90f;
        
        // Internal State
        private bool isOpen = false;
        private bool animationInProgress = false;
        private bool playerNearby = false;

        // Networking
        private string uniqueId;

        // Components
        private Renderer ButtonRenderer;
        private AudioSource AudioSource;
        
        void Start()
        {
            ButtonRenderer = CrateButton.GetComponent<Renderer>();
            AudioSource = GetComponent<AudioSource>();
            if (AudioSource == null) AudioSource = gameObject.AddComponent<AudioSource>();

            foreach (var light in lightList)
            {
                light.enabled = false;
                light.intensity = 0.0f;
            }
            
            UpdateVisuals(isOpen);
        }

        #region IWorldInteractable Implementation
        
        public void Initialize(string id)
        {
            this.uniqueId = id;
        }

        public JToken GetState(string subId)
        {
            // This is called on the host. We toggle the state and return the new state.
            isOpen = !isOpen;
            return new JObject { ["isOpen"] = isOpen };
        }

        public void SetState(string subId, JToken state)
        {
            if (state == null || state["isOpen"] == null) return;
            
            bool shouldBeOpen = state["isOpen"].Value<bool>();
            if (isOpen == shouldBeOpen) return;

            isOpen = shouldBeOpen;
            PlayAnimation(isOpen);
        }

        #endregion

        #region Public Interaction Methods
        // Called by the Interactable component
        public void Interact()
        {
            if (animationInProgress) return;
            WorldInteractableManager.Instance.RequestStateChange(uniqueId);
        }
        #endregion

        private void PlayAnimation(bool open)
        {
            if (animationInProgress) return;
            animationInProgress = true;

            if (open)
            {
                TweenRX.Add(CrateTop, OpenTime, RotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
            }
            else // Closing
            {
                TweenRX.Add(CrateTop, OpenTime, -RotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
            }

            UpdateVisuals(open);
            
            if(AudioSource != null && CrateSound != null)
                AudioSource.PlayOneShot(CrateSound, 1.0F);
        }

        private void UpdateVisuals(bool open)
        {
            if (ButtonRenderer != null)
                ButtonRenderer.material.SetColor("_EmissionColor", open ? Color.red * EmissionIntensity : Color.white * EmissionIntensity);

            // The original script turned lights off when opening, and on when closing (if player is nearby)
            playerNearby = !open;
        }

        private void EndAnimationFlag()
        {
            animationInProgress = false;
        }

        // The light fading can remain a local, client-side effect based on proximity.
        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("MainCamera"))
            {
                if (!isOpen) // Only trigger lights if the crate is closed
                {
                    playerNearby = true;
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("MainCamera"))
            {
                playerNearby = false;
            }
        }
        
        void Update()
        {
            // This locally-driven light effect is fine to keep.
            float targetIntensity = playerNearby && !isOpen ? 1.5f : 0.0f;

            foreach (var light in lightList)
            {
                light.intensity = Mathf.Lerp(light.intensity, targetIntensity, Time.deltaTime * 1.2f);
                light.enabled = light.intensity > 0.01f;
            }
        }
    }
}