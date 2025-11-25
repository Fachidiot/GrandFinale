// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace creepycat.scifikitvol4
{
     // A simple classs to make flickering light
    public class FlickeringLight : MonoBehaviour {
        public new Light light;

        public float minIntensity = 0f;
        public float maxIntensity = 1f;
        [Range(1, 50)] public int smoothing = 5;

        Queue<float> smoothQueue;
        float lastSum = 0;

        public void Reset() {
            smoothQueue.Clear();
            lastSum = 0;
        }

        void Start() {
            smoothQueue = new Queue<float>(smoothing);

             // Editor or internal light?
             if (light == null) light = GetComponent<Light>();
        }

        void Update() {
            // pop off an item
            while (smoothQueue.Count >= smoothing) {
                lastSum -= smoothQueue.Dequeue();
            }

            // Generate random new item
            float newVal = Random.Range(minIntensity, maxIntensity);
            smoothQueue.Enqueue(newVal);
            lastSum += newVal;

            // New smoothed average
            light.intensity = lastSum / (float)smoothQueue.Count;
        }

    }

}