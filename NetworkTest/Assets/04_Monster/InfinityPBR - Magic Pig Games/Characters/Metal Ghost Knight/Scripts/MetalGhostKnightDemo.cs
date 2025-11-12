using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InfinityPBR.Demo
{
    public class MetalGhostKnightDemo : InfinityDemoCharacter
    {
        [Header("Materials")] 
        public Material[] materials;
        public Material[] swordMaterial;
        public Material[] shieldMaterial;

        [Header("Objects")] 
        public List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
        public SkinnedMeshRenderer sword;
        public SkinnedMeshRenderer shield;
        
        [Header("Other")]
        public GameObject castParticle1;
        public ParticleSystem castParticle2;

        public GameObject ragdollHead;
        public GameObject head;
        public GameObject headParticle;
        public float popPower = 3f;
        public bool popHead = false;

        public Transform ragdollParent;

        private PrefabAndObjectManager _prefabAndObjectManager;

        public void Awake() => _prefabAndObjectManager = GetComponent<PrefabAndObjectManager>();

        public void SetMaterial(int i) => SetMaterial(materials[i]);
        public void SetSwordMaterial(int i) => SetSwordMaterial(swordMaterial[i]);
        public void SetShieldMaterial(int i) => SetShieldMaterial(shieldMaterial[i]);

        public void SetMaterial(Material value) => skinnedMeshRenderers.ForEach(x => x.material = value);
        public void SetSwordMaterial(Material value) => sword.material = value;
        public void SetShieldMaterial(Material value) => shield.material = value;

        public void RandomizeTextures()
        {
            SetMaterial(materials[Random.Range(0, materials.Length)]);
            SetSwordMaterial(swordMaterial[Random.Range(0, swordMaterial.Length)]);
            SetShieldMaterial(shieldMaterial[Random.Range(0, shieldMaterial.Length)]);
        }
        public void RandomizeWardrobe() => _prefabAndObjectManager.ActivateRandomAllGroups();
        
        public void RandomizeAll()
        {
            RandomizeTextures();
            RandomizeWardrobe();
        }

        public void Cast1()
        {
            castParticle1.SetActive(true);
            StartCoroutine(TurnOffParticle());
        }

        IEnumerator TurnOffParticle()
        {

            yield return new WaitForSeconds(4.5f);

            castParticle1.SetActive(false);

        }

        public void Cast2Play()
        {
            castParticle2.Play();
        }

        public void Cast2Stop()
        {
            castParticle2.Stop();
        }

        public void PopHeadOff()
        {
            //if (popHead)
            //{
                head.SetActive(false);
                ragdollHead.SetActive(true);
                if (headParticle != null)
                    headParticle.SetActive(true);
                ragdollHead.transform.parent = null;
                ragdollHead.GetComponent<Rigidbody>().AddForce(transform.up * popPower);
                ragdollHead.GetComponent<Rigidbody>().AddForce(-transform.forward * (popPower / 3));
                popHead = false;
            //}
        }

        public void PrepPop()
        {
            popHead = true;
        }

        public void ResetHead()
        {
            ragdollHead.transform.parent = ragdollParent;
            ragdollHead.transform.localPosition = new Vector3(0, 0, 0);
            ragdollHead.transform.localEulerAngles = new Vector3(0, 0, 90);
            ragdollHead.SetActive(false);
            head.SetActive(true);
            if (headParticle != null)
                headParticle.SetActive(false);
        }
    }
}