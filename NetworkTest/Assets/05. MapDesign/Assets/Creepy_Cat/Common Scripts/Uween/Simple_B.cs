using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Uween;

public class Simple_B : MonoBehaviour
{
    public GameObject myObject;

    private void Start()
    {
        StartAnim();
    }

    void EndAnim()
    {
       // TweenY.Add(myObject, 2.0f, 3).EaseInOutSine().Then(StartAnim);
        TweenRY.Add(myObject, 2.0f, 180).Relative().Then(StartAnim);
        

        TweenSXYZ.Add(myObject, 2.0f, new Vector3(1.0f, 1.0f, 1.0f)).EaseInOutSine().Then(StartAnim);
    }

    void StartAnim()
    {
       // TweenY.Add(myObject, 2.0f, -3).EaseInOutSine().Then(EndAnim);
        TweenRY.Add(myObject, 2.0f, 180).Relative().Then(EndAnim);

        TweenSXYZ.Add(myObject, 2.0f, new Vector3(3.0f, 3.0f, 3.0f)).EaseInOutSine().Then(EndAnim);
    }
}
