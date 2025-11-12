using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace InfinityPBR.Demo
{
    public class BirdyDemo : InfinityDemoCharacter
    {
        [Header("Birdy Materials")]
        public Material[] bodyMaterials;
        public Material[] furMaterials;
        public Material[] eyeMaterials;
        
        [Header("Renderers")]
        public SkinnedMeshRenderer[] bodyRenderers;
        [FormerlySerializedAs("furRenderer")] public SkinnedMeshRenderer[] furRenderers;
        [FormerlySerializedAs("eyeRenderer")] public SkinnedMeshRenderer[] eyeRenderers;

        [Header("Birdy Randoms")]
        public bool randomIdleStyle = false;

        public Button reallySuperRandomButton;
        public KeyCode reallyRandomKey = KeyCode.T;

        private static readonly int WalkSpeed = Animator.StringToHash("WalkSpeed");
        private static readonly int IdleStyle = Animator.StringToHash("IdleStyle");

        
        //private static readonly int AngryIdleBreakStyle = Animator.StringToHash("AngryIdleBreakStyle");
        //private float _spellTimer = 0f;
        //private GameObject _spellOnDeck;
        public float airHeight = 104.0255f;
        private static readonly int IsAir = Animator.StringToHash("IsAir");
        private bool IsAirValue => animator.GetBool(IsAir);
        private Vector3 StartPositionAir => new Vector3(_startPosition.x, airHeight, _startPosition.z);

        void Start()
        {
            base.Start();
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(reallyRandomKey)) reallySuperRandomButton.onClick.Invoke();
        }

        protected override void ResetPosition()
        {
            _transform.position = IsAirValue ? StartPositionAir : _startPosition;
        }
        
        private void SetStyleValue(float randomValue)
        {
            foreach (var key in styleKeys)
                animator.SetFloat(key, randomValue);
        }

        public void RandomTexture()
        {
            UpdateMaterials(bodyRenderers, bodyMaterials, Random.Range(0, bodyMaterials.Length));
            UpdateMaterials(furRenderers, furMaterials, Random.Range(0, furMaterials.Length));
            SetEyeMaterial(Random.Range(0, eyeMaterials.Length));
        }

        public void ReallyRandom()
        {
            UpdateMaterials(bodyRenderers, bodyMaterials, Random.Range(0, bodyMaterials.Length));
            SetEyeMaterial(Random.Range(0, eyeMaterials.Length));
            SetFurMaterial0(Random.Range(0, furMaterials.Length));
            SetFurMaterial1(Random.Range(0, furMaterials.Length));
            SetFurMaterial2(Random.Range(0, furMaterials.Length));
            SetFurMaterial3(Random.Range(0, furMaterials.Length));
            SetFurMaterial4(Random.Range(0, furMaterials.Length));
            SetFurMaterial5(Random.Range(0, furMaterials.Length));
            SetFurMaterial6(Random.Range(0, furMaterials.Length));
            SetFurMaterial7(Random.Range(0, furMaterials.Length));
            SetFurMaterial8(Random.Range(0, furMaterials.Length));
            Debug.Log($"{Random.Range(0, furMaterials.Length)} and {Random.Range(0, furMaterials.Length)} and {Random.Range(0, furMaterials.Length)}");
        }
        
        public void UpdateMaterials(SkinnedMeshRenderer[] renderers, Material[] materials, int index)
        {
            foreach(var meshRenderer in renderers)
                meshRenderer.material = materials[index];
        }
        
        public void SetBodyMaterial(int index) => UpdateMaterials(bodyRenderers, bodyMaterials, index);
        public void SetFurMaterial(int index) => UpdateMaterials(furRenderers, furMaterials, index);

        public void SetEyeMaterial(int index)
        {
            foreach(var meshRenderer in eyeRenderers)
            {
                var materials = meshRenderer.materials;
                materials[0] = eyeMaterials[index];
                materials[1] = eyeMaterials[index];
                meshRenderer.materials = materials;
            }
        }
        
        public void SetFurMaterial(int index, int rendererIndex) => furRenderers[rendererIndex].material = furMaterials[index];
        
        public void SetFurMaterial0(int index) => furRenderers[0].material = furMaterials[index];
        public void SetFurMaterial1(int index) => furRenderers[1].material = furMaterials[index];
        public void SetFurMaterial2(int index) => furRenderers[2].material = furMaterials[index];
        public void SetFurMaterial3(int index) => furRenderers[3].material = furMaterials[index];
        public void SetFurMaterial4(int index) => furRenderers[4].material = furMaterials[index];
        public void SetFurMaterial5(int index) => furRenderers[5].material = furMaterials[index];
        public void SetFurMaterial6(int index) => furRenderers[6].material = furMaterials[index];
        public void SetFurMaterial7(int index) => furRenderers[7].material = furMaterials[index];
        public void SetFurMaterial8(int index) => furRenderers[8].material = furMaterials[index];
        public void SetFurMaterial9(int index) => furRenderers[9].material = furMaterials[index];
        
        
        public void SetWalkSpeed(float value) => animator.SetFloat(WalkSpeed, value);
        public void SetIdleStyle(float value) => animator.SetFloat(IdleStyle, value);
        
        public void SetIsAir() => animator.SetBool(IsAir, true);
        public void SetIsGround() => animator.SetBool(IsAir, false);
    }
}
