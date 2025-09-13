using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GetActualTransform : OnRig
{
    public bool applySelf;
    public Transform getFromTransform;
    private Vector3 _actualPosition;
    private Vector3 _actualLocalPosition;
    private Quaternion _actualRotation;
    private Quaternion _actualLocalRotation;

    [Serializable]
    public struct PositionOffsetElement
    {
        public Transform fromTransform;
        public bool getfromLocal;
    }

    public List<PositionOffsetElement> addOfsetFrom = new List<PositionOffsetElement>();

    public Vector3 GETActualPosition(bool local)
    {
        var offset = Vector3.zero;

        foreach (var offsetFrom in addOfsetFrom)
        {
            offset += offsetFrom.getfromLocal ? offsetFrom.fromTransform.localPosition : offsetFrom.fromTransform.position;
        }

        return local ? _actualLocalPosition + offset : _actualPosition + offset;
    }

    public Quaternion GETActualRotation(bool local)
    {
        return local ? _actualLocalRotation : _actualRotation;
    }
    public override void Execute()
    {
        if (getFromTransform == null) return;

        if (applySelf)
        {
            transform.position = getFromTransform.position;
            transform.rotation = getFromTransform.rotation;
            return;
        }

        _actualLocalPosition = getFromTransform.localPosition;
        _actualPosition = getFromTransform.position;
        _actualRotation = getFromTransform.rotation;
        _actualLocalRotation = getFromTransform.localRotation;
    }
}
