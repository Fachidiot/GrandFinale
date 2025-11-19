using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace InfinityPBR
{
    public class GellyCubeColor : MonoBehaviour
    {
        public Color[] colors;

        public SkinnedMeshRenderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        
        private float _counter = 0f;
        public float _transitionTime = 0.5f;

        private Color _desiredColor;
        private Color _startColor;

        public bool constantColorChange = true;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();

            if (constantColorChange)
                RandomNewColor();

            if (_renderer != null) return;

            Debug.LogWarning("No SkinnedMeshRenderer assigned");
            Destroy(this);
            
        }

        public void ToggleConstantChange(bool value) => constantColorChange = value;

        public void RandomNewColor()
        {
            StopCoroutine(ColorTransition());
            _renderer.GetPropertyBlock(_propertyBlock);
            _startColor = _propertyBlock.GetColor("_Color");
            _desiredColor = new Color(Random.Range(0.2f, 1f),Random.Range(0.2f, 1f),Random.Range(0.2f, 1f),Random.Range(0.5f, 1f));
            _counter = 0f;
            StartCoroutine(ColorTransition());
        }

        public void Update()
        {
            if (!constantColorChange) return;
            
            if (_counter >= _transitionTime)
            {
                RandomNewColor();
            }
        }

        public void SetRandomColor()
        {
            var i = Random.Range(0, colors.Length);
            if (i >= 0 && colors.Length > i) SetColor(colors[i]);
        }
        
        public void SetColor(Color value)
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_Color", value);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        private IEnumerator ColorTransition()
        {
            while (_counter < _transitionTime)
            {
                _counter += Time.deltaTime;
                
                SetColor(Color.Lerp(_startColor, _desiredColor, _counter / _transitionTime));
                yield return null;
            }
        }
    }
}
