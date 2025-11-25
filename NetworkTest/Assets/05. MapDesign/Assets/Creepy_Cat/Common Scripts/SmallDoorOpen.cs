// Code by Creepy Cat (C) 2021/2022
// Modified for network synchronization

using UnityEngine;
using Uween;
using Newtonsoft.Json.Linq;

namespace creepycat.scifikitvol4
{
    public class SmallDoorOpen : MonoBehaviour, IWorldInteractable
    {
        [Header("Door Objects")]
        public GameObject DoorLeftA;

        [Header("Visual/Audio Feedback")]
        [SerializeField] private GameObject[] emissiveList;
        public GameObject DoorButtonA;
        public GameObject DoorButtonB;
        public Light SpotLightA;
        public Light SpotLightB;
        public AudioClip DoorSound;

        [Header("Animation Settings")]
        public float moveTimeA = 2.0f;
        public float emissionIntensity = 3.0f;
        public float moveMax = 0.6f;

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
            return new JObject { ["isOpen"] = nextState };
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
                TweenX.Add(DoorLeftA, moveTimeA, moveMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
            }
            else // Closing
            {
                TweenX.Add(DoorLeftA, moveTimeA, -moveMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
            }

            UpdateVisuals(open);

            if (audioSource != null && DoorSound != null)
                audioSource.PlayOneShot(DoorSound, 1.0F);
        }

        private void UpdateVisuals(bool open)
        {
            if (buttonRendererA != null)
                buttonRendererA.material.SetColor("_EmissionColor", open ? Color.white / 2 : Color.white * 1.5f);
            if (buttonRendererB != null)
                buttonRendererB.material.SetColor("_EmissionColor", open ? Color.white / 2 : Color.white * 1.5f);

            if (SpotLightA != null) SpotLightA.enabled = open;
            if (SpotLightB != null) SpotLightB.enabled = open;
            if (SpotLightA != null) SpotLightA.intensity = open ? emissionIntensity : 0f;
            if (SpotLightB != null) SpotLightB.intensity = open ? emissionIntensity : 0f;

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
    }
}