using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class WeaponCollision : OnRig
{
    [Header("Components")]
    public EventsCenter eventsCenter;
    public WeaponController weaponController;
    public Transform rayStartPoint;
    public Transform cameraOrienter;

    [Space(15f)]
    public LayerMask collisionMasks;

    [Header("Values from weapon")]
    public Vector3 rayPositionOffset;
    public float detectionLength;
    public float maxOffset;

    [Header("Position parameters")]
    [SerializeField] private Transform positionOffsetPivot;
    [SerializeField] private Vector3 loweredPosition;
    [SerializeField] private float positionApplyOffset = 0.001f;
    [SerializeField] private float positionChangeRate = 5f;

    [Header("Rotation parameters")]
    [SerializeField] private Transform loweredOffsetPivot;
    [SerializeField] private Vector3 loweredRotation = new Vector3(80, -90, 0);
    [SerializeField] private float rotationApplyOffset = 0.05f;
    [SerializeField] private float ratationChangeRate = 5f;

    private void OnEnable()
    {
        eventsCenter.OnWeaponChange += WeaponChangeEventListener;
    }

    private void WeaponChangeEventListener(bool change)
    {
        if (!change)
        {
            Weapon weapon = weaponController.GETCurrentWeapon;
            detectionLength = weapon.CollisionDetectionLength;
            maxOffset = weapon.MaxZPositionOffsetCollision;
            rayPositionOffset = weapon.InHandsPositionOffset;
            rayPositionOffset.y += 0.05f;
        }
    }

    public override void Execute()
    {
        var startPosition = rayStartPoint.position + (cameraOrienter.forward.normalized * -0.1f + cameraOrienter.right * rayPositionOffset.x + cameraOrienter.up * rayPositionOffset.y);

        Debug.DrawLine(startPosition, startPosition + cameraOrienter.forward * detectionLength, Color.blue);

        if (Physics.Linecast(startPosition, startPosition + cameraOrienter.forward * detectionLength, out RaycastHit hit, collisionMasks))
        {
            if (hit.collider.CompareTag("HitBox")) return;

            float hitDistance = Vector3.Distance(startPosition, hit.point);

            var finalPosition = Vector3.zero;



            if (hitDistance > detectionLength - maxOffset)
            {
                finalPosition = new Vector3(0, 0, -(detectionLength - hitDistance) * 1.2f);

                positionOffsetPivot.localPosition = PositionLerp(positionOffsetPivot.localPosition, finalPosition, positionChangeRate);
                loweredOffsetPivot.localRotation = RotationLerp(loweredOffsetPivot.localRotation, Quaternion.identity, ratationChangeRate);
            }
            else
            {
                finalPosition = loweredPosition;

                positionOffsetPivot.localPosition = PositionLerp(positionOffsetPivot.localPosition, finalPosition, positionChangeRate);
                loweredOffsetPivot.localRotation = RotationLerp(loweredOffsetPivot.localRotation, Quaternion.Euler(loweredRotation), ratationChangeRate);
            }


        }
        else
        {
            positionOffsetPivot.localPosition = PositionLerp(positionOffsetPivot.localPosition, Vector3.zero, positionChangeRate);
            loweredOffsetPivot.localRotation = RotationLerp(loweredOffsetPivot.localRotation, Quaternion.identity, ratationChangeRate);
        }

    }

    Vector3 PositionLerp(Vector3 inputPosition, Vector3 targetPosition, float changeRate)
    {
        if (Vector3.Distance(inputPosition, targetPosition) > positionApplyOffset)
        {
            inputPosition = Vector3.Lerp(inputPosition, targetPosition, Time.deltaTime * changeRate);
            return inputPosition;
        }
        else
        {
            return targetPosition;
        }
    }

    Quaternion RotationLerp(Quaternion inputRotation, Quaternion targetRotation, float changeRate)
    {
        if (Quaternion.Angle(inputRotation, targetRotation) > rotationApplyOffset)
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
