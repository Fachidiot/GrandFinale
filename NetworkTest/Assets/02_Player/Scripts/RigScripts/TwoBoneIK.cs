using UnityEngine;

public class TwoBoneIK : OnRig
{
    public Transform endJoint, target;
    Transform _midJoint, _topJoint;
    public Vector3 posOffset;
    [Range(0, 1)] public float weight, positionWeight, rotationWeight;

    private void Start()
    {
        _midJoint = endJoint.parent;
        _topJoint = _midJoint.parent;
    }

    public override void Execute()
    {
        if (target == null | endJoint == null) return;

        Quaternion aRotation = _topJoint.rotation;
        Quaternion bRotation = _midJoint.rotation;
        Quaternion eRotation = target.rotation;

        Vector3 aPosition = _topJoint.position;
        Vector3 bPosition = _midJoint.position;
        var position = endJoint.position;
        Vector3 cPosition = position;

        Vector3 offsetPos = target.right * posOffset.x + target.up * posOffset.y + target.forward * posOffset.z;
        Vector3 ePosition = Vector3.Lerp(cPosition, target.position + offsetPos, positionWeight * weight);

        Vector3 ab = bPosition - aPosition;
        Vector3 bc = cPosition - bPosition;
        Vector3 ac = cPosition - aPosition;
        Vector3 ae = ePosition - aPosition;

        float abcAngle = TrAngle(ac.magnitude, ab, bc);
        float abeAngle = TrAngle(ae.magnitude, ab, bc);
        float angle = (abcAngle - abeAngle) * Mathf.Rad2Deg;
        Vector3 axis = Vector3.Cross(ab, bc).normalized;

        Quaternion fromToRotation = Quaternion.AngleAxis(angle, axis);

        Quaternion worldQ = fromToRotation * bRotation;
        _midJoint.rotation = worldQ;

        cPosition = endJoint.position;
        ac = cPosition - aPosition;
        Quaternion fromTo = Quaternion.FromToRotation(ac, ae);
        _topJoint.rotation = fromTo * aRotation;

        endJoint.rotation = Quaternion.Lerp(endJoint.rotation, eRotation, rotationWeight * weight);

    }
    private float TrAngle(float len, Vector3 v1, Vector3 v2)
    {
        float len1 = v1.magnitude;
        float len2 = v2.magnitude;
        float ik = Mathf.Clamp((len1 * len1 + len2 * len2 - len * len) / (len1 * len2) / 2.0f, -1.0f, 1.0f);
        return Mathf.Acos(ik);
    }
}