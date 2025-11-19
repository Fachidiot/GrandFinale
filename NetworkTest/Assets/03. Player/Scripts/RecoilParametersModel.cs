using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RecoilParametersModel
{
    public float recoilForce;

    [Serializable]
    public struct RecoilAxis
    {
        public bool canBeNegative;
        public bool randomValue;
        public float smoothReturnTime;
        [Range(0, 1)] public float percentageOfRecoilValue; //if not use Increasing Curve
        public AnimationCurve axisValueCurve;
    }

    public RecoilAxis[] cameraRecoilAxes;
    public RecoilAxis[] weaponPositionRecoilAxes;
    public RecoilAxis[] weaponRotationRecoilAxes;
}
