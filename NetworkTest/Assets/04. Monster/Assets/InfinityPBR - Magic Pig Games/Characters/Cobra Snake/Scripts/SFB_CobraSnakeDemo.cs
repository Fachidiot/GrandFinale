using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_CobraSnakeDemo : MonoBehaviour
{

    public float Locomotion = 0.0f;
    public bool isCalm = true;

    public ParticleSystem snakeBreath;
    public GameObject spitBall;
    public Transform spitPosition;

    private Animator animator;

    // Use this for initialization
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateLocomotion(float value)
    {
        animator.SetFloat("Locomotion", value);
    }

    public void UpdateStyle(float value)
    {
        animator.SetFloat("AttackStatus", value);
    }

    public void CalmToAttack()
    {
        animator.SetTrigger("CalmToAttack");
        StartCoroutine(Delay(0.5f, "AttackStatus", 1.0f));
    }

    public void AttackToCalm()
    {
        animator.SetTrigger("AttackToCalm");
        StartCoroutine(Delay(0.5f, "AttackStatus", 0.0f));
    }

    IEnumerator Delay(float value, string name, float triggerValue)
    {
        yield return new WaitForSeconds(value);
        animator.SetFloat(name, triggerValue);
    }

    public void StartSnakeBreath()
    {
        snakeBreath.Play(true);
    }

    public void StopSnakeBreath()
    {
        snakeBreath.Stop(true);
    }

    public void Spit()
    {
        GameObject ball;
        ball = Instantiate(spitBall, spitPosition.position, Quaternion.identity);
        ball.transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
    }
}
