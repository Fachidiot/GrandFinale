using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodySlope_Handler : MonoBehaviour
{
    [SerializeField] private BodySlope bodySlope;
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private float bodySlopeChangeRate = 5f;
    [SerializeField] private float valueApplyOffset = 0.01f;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private LayerMask collisionMask;
    public float targetAngle;
    private float hitDistance;


    public void setInput(float InputAngle)
    {
        targetAngle = InputAngle * maxSlopeAngle * hitDistance;
    }

    void Update()
    {
        bodySlope.slopeAngle = SmoothValue(bodySlope.slopeAngle, targetAngle, bodySlopeChangeRate);
        CheckBodyCollision();
    }

    private float SmoothValue(float inputValue, float targetvalue, float changeRateValue)
    {
        if (inputValue < targetvalue - valueApplyOffset || inputValue > targetvalue + valueApplyOffset)
        {
            inputValue = Mathf.Lerp(inputValue, targetvalue, Time.deltaTime * changeRateValue);

            return inputValue;
        }
        else
        {
            return targetvalue;
        }
    }

    private void CheckBodyCollision()
    {
        if (Physics.Linecast(transform.position + Vector3.up * playerCamera.localPosition.y, transform.position + Vector3.up * playerCamera.localPosition.y + playerCamera.right * (Input.GetAxisRaw("Slope") * 0.51f), out RaycastHit raycastHit, collisionMask))
        {
            hitDistance = Mathf.Lerp(0, 1, Vector3.Distance(transform.position + Vector3.up * playerCamera.localPosition.y, raycastHit.point) / 0.5f);
        }
        else
        {
            hitDistance = 1;
        }
    }
}
