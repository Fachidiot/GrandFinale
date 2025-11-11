using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SFB_DemonessDemo : MonoBehaviour {

    public float turnValue = 0.0f;      // value from slider of turning
    public float turnRate = 30.0f;      // Speed of turning

    public Material material;
    public bool turningOn = false;
    public float onSpeedMultiplier = 4.0f;  // Speed of turning on texture blend
    public float offSpeedMultiplier = 0.25f; // Speed of turning off texture blend
    public float blendValue = 0.0f;        // Value of blend

    public ParticleSystem[] particlesWithBlend;

    public Toggle[] lowPolyToggles;

	// Use this for initialization
	void Start () {
		
	}

    public void LowPolyRandom()
    {
        foreach (var toggle in lowPolyToggles)
        {
            toggle.isOn = Random.Range(0, 2) == 1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 newAngle = transform.eulerAngles;
        newAngle = new Vector3(newAngle.x, newAngle.y + (turnValue * Time.deltaTime * turnRate), newAngle.z);
        transform.eulerAngles = newAngle;

        if (Input.GetKeyDown(KeyCode.A))
        {
            TurnOnBlend(0.0f);
        }

        if (turningOn)
        {
            blendValue += Time.deltaTime * onSpeedMultiplier;
            if (blendValue > 1)
            {
                blendValue = 1.0f;
                turningOn = false;
            }
            material.SetFloat("_TextureBlend", blendValue);
        }
        else if (blendValue > 0)
        {
            blendValue -= Time.deltaTime * offSpeedMultiplier;
            if (blendValue < 0)
            {
                blendValue = 0;
                for (int i = 0; i < particlesWithBlend.Length; i++)
                {
                    var em = particlesWithBlend[i].emission;
                    em.enabled = false;
                }
            }
            material.SetFloat("_TextureBlend", blendValue);
        }
    }

    public void SetLocomotion(float value)
    {
        GetComponent<Animator>().SetFloat("Locomotion", value);
    }

    public void SetTurning(float value)
    {
        turnValue = value;
    }

    public void TurnOnBlend(float startValue){
        turningOn = true;
        material.SetFloat("_TextureBlend", startValue);
        for (int i = 0; i < particlesWithBlend.Length; i++){
            var em = particlesWithBlend[i].emission;
            em.enabled = true;
        }
    }

    public void SetBlendValue(float value){
        blendValue = value;
        material.SetFloat("_TextureBlend", value);
    }
}
