using System;
using System.Collections.Generic;
using UnityEngine;

public class EventsCenter : MonoBehaviour
{
    public delegate void RightHandIKWeightUpdate(float weight);
    public event RightHandIKWeightUpdate OnRightHandIKWeightUpdate;

    public delegate void LeftHandIKWeightUpdate(float weight2);
    public event LeftHandIKWeightUpdate OnLeftHandIKWeightUpdate;

    public delegate void GunWeightUpdate(float activeID);
    public event GunWeightUpdate OnGunWeightUpdate;

    public delegate void GunOffsetRelativeToParent(int handID, int ApplyOffset);
    public event GunOffsetRelativeToParent OnGunOffsetRelativeToParent;

    public delegate void ApplyGunPositionOffset(float applyOffset);
    public event ApplyGunPositionOffset OnApplyGunPositionOffset;

    public delegate void GunParentChange(float handID);
    public event GunParentChange OnGunParentChange;

    public delegate void HandIKTargetChange(int HandId, string TargetComponentName);
    public event HandIKTargetChange OnHandIKTargetChange;

    public delegate void WeaponChange(bool changed);
    public event WeaponChange OnWeaponChange;

    private Dictionary<string, Action<object[]>> _eventHandlers;

    private void Awake()
    {
        _eventHandlers = new Dictionary<string, Action<object[]>>
        {
            { "OnRightHandIKWeightUpdate", p => OnRightHandIKWeightUpdate?.Invoke((float)p[0]) },
            { "OnLeftHandIKWeightUpdate", p => OnLeftHandIKWeightUpdate?.Invoke((float)p[0]) },
            { "OnGunWeightUpdate", p => OnGunWeightUpdate?.Invoke((float)p[0]) },
            { "OnGunOffsetRelativeToParent", p => OnGunOffsetRelativeToParent?.Invoke((int)p[0], (int)p[1]) },
            { "OnApplyGunPositionOffset", p => OnApplyGunPositionOffset?.Invoke((float)p[0]) },
            { "OnGunParentChange", p => OnGunParentChange?.Invoke((float)p[0]) },
            { "OnHandIKTargetChange", p => OnHandIKTargetChange?.Invoke((int)p[0], (string)p[1]) },
            { "OnWeaponChange", p => OnWeaponChange?.Invoke((bool)p[0]) }
        };
    }

    public void EventInvoke(string eventName, object[] parameters)
    {
        if (_eventHandlers.TryGetValue(eventName, out var handler))
        {
            handler?.Invoke(parameters);
        }
    }

    private void OnEnable()
    {
        var animator = GetComponent<Animator>();
        var stateMachineBehaviours = animator.GetBehaviours<StateMachineBehaviour>();
        foreach (var stateMachineBehaviour in stateMachineBehaviours)
        {
            if (stateMachineBehaviour is IEventCenterComponent iEventCenterComponent)
            {
                iEventCenterComponent.EventsCenter = this;
            }
        }
    }
}

