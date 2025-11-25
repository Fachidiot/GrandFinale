using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Uween;

// Note: A code to use Uween to increase vars (via empty gameobject positions, i crushed you uween.
// Why this library do not have functions to tween a float... I don't understand coders sometime!)
public class Simple_D : MonoBehaviour
{

    [SerializeField]
    public Light lightObj;

    public GameObject myObject;

    public float varMinValue = 0.0f;
    public float varMaxValue = 2.0f;

    public float varChangeSpeed = 0.5f;


    private float myVar;

    private void Start(){
        Anim_A();
    }

    void Anim_A(){
        TweenX.Add(myObject, varChangeSpeed, varMaxValue).EaseInOutSine().Then(Anim_B);
    }

    void Anim_B(){
        TweenX.Add(myObject, varChangeSpeed, varMinValue).EaseInOutSine().Then(Anim_A);
    }

    void Update()
    {
        // I'm cheating, i use the fake gameobject position to increase light power
        myVar = myObject.transform.position.x;
        lightObj.intensity = myVar;
      //  Debug.Log(myVar);
    }
}
