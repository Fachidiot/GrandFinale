using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyTiltInSprint : MonoBehaviour
{
    public MoveState standState;
    public CharacterMove characterMove;
    public Transform bodyRotator;

    public float maxTilrDeg = 15;
    public float tiltForce;
    public float tiltDeg;

    public float mouseXMove;
    // Start is called before the first frame update
    void Start()
    {
        standState = characterMove.standState;
    }

    public void SetMouseXMove(float inputValue)
    {
        mouseXMove = inputValue;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (standState.isSprint)
        {

            tiltDeg = -mouseXMove * maxTilrDeg;
            tiltDeg = Mathf.Clamp(tiltDeg, -maxTilrDeg, maxTilrDeg);
            // bodyRotator.localRotation = Quaternion.Euler(0, 0, mouseXMove * tiltForce * Time.deltaTime);
            bodyRotator.localRotation = RotationLerp(bodyRotator.localRotation, Quaternion.Euler(0, 0, tiltDeg), tiltForce);
        }
        else
        {
            bodyRotator.localRotation = RotationLerp(bodyRotator.localRotation, Quaternion.identity, 10f);
        }
    }

    Quaternion RotationLerp(Quaternion inputRotation, Quaternion targetRotation, float changeRate)
    {
        if (Quaternion.Angle(inputRotation, targetRotation) > 0.01f)
        {
            inputRotation = Quaternion.Slerp(inputRotation, targetRotation, Time.deltaTime * changeRate);
            return inputRotation;
        }
        else
        {
            return targetRotation;
        }
    }
}
