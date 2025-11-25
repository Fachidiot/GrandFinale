// Code by Creepy Cat (C) 2021/2022
// Modified for network synchronization

using UnityEngine;
using Uween;
using Newtonsoft.Json.Linq;

namespace creepycat.scifikitvol4
{
    public class ContainerOpen : MonoBehaviour, IWorldInteractable
    {
        [Header("Door Objects")]
        public GameObject DoorLeft;
        public GameObject DoorRight;

        [Header("Visual/Audio Feedback")]
        public GameObject DoorButton;
        public AudioClip DoorSound;

        [Header("Animation Settings")]
        public float moveTimeA = 2.0f;
        private float rotateMax = 90f;

        // Internal State
        private bool isOpen = false;
        private bool animationInProgress = false;

        // Networking
        private string uniqueId;

        // Components
        private Renderer buttonRenderer;
        private AudioSource audioSource;

        void Start()
        {
            // Cache components
            buttonRenderer = DoorButton?.GetComponent<Renderer>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

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
        // Called by the Interactable component on the door button
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
                TweenRY.Add(DoorLeft, moveTimeA, rotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
                TweenRY.Add(DoorRight, moveTimeA, -rotateMax).Relative().EaseInOutCubic();
            }
            else // Closing
            {
                TweenRY.Add(DoorLeft, moveTimeA, -rotateMax).Relative().EaseInOutCubic().Then(EndAnimationFlag);
                TweenRY.Add(DoorRight, moveTimeA, rotateMax).Relative().EaseInOutCubic();
            }

            UpdateVisuals(open);

            if (audioSource != null && DoorSound != null)
                audioSource.PlayOneShot(DoorSound, 1.0F);
        }

        private void UpdateVisuals(bool open)
        {
            if (buttonRenderer != null)
            {
                buttonRenderer.material.SetColor("_EmissionColor", open ? Color.white / 3 : Color.white * 1.5f);
            }
        }

        private void EndAnimationFlag()
        {
            animationInProgress = false;
        }
    }
}
