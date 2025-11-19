using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SFB_HarlequinDemo : MonoBehaviour {

    public Animator animator;

    public Material[] materials;
    public SkinnedMeshRenderer[] renderers;

    public Transform spawnPosRight;
    public Transform spawnPosLeft;
    public GameObject shurikenParticle;

    public Transform demoTarget;

    public Toggle[] wardrobeToggles;

	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();
	}

    public void RandomizeWardrobe()
    {
        foreach (var toggle in wardrobeToggles)
        {
            toggle.isOn = Random.Range(0, 2) == 1;
        }
    }

    public void Crouch()
    {
        animator.SetFloat("Crouch", 1.0f);
    }

    public void Stand()
    {
        animator.SetFloat("Crouch", 0.0f);
    }

    public void SetLocomotion(float locomotion)
    {
        animator.SetFloat("Locomotion", locomotion);
    }

    public void SetMaterial(int materialIndex)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            mats[0] = materials[materialIndex];
            renderers[i].materials = mats;
        }
    }

    public void ShootLeft()
    {
        Shoot(spawnPosLeft);
    }

    public void ShootRight()
    {
        Shoot(spawnPosRight);
    }

    public void Shoot(Transform spawnPos)
    {
        GameObject newShuriken = Instantiate(shurikenParticle, spawnPos.position, Quaternion.identity);
        newShuriken.transform.LookAt(demoTarget);
    }
}
