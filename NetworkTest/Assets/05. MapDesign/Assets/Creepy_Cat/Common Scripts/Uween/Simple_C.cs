using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Uween;

public class Simple_C : MonoBehaviour
{
    public GameObject myObject;
    public float moveSpeed = 2.0f;
    public float rotateSpeed = 0.5f;

    private void Start()
    {
        Anim_A();
    }

    void Anim_A()
    {
        TweenX.Add(myObject, moveSpeed, 3).EaseInOutSine().Then(Anim_B);
    }

    void Anim_B()
    {
        TweenY.Add(myObject, moveSpeed, 3).EaseInOutSine().Then(Anim_C);
        TweenRY.Add(myObject, rotateSpeed, 90).EaseInOutSine().Relative();
    }

    void Anim_C()
    {
         TweenX.Add(myObject, moveSpeed, -3).EaseInOutSine().Then(Anim_D);
    }

    void Anim_D()
    {
        TweenY.Add(myObject, moveSpeed, -3).EaseInOutSine().Then(Anim_A);
        TweenRY.Add(myObject, rotateSpeed, 90).EaseInOutSine().Relative();
    }
}
