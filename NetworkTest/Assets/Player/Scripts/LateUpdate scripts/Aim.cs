using UnityEngine;

public class Aim : OnRig
{
    public override bool isReturnableToOriginalState { get => returner; }
    public bool returner;

    public float weight;
    public Vector3 axesWeight;
    public Transform target;
    public Vector3 rotationOffset;
    private Quaternion startRotation;


    private void Start()
    {
        startRotation = transform.localRotation;
    }

    public override void ReturnToOriginalState()
    {
       transform.localRotation = startRotation;
    }

    public override void Execute()
    {
        if (weight <= 0) return;

        Vector3 dir = target.position - transform.position;
        Quaternion lookRot = Quaternion.LookRotation(dir, transform.up);

        var finalLocalRotation = Quaternion.Slerp(startRotation, (Quaternion.Inverse(transform.parent.rotation) * lookRot), weight);


        var xRotation = Mathf.Lerp(startRotation.eulerAngles.x, finalLocalRotation.eulerAngles.x + rotationOffset.x, axesWeight.x);
        var yRotation = Mathf.Lerp(startRotation.eulerAngles.y, finalLocalRotation.eulerAngles.y + rotationOffset.y, axesWeight.y);
        var zRotation = Mathf.Lerp(startRotation.eulerAngles.z, finalLocalRotation.eulerAngles.z + rotationOffset.z, axesWeight.z);

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
    }
}