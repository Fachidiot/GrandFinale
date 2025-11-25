// Code by Creepy Cat (C) 2021/2022
// Modified for network synchronization

using UnityEngine;
using Uween;
using Newtonsoft.Json.Linq;

namespace creepycat.scifikitvol4
{
    public class WardrobeOpen : MonoBehaviour, IWorldInteractable
    {
        [Header("Door Objects")]
        public GameObject DoorLeftA;
        public GameObject DoorRightA;
        public GameObject DoorLeftB;
        public GameObject DoorRightB;

        [Header("Visual/Audio Feedback")]
        public GameObject DoorLeftButton;
        public GameObject DoorRightButton;
        public Light CaseLightLeft;
        public Light CaseLightRight;
        public AudioClip DoorSound;

        [Header("Animation Settings")]
        public float moveTimeA = 2.0f;
        public float emissionIntensity = 3.0f;
        private float rotateMax = 90f;

        // Internal State
        private bool isLeftOpen = false;
        private bool isRightOpen = false;
        private bool animationInProgress = false;

        // Networking
        private string uniqueId;

        // Components
        private Renderer buttonRendererLeft;
        private Renderer buttonRendererRight;
        private AudioSource audioSource;

        void Start()
        {
            // Cache components
            buttonRendererLeft = DoorLeftButton?.GetComponent<Renderer>();
            buttonRendererRight = DoorRightButton?.GetComponent<Renderer>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();


            // Set initial visual state based on internal state
            UpdateVisuals(isLeftOpen, true);
            UpdateVisuals(isRightOpen, false);
        }

        #region IWorldInteractable Implementation

        public void Initialize(string id)
        {
            this.uniqueId = id;
        }

        public JToken GetState(string subId)
        {
            if (subId == "left")
            {
                return new JObject { ["isOpen"] = !isLeftOpen };
            }
            if (subId == "right")
            {
                return new JObject { ["isOpen"] = !isRightOpen };
            }
            return null;
        }

        public void SetState(string subId, JToken state)
        {
            if (state == null || state["isOpen"] == null) return;
            bool shouldBeOpen = state["isOpen"].Value<bool>();

            if (subId == "left")
            {
                if (isLeftOpen == shouldBeOpen) return;
                isLeftOpen = shouldBeOpen;
                PlayAnimation(true, isLeftOpen);
            }
            else if (subId == "right")
            {
                if (isRightOpen == shouldBeOpen) return;
                isRightOpen = shouldBeOpen;
                PlayAnimation(false, isRightOpen);
            }
        }

        #endregion

        #region Public Interaction Methods

        // Called by the Interactable component on the left door button
        public void InteractLeft()
        {
            if (animationInProgress) return;
            WorldInteractableManager.Instance.RequestStateChange(uniqueId + "_left");
        }

        // Called by the Interactable component on the right door button
        public void InteractRight()
        {
            if (animationInProgress) return;
            WorldInteractableManager.Instance.RequestStateChange(uniqueId + "_right");
        }

        #endregion

        private void PlayAnimation(bool isLeft, bool open)
        {
            if (animationInProgress) return;
            animationInProgress = true;

            GameObject doorA = isLeft ? DoorLeftA : DoorLeftB;
            GameObject doorB = isLeft ? DoorRightA : DoorRightB;
            
            if (open)
            {
                TweenRY.Add(doorA, moveTimeA, rotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
                TweenRY.Add(doorB, moveTimeA, -rotateMax).Relative().EaseInOutCubic();
            }
            else // Closing
            {
                TweenRY.Add(doorA, moveTimeA, -rotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
                TweenRY.Add(doorB, moveTimeA, rotateMax).Relative().EaseInOutCubic();
            }
            
            UpdateVisuals(open, isLeft);

            if (audioSource != null && DoorSound != null)
                audioSource.PlayOneShot(DoorSound, 1.0F);
        }

        private void UpdateVisuals(bool isOpen, bool isLeft)
        {
            Renderer buttonRenderer = isLeft ? buttonRendererLeft : buttonRendererRight;
            Light caseLight = isLeft ? CaseLightLeft : CaseLightRight;

            if (buttonRenderer != null)
            {
                buttonRenderer.material.SetColor("_EmissionColor", isOpen ? Color.white / 3 : Color.white * 1.5f);
            }
            if (caseLight != null)
            {
                // Light fading can be a simple lerp in Update if desired, or just set directly.
                caseLight.enabled = isOpen;
                caseLight.intensity = isOpen ? emissionIntensity : 0f;
            }
        }

        private void EndAnimationFlag()
        {
            animationInProgress = false;
        }
    }
}
