using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Uween;

public class Simple_A : MonoBehaviour
{
    public GameObject myObject;

    private void Start()
    {
        StartAnim();
    }

    void EndAnim()
    {
        TweenY.Add(myObject, 2.0f, 3).EaseInOutSine().Then(StartAnim);
        TweenRY.Add(myObject, 2.0f, 180).Relative().Then(StartAnim);
    }

    void StartAnim()
    {
        TweenY.Add(myObject, 2.0f, -3).EaseInOutSine().Then(EndAnim);
        TweenRY.Add(myObject, 2.0f, 180).Relative().Then(EndAnim);
    }
}
