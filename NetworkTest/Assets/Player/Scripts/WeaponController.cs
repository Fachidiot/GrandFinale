using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class WeaponController : MonoBehaviour
{
    public Animator animator;
    public EventsCenter eventsCenter;
    public SlotController[] slots;
    public SlotController GETCurrentSlot => slots[activeID - 1];
    public Weapon GETCurrentWeapon => _weaponCache.TryGetValue(activeID, out var weapon) ? weapon : null;
    private readonly Dictionary<int, Weapon> _weaponCache = new Dictionary<int, Weapon>();

    public TwoBoneIK rightHandIK;
    public TwoBoneIK leftHandIK;
    public int activeID; // activeGun
    public int nextID;
    public Transform offsetForGun; // transform forOffset 
    public GetActualTransform aimPointEffector;


    [Header("Gun Detection")]
    public float detectionLength;   //raycast Length
    public Transform detectionStartPoint; //raycast start point
    public LayerMask detectionLayer; //weapons layer
    public float pickUpInputLong;
    //if the weapon has slot type 1, then with a short press the weapon will rise to slot one and with a long press into slot 2
    private IEnumerator _pickUpInputCor;

    // shoot event, called when fired
    public delegate void Shoot();
    public event Shoot OnShoot;
    public bool changed; // true if weapon changed
    public bool canShoot;

    void OnEnable()
    {
        // Cache weapon components for faster access
        _weaponCache.Clear();
        for (int i = 0; i < slots.Length; i++)
        {
            var weapon = slots[i].GetComponentInChildren<Weapon>();
            if (weapon != null)
            {
                // Slot IDs are 1-based, array indices are 0-based
                _weaponCache[i + 1] = weapon;
            }
        }

        var gunchangeSMBs = animator.GetBehaviours<GunChange_SMB>(); // get gunchange state machine behaviours from animator
        foreach (var gunchangeSMB in gunchangeSMBs)
        {
            gunchangeSMB.setWeaponController(this); // set this as gunchangers in state machine behaviours from animator
        }

        eventsCenter = animator.transform.GetComponent<EventsCenter>();

        // subscribes on events
        eventsCenter.OnRightHandIKWeightUpdate += ApplyRightHandIkWeight;
        eventsCenter.OnLeftHandIKWeightUpdate += ApplyLeftHandIkWeight;
        eventsCenter.OnGunWeightUpdate += ApplyGunActiveWeight;
        eventsCenter.OnGunOffsetRelativeToParent += ApplyGunOffsetRelativeToParent;
        eventsCenter.OnGunParentChange += ApplyGunParent;
        eventsCenter.OnHandIKTargetChange += ApplyHandsIKTarget;
        eventsCenter.OnApplyGunPositionOffset += ApplyGunPositionOffsetInHands;
        eventsCenter.OnWeaponChange += GunChangeCheck;
    }

    private void OnDisable()
    {
        eventsCenter.OnRightHandIKWeightUpdate -= ApplyRightHandIkWeight;
        eventsCenter.OnLeftHandIKWeightUpdate -= ApplyLeftHandIkWeight;
        eventsCenter.OnGunWeightUpdate -= ApplyGunActiveWeight;
        eventsCenter.OnGunOffsetRelativeToParent -= ApplyGunOffsetRelativeToParent;
        eventsCenter.OnGunParentChange -= ApplyGunParent;
        eventsCenter.OnHandIKTargetChange -= ApplyHandsIKTarget;
        eventsCenter.OnApplyGunPositionOffset -= ApplyGunPositionOffsetInHands;
        eventsCenter.OnWeaponChange -= GunChangeCheck;
    }

    public void StartShoot()
    {
        var currentWeapon = GETCurrentWeapon;
        if (currentWeapon != null && currentWeapon.Shoot())
            OnShoot?.Invoke();
    }

    IEnumerator setLHandIkWeight(float t, float pause)
    {
        yield return new WaitForSeconds(pause);
        float startWeight = leftHandIK.weight;

        float t00 = 0;
        while (t00 < 0.1f)
        {
            leftHandIK.weight = Mathf.Lerp(startWeight, 0, t00 / 0.1f);
            t00 += Time.deltaTime;

            yield return null;
        }

        ApplyHandsIKTarget(1, "LeftHandDefault");

        float t0 = 0;
        while (t0 < t)
        {
            leftHandIK.weight = Mathf.Lerp(0, 1, t0 / t);
            t0 += Time.deltaTime;

            yield return null;
        }
        leftHandIK.weight = 1;

    }

    void GunChangeCheck(bool changing)
    {
        canShoot = !changing;
        this.changed = changing;

        var currentWeapon = GETCurrentWeapon;
        if (!changing && currentWeapon != null && currentWeapon.AimPoint != null) 
            aimPointEffector.getFromTransform = currentWeapon.AimPoint.transform;
    }

    void ApplyGunOffsetRelativeToParent(int handId, int applyOffset) => GETCurrentSlot.ApplyHandOffset(handId, applyOffset == 1);

    void ApplyGunPositionOffsetInHands(float active)
    {
        var currentWeapon = GETCurrentWeapon;
        if (currentWeapon != null)
            offsetForGun.localPosition = currentWeapon.InHandsPositionOffset * active;
    }

    void ApplyRightHandIkWeight(float weight) => rightHandIK.weight = weight;

    void ApplyLeftHandIkWeight(float weight) => leftHandIK.weight = weight;

    void ApplyGunParent(float handActive) => GETCurrentSlot.HandActive = handActive;

    void ApplyGunActiveWeight(float weight) => GETCurrentSlot.weight = weight;

    void ApplyHandsIKTarget(int handId, string pointName)
    {
        var handIk = handId > 0 ? leftHandIK : rightHandIK;
        var currentWeapon = GETCurrentWeapon;

        if (currentWeapon == null) return;

        // Use the cached dictionary for a fast lookup
        if (Enum.TryParse<WeaponPoint.PointType>(pointName, out var pointType))
        {
            if (currentWeapon.WeaponPointsDict.TryGetValue(pointType, out var targetTransform))
            {
                handIk.target = targetTransform;
            }
            else
            {
                handIk.target = null; // Or a default target
            }
        }
        else
        {
            Debug.LogWarning("Not a correct WeaponPoint Type: " + pointName);
            handIk.target = null;
        }
    }

    public void ToChange(int nextGunSlotID)
    {
        if (changed) return;
        if (activeID == nextGunSlotID) return;

        // Use Animator.StringToHash for better performance and to avoid string allocations.
        string animaName = "PutSlot" + activeID;
        int animationHash = Animator.StringToHash(animaName);
        
        this.nextID = nextGunSlotID;

        animator.CrossFadeInFixedTime(animationHash, 0.25f, 1);
    }
}
