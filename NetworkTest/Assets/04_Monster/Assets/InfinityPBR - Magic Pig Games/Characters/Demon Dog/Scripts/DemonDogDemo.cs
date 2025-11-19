using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InfinityPBR.Demo
{
   public class DemonDogDemo : InfinityDemoCharacter
   {
      [Header("Demon Dog Materials")]
      public Material[] materialsMain;
      public Material[] materialsEyes;

      [Header("Randoms")] 
      public bool randomWalkStyle = false;
      public bool randomIdleStyle = false;
      public bool randomAngryIdleBreakStyle = false;
      
      private static readonly int Locomotion = Animator.StringToHash("Locomotion");

      public override void Start()
      {
         base.Start();

         var locomotionObject = GameObject.Find("Locomotion");
         var locomotionScript = locomotionObject.GetComponent<InfinityDemoFloatSlider>();
         locomotionScript.negativeOne = true;
      }

      public void RandomBodyTexture() => UpdateBodyTextures(Random.Range(0, materialsMain.Length));
      public void RandomEyeTexture() => UpdateEyeTextures(Random.Range(0, materialsEyes.Length));

      public void UpdateBodyTextures(int index)
      {
         var materials = skinnedMeshRenderer.sharedMaterials;
         materials[0] = materialsMain[index];
         skinnedMeshRenderer.sharedMaterials = materials;
      }
      
      public void UpdateEyeTextures(int index)
      {
         var materials = skinnedMeshRenderer.sharedMaterials;
         materials[1] = materialsEyes[index];
         skinnedMeshRenderer.sharedMaterials = materials;
      }
   }
}
