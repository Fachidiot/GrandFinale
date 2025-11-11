using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InfinityPBR.Demo
{
    public class TreantDemo : InfinityDemoCharacter
    {
        public bool isLowPoly = true;
        private ColorShiftRuntime _colorShiftRuntime;
        
        [Header("Treant Materials")]
        public Material[] materialsMain;
        public Material[] materialsLimbs;

        [Header("Particles")] 
        public GameObject[] magicSpells;
        
        [Header("Treant Randoms")] 
        public bool randomWalkStyle = false;
        public bool randomIdleStyle = false;
        public bool randomAngryIdleBreakStyle = false;
        /*public float randomSeedChangeSpeedMin = 1f; // How fast the random seed changes
        public float randomSeedChangeSpeedMax = 3f; // How fast the random seed changes
        public float randomSeedChangePeriodMin = 1.5f; // Period between changes
        public float randomSeedChangePeriodMax = 4f; // Period between changes
        */

        // Privates
        /*private float _randomValue;
        private float _randomFrom;
        private float _randomSeed;
        private float _randomLerp;
        private bool _randomWait;
        private float _randomSeedChangeSpeed = 1f;
        private float _randomSeedChangePeriod = 1f;*/
        private static readonly int WalkStyle = Animator.StringToHash("WalkStyle");
        private static readonly int WalkSpeed = Animator.StringToHash("WalkSpeed");
        private static readonly int IdleStyle = Animator.StringToHash("IdleStyle");
        private static readonly int AngryIdleBreakStyle = Animator.StringToHash("AngryIdleBreakStyle");
        private float _spellTimer = 0f;
        private GameObject _spellOnDeck;

        void Start()
        {
            base.Start();
            if (isLowPoly)
            {
                _colorShiftRuntime = GetComponent<ColorShiftRuntime>();
            }
            //StartCoroutine(nameof(Randomize));
        }

        private void CastSpell(int i)
        {
            if (i < 0 || i >= magicSpells.Length) return;

            if (magicSpells[i].TryGetComponent(out ParticleSystem parentPS))
            {
                parentPS.Play();
            }
            
            foreach (Transform child in magicSpells[i].transform)
            {
                if (!child.gameObject.TryGetComponent(out ParticleSystem ps)) return;
                ps.Play();
            }
        }

        /*
        void LateUpdate()
        {
            if (!automateStyles) return;
            if (randomWalkStyle) animator.SetFloat(WalkStyle, _randomValue);
            if (randomIdleStyle) animator.SetFloat(IdleStyle, _randomValue);
            if (randomAngryIdleBreakStyle) animator.SetFloat(AngryIdleBreakStyle, _randomValue);
        }

*/
        /*
        IEnumerator Randomize()
        {
            while (true)
            {
                while (!automateStyles)
                    yield return null;

                while (_randomLerp < 1f)
                {
                    if (_randomLerp <= 0) _randomSeed = Random.Range(_randomSeed < 0.5 ? 0.5f : 0f, _randomSeed >= 0.5 ? 0.5f : 1f); // Set new random value
                    
                    _randomLerp += Time.deltaTime / Random.Range(randomSeedChangeSpeedMin, randomSeedChangeSpeedMax);
                    _randomValue = Mathf.Lerp(_randomFrom, _randomSeed, _randomLerp);
                    SetStyleValue(_randomValue);
                    yield return null;
                }
                
                _randomLerp = 0f; // Reset
                _randomFrom = _randomSeed; // Cache the value
                yield return new WaitForSeconds(Random.Range(randomSeedChangePeriodMin, randomSeedChangePeriodMax)); // Wait for the next period
            }
        }
        */

        private void SetStyleValue(float randomValue)
        {
            foreach (var key in styleKeys)
                animator.SetFloat(key, randomValue);
        }

        public void RandomTexture() => UpdateTextures(Random.Range(0, materialsLimbs.Length));
        
        public void UpdateTextures(int index)
        {
            if (isLowPoly)
            {
                RandomColorShifter();
                return;
            }
            var materials = skinnedMeshRenderer.sharedMaterials;
            materials[0] = materialsLimbs[index];
            materials[1] = materialsMain[index];
            skinnedMeshRenderer.sharedMaterials = materials;
        }

        public void RandomColorShifter()
        {
            _colorShiftRuntime.SetRandomColorSet();
        }
        
        public void SetWalkStyle(float value) => animator.SetFloat(WalkStyle, value);
        public void SetWalkSpeed(float value) => animator.SetFloat(WalkSpeed, value);
        public void SetIdleStyle(float value) => animator.SetFloat(IdleStyle, value);
        public void SetAngryIdleBreakStyle(float value) => animator.SetFloat(AngryIdleBreakStyle, value);
    }
}
