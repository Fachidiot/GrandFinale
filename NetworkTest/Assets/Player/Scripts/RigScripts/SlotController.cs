using System;
using UnityEngine;

public class SlotController : OnRig
{
    public Transform constrained;
    [Range(0, 1)] public float weight;
    public Transform inactiveSlot;

    [Serializable]
    public class HandSlot
    {
        public Transform pointer;
        public Vector3 actualPos;
        public Vector3 positionOffset;
        public Quaternion rotationOffset = Quaternion.identity;
    }

    [SerializeField] [Range(0, 1)] private float handActive;
    public float HandActive
    {
        get => handActive;
        set => handActive = Mathf.Clamp(value, 0, 1);
    }

    public HandSlot[] hands = new HandSlot[2];

    public void ApplyHandOffset(int handID, bool applyOffset)
    {
        hands[handID].positionOffset = applyOffset ? constrained.InverseTransformPoint(constrained.position) - constrained.InverseTransformPoint(hands[handID].actualPos) : Vector3.zero;

        hands[handID].rotationOffset = applyOffset ? Quaternion.Inverse(hands[handID].pointer.rotation) * constrained.rotation : Quaternion.identity;
    }

    public override void Execute()
    {
        foreach (var pr in hands)
        {
            pr.actualPos = pr.pointer.position;
        }

        Vector3 inHandsPosition = Vector3.Lerp(hands[0].pointer.position, hands[1].pointer.position, HandActive);
        Vector3 inHandsPositionOffset = Vector3.Lerp(hands[0].positionOffset, hands[1].positionOffset, HandActive);

        inHandsPosition += inHandsPositionOffset;

        var rightHandRotationOffset = hands[0].rotationOffset == Quaternion.identity ? hands[0].pointer.rotation : hands[0].pointer.rotation * hands[0].rotationOffset;

        var leftHandRotationOffset = hands[1].rotationOffset == Quaternion.identity ? hands[1].pointer.rotation : hands[1].pointer.rotation * hands[1].rotationOffset;

        var inHandsRotation = Quaternion.identity;

        if (rightHandRotationOffset != Quaternion.identity | leftHandRotationOffset != Quaternion.identity)
            inHandsRotation = Quaternion.Lerp(rightHandRotationOffset, leftHandRotationOffset, Mathf.Clamp(HandActive, 0, 0.99f));


        constrained.position = Vector3.Lerp(inactiveSlot.position, inHandsPosition, weight);

        constrained.rotation = Quaternion.Lerp(inactiveSlot.rotation, inHandsRotation, weight);
    }
}
