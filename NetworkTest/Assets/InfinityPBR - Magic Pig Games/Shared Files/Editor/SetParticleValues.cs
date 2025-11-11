using UnityEngine;
using UnityEditor;

namespace MagicPigGames
{
    public class SetParticleValues : EditorWindow
    {

        // ------------------------------------------------------------------
        // PRE WARM
        // ------------------------------------------------------------------
        
        [MenuItem("Window/Magic Pig Games/Set Particles/Pre Warm True")]
        private static void SetPreWarmTrue()
        {
            foreach (var obj in Selection.gameObjects)
                SetPreWarm(obj, true);
            
            Debug.Log("Particle system Pre warm set to true for all selected objects and their children.");
        }
        
        [MenuItem("Window/Magic Pig Games/Set Particles/Pre Warm False")]
        private static void SetPreWarmFalse()
        {
            foreach (var obj in Selection.gameObjects)
                SetPreWarm(obj, false);
            
            Debug.Log("Particle system Pre warm set to true for all selected objects and their children.");
        }

        private static void SetPreWarm(GameObject obj, bool value)
        {
            // Apply the change to the current object if it has a ParticleSystem component
            var particleSystem = obj.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var mainModule = particleSystem.main;
                mainModule.prewarm = value;
            }

            // Recursively apply the change to all child objects
            foreach (Transform child in obj.transform)
                SetPreWarm(child.gameObject, value);
        }

        // ------------------------------------------------------------------
        // SIMULATION SPACE
        // ------------------------------------------------------------------
        
        [MenuItem("Window/Magic Pig Games/Set Particles/Simulation Space Local")]
        private static void SetSimulationSpaceToLocal()
        {
            foreach (var obj in Selection.gameObjects)
                SetSimulationSpace(obj, ParticleSystemSimulationSpace.Local);

            Debug.Log("Particle system simulation space set to Local for all selected objects and their children.");
        }
        
        [MenuItem("Window/Magic Pig Games/Set Particles/Simulation Space World")]
        private static void SetSimulationSpaceToWorld()
        {
            foreach (var obj in Selection.gameObjects)
                SetSimulationSpace(obj, ParticleSystemSimulationSpace.World);

            Debug.Log("Particle system simulation space set to World for all selected objects and their children.");
        }

        private static void SetSimulationSpace(GameObject obj, ParticleSystemSimulationSpace value)
        {
            // Apply the change to the current object if it has a ParticleSystem component
            var particleSystem = obj.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var mainModule = particleSystem.main;
                mainModule.simulationSpace = value;
            }

            // Recursively apply the change to all child objects
            foreach (Transform child in obj.transform)
                SetSimulationSpace(child.gameObject, value);
        }
    }
}