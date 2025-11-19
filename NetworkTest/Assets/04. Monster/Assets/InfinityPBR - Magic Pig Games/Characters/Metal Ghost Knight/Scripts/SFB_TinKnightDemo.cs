using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_TinKnightDemo : MonoBehaviour {

    public Animator animator;
    public GameObject castParticle1;
    public ParticleSystem castParticle2;

    public GameObject ragdollHead;
    public GameObject head;
    public GameObject headParticle;
    public float popPower = 3f;
    public bool popHead = false;

    public Transform ragdollParent;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Locomotion(float locomotion)
    {
        animator.SetFloat("Locomotion", locomotion);
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
        if (popHead)
        {
            head.SetActive(false);
            ragdollHead.SetActive(true);
            headParticle.SetActive(true);
            ragdollHead.transform.parent = null;
            ragdollHead.GetComponent<Rigidbody>().AddForce(transform.up * popPower);
            ragdollHead.GetComponent<Rigidbody>().AddForce(-transform.forward * (popPower / 3));
            popHead = false;
        }
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
        headParticle.SetActive(false);
    }
}
