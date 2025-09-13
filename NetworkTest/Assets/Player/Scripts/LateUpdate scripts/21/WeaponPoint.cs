using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPoint : MonoBehaviour
{
    public enum PointType
    {
        RightHandDefault,
        LeftHandDefault,
        LeftHandguard,
        RightHandguard,
        bolt,
        clip
    }
    public PointType pointType;
}
