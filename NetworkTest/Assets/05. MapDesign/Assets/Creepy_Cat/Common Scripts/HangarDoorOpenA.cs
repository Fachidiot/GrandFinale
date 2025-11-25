// Code by Creepy Cat (C) 2021/2022
// Modified for network synchronization

using UnityEngine;
using Uween;
using Newtonsoft.Json.Linq;

namespace creepycat.scifikitvol4
{
    public class HangarDoorOpenA : MonoBehaviour, IWorldInteractable
    {
        [Header("Door Objects")]
        public GameObject DoorLeft;
        public GameObject DoorRight;

        [Header("Visual/Audio Feedback")]
        [SerializeField] private GameObject[] emissiveList;
        public GameObject DoorButtonA;
        public GameObject DoorButtonB;
        public Light SpotLightA;
        public AudioClip DoorSound;

        [Header("Animation Settings")]
        public float moveTimeA = 2.0f;
        public float moveTimeB = 2.0f;
        public float emissionIntensity = 3.0f;
        public float moveMax = 0.6f;
        public float fadeTime = 1.2f;

        // Internal State
        private bool isOpen = false;
        private bool animationInProgress = false;

        // Networking
        private string uniqueId;

        // Components
        private Renderer buttonRendererA;
        private Renderer buttonRendererB;
        private AudioSource audioSource;
        private Renderer[] emissiveRenderers;

        void Start()
        {
            // Cache components
            buttonRendererA = DoorButtonA?.GetComponent<Renderer>();
            buttonRendererB = DoorButtonB?.GetComponent<Renderer>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            emissiveRenderers = new Renderer[emissiveList.Length];
            for (int i = 0; i < emissiveList.Length; i++)
            {
                emissiveRenderers[i] = emissiveList[i]?.GetComponent<Renderer>();
            }

            // Set initial visual state
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
            bool nextState = !isOpen;
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
        // Called by the Interactable component on the door buttons
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
                TweenY.Add(DoorLeft, moveTimeA, -moveMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
                TweenY.Add(DoorRight, moveTimeB, -moveMax).Relative().EaseInOutCubic();
            }
            else // Closing
            {
                TweenY.Add(DoorLeft, moveTimeA, moveMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
                TweenY.Add(DoorRight, moveTimeB, moveMax).Relative().EaseInOutCubic();
            }

            UpdateVisuals(open);

            if (audioSource != null && DoorSound != null)
                audioSource.PlayOneShot(DoorSound, 1.0F);
        }

        private void UpdateVisuals(bool open)
        {
            // Button Emission
            if (buttonRendererA != null)
                buttonRendererA.material.SetColor("_EmissionColor", open ? Color.white / 3 : Color.white * 1.5f);
            if (buttonRendererB != null)
                buttonRendererB.material.SetColor("_EmissionColor", open ? Color.white / 3 : Color.white * 1.5f);

            // Emissive List
            if (emissiveRenderers == null) return;
            foreach (var emissiveRenderer in emissiveRenderers)
            {
                if (emissiveRenderer != null)
                    emissiveRenderer.material.SetColor("_EmissionColor", open ? Color.white * 1.5f : Color.white / 3.0f);
            }
        }

        private void EndAnimationFlag()
        {
            animationInProgress = false;
        }

        void Update()
        {
            // Light fading can remain a local effect
            if (SpotLightA == null) return;

            float targetIntensity = isOpen ? emissionIntensity : 0.0f;
            SpotLightA.intensity = Mathf.Lerp(SpotLightA.intensity, targetIntensity, Time.deltaTime * fadeTime);
        }
    }
}