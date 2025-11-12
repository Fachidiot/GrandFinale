using System.Collections.Generic;
using System;
using UnityEngine;

public class RotationConstrained : OnRig
{
    public override bool isReturnableToOriginalState { get => returner; }
    public bool returner;
    [Range(0, 1)] public float weight;
    public Vector3 axesWeight;

    public enum GetRotationType
    {
        Local,
        World,
    }
    public enum SetRotationType
    {
        Pivot,
        Copy,
        Add,
        Subtract
    }

    [Header("From")]
    public Transform fromObject;
    public GetRotationType fromObjectRotationType;
    public bool inverse;

    [Header("To")]
    public Transform toObject;
    public GetRotationType toObjectRotationType;
    public SetRotationType setRotationType;

    [Header("add rotation")]
    //public Transform addRotationFrom;
    public List<Transform> additionalRotationSource;

    private Quaternion originalRotation;
    private Quaternion lastRotation;

    private void Start()
    {
        originalRotation = toObjectRotationType == GetRotationType.Local ? toObject.localRotation : toObject.rotation;
    }

    public override void ReturnToOriginalState()
    {
        if (toObjectRotationType == GetRotationType.Local) toObject.localRotation = originalRotation;
        else toObject.rotation = originalRotation;
    }

    public override void Execute()
    {
        if (weight <= 0) return;

        var fromRotationValue = fromObjectRotationType == GetRotationType.Local ? fromObject.localRotation : fromObject.rotation;
        if (inverse) fromRotationValue = Quaternion.Inverse(fromRotationValue);
        var toRotationValue = toObjectRotationType == GetRotationType.Local ? toObject.localRotation : toObject.rotation;

        Quaternion finalRotationValue;

        switch (setRotationType)
        {
            case SetRotationType.Pivot:
                finalRotationValue = fromRotationValue * toRotationValue;
                break;
            case SetRotationType.Copy:
                finalRotationValue = fromRotationValue;
                break;
            case SetRotationType.Add:
                finalRotationValue = Quaternion.Euler(toRotationValue.eulerAngles + fromRotationValue.eulerAngles);
                break;
            case SetRotationType.Subtract:
                finalRotationValue = Quaternion.Euler(toRotationValue.eulerAngles - fromRotationValue.eulerAngles);
                break;
            default:
                finalRotationValue = Quaternion.identity;
                break;
        }

        var xRotation = Mathf.Lerp(toRotationValue.eulerAngles.x, finalRotationValue.eulerAngles.x, axesWeight.x);

        var yRotation = Mathf.Lerp(toRotationValue.eulerAngles.y, finalRotationValue.eulerAngles.y, axesWeight.y);

        var zRotation = Mathf.Lerp(toRotationValue.eulerAngles.z, finalRotationValue.eulerAngles.z, axesWeight.z);

       /*  if (addRotationFrom != null)
        {
            toRotationValue = addRotationFrom.localRotation * toRotationValue;
        } */
        if (additionalRotationSource.Count > 0)
        {
            foreach (var source in additionalRotationSource)
            {
                toRotationValue = source.localRotation * toRotationValue;
            }
        }

        finalRotationValue = Quaternion.Slerp(toRotationValue, Quaternion.Euler(xRotation, yRotation, zRotation), weight);

        lastRotation = finalRotationValue;

        if (toObjectRotationType == GetRotationType.Local) toObject.localRotation = finalRotationValue;
        else toObject.rotation = finalRotationValue;
    }
}
