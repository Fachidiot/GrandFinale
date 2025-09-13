using UnityEngine;
using System.Collections;

public class BodySlope : OnRig
{
    public override bool isReturnableToOriginalState { get => returner; }
    public bool returner;
    public bool canSlope = true;
    public Transform constrainedObject;
    public Transform bodyTransform;
    public float weight = 1;
    public float slopeAngle;

    private Quaternion startRotation;

    private void Start()
    {
        startRotation = constrainedObject.localRotation;
    }

    public override void ReturnToOriginalState()
    {
        constrainedObject.localRotation = startRotation;
    }

    public override void Execute()
    {
        if (!canSlope) return;

        Quaternion defRot = constrainedObject.rotation;
        Quaternion bodyRotation = bodyTransform.rotation;

        transform.localRotation = Quaternion.Euler(0, 0, slopeAngle);

        Quaternion rotationDelta = Quaternion.Euler(transform.localRotation.eulerAngles + bodyRotation.eulerAngles)
                                   * Quaternion.Euler(defRot.eulerAngles - bodyRotation.eulerAngles);

        constrainedObject.rotation = Quaternion.Lerp(defRot, rotationDelta, weight);
    }
}
