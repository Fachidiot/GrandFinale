using UnityEngine;

public class RotationConstrainedForBone : OnRig
{
	public Transform source, joint, body;
	public float weight;

	public override void Execute()
	{
		Quaternion beforeRot = joint.rotation;

		Quaternion rotationDelta = source.rotation * Quaternion.Euler(joint.rotation.eulerAngles
																					  - body.rotation.eulerAngles);
																					  
		joint.rotation = Quaternion.Lerp(beforeRot, rotationDelta, weight);
	}
}
