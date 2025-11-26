using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;

public class WeaponController : MonoBehaviour
{
    public Animator animator;
    public EventsCenter eventsCenter;
    public SlotController[] slots;
    public SlotController GETCurrentSlot
    {
        get
        {
            if (activeID <= 0 || activeID > slots.Length)
            {
                return null;
            }
            return slots[activeID - 1];
        }
    }
    public IWeapon GETCurrentWeapon => _weaponCache.TryGetValue(activeID, out var weapon) ? weapon : null;
    private readonly Dictionary<int, IWeapon> _weaponCache = new Dictionary<int, IWeapon>();

    public TwoBoneIK rightHandIK;
    public TwoBoneIK leftHandIK;
    public int activeID; // activeGun
    public int nextID;
    public Transform offsetForGun; // transform forOffset
    public GetActualTransform aimPointEffector;

    public bool IsProcessingRemoteWeaponChange { get; private set; } = false;


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

    private BaseWeapon _unarmedWeapon;

    private void InitialCheck()
    {
        if (animator == null)
            Debug.LogError("WeaponController: Animator is not assigned. Weapon animations will not function.");
        if (eventsCenter == null)
            Debug.LogError("WeaponController: EventsCenter is not assigned. Event subscriptions will fail.");
        if (slots == null || slots.Length == 0)
            Debug.LogError("WeaponController: Weapon slots are not assigned or empty. Weapon switching will not function.");
        if (rightHandIK == null)
            Debug.LogError("WeaponController: RightHandIK is not assigned. Right hand IK will not function.");
        if (leftHandIK == null)
            Debug.LogError("WeaponController: LeftHandIK is not assigned. Left hand IK will not function.");
        if (offsetForGun == null)
            Debug.LogError("WeaponController: OffsetForGun transform is not assigned. Weapon position offsets will not function.");
        if (aimPointEffector == null)
            Debug.LogError("WeaponController: AimPointEffector is not assigned. Aiming functionality will be affected.");
        if (_unarmedWeapon == null)
            Debug.LogError("WeaponController: UnarmedWeapon component not found. Unarmed combat will not function correctly.");
    }

    void OnEnable()
    {
        // Cache weapon components for faster access
        _weaponCache.Clear();

        _unarmedWeapon = GetComponent<UnarmedWeapon>();
        _weaponCache[0] = _unarmedWeapon;
        for (int i = 0; i < slots.Length; i++)
        {
            var weapon = slots[i].GetComponentInChildren<IWeapon>();
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


        UIEvents.PlayerInitialized(this);
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

    // void InitializeWeapon()
    // {
    //     int i = 1;
    //     foreach (var slot in slots)
    //     {
    //         if (slot.slotActive)
    //         {
    //             ToChange(i);
    //             return;
    //         }
    //         ++i;
    //     }
    // }

    void Start()
    {
        InitialCheck();

        activeID = 0;
        nextID = -1;
        animator.CrossFadeInFixedTime("UnArmIdle", 0.25f, 3);
        changed = false;

        EquipmentManager.Instance.OnSlotEquip += SlotEquip;
        EquipmentManager.Instance.OnSlotUnequip += SlotUnequip;
    }

    private float lastChangeDebugTime = 0f;
    void Update()
    {
        // Temp Code : 추후에 고쳐야함!!!
        // 고쳐야할 요소 : 장비 스왑키를 연타하다 보면 애니메이션 끝나기도 전에 스왑이 되면서 changed가 true상태가 됨.
        if (changed && lastChangeDebugTime + 3 < Time.time)
        {
            Debug.LogWarning("WeaponController: GunChange_SMB 실행전 애니메이션 중복 실행 오류 발생. 임시방편 완화 실행.");
            changed = false;
        }
    }

    public void StartShoot()
    {
        var currentWeapon = GETCurrentWeapon;
        if (currentWeapon != null && !changed && currentWeapon.Attack())
        {
            OnShoot?.Invoke();
        }
    }

    public void RemoteShoot()
    {
        var currentWeapon = GETCurrentWeapon;
        currentWeapon?.Attack();
    }

    private void SlotEquip(int slotIndex, RelicData relicData)
    {
        Debug.Log($"{slotIndex} 슬롯 -> {relicData}");

        // 슬롯이 비어있으면 장비 착용.
        if (!slots[slotIndex].slotActive)
            slots[slotIndex].AddSlot(relicData.modelPrefab);
    }

    private void SlotUnequip(int slotIndex)
    {
        Debug.Log($"{slotIndex} 슬롯 해제.");

        // 슬롯에 장비가 있다면 해제.
        if (slots[slotIndex].slotActive)
            slots[slotIndex].DeleteSlot();
    }

    public void ToChange(int nextGunSlotID)
    {
        if (changed)
            return;
        if (activeID == nextGunSlotID)
            return;

        if (nextGunSlotID > 0 && nextGunSlotID <= slots.Length)
        {
            // 슬롯에 무기가 있는지 체크.
            if (!slots[nextGunSlotID - 1].slotActive)
                return;
        }

        // Unarmed -> Weapon 예외처리.
        if (activeID == 0)
        {   // 아래 PutSlot 애니메이션 없이 바로 GetSlot애니메이션으로 넘어가기.
            ToGetSlot(nextGunSlotID);
            return;
        }

        string animaName = "PutSlot" + activeID;
        int animationHash = Animator.StringToHash(animaName);

        this.nextID = nextGunSlotID;

        animator.CrossFadeInFixedTime(animationHash, 0.25f, 1);
        lastChangeDebugTime = Time.time;
    }

    public void ToGetSlot(int nextGunSlotID)
    {
        if (changed)
            return;
        if (activeID == nextGunSlotID)
            return;

        animator.CrossFadeInFixedTime("Weapon", 0.25f, 3);
        string animaName = "GrabSlot" + nextGunSlotID;
        int animationHash = Animator.StringToHash(animaName);

        this.activeID = nextGunSlotID;

        animator.CrossFadeInFixedTime(animationHash, 0.25f, 1);
        animator.SetBool("isArmed", true);
    }

    public void RemoteToChange(int nextGunSlotID)
    {
        if (changed)
            return;
        if (activeID == nextGunSlotID)
            return;

        IsProcessingRemoteWeaponChange = true; // 플래그 설정

        string animaName = "PutSlot" + activeID;
        int animationHash = Animator.StringToHash(animaName);

        this.nextID = nextGunSlotID;

        animator.CrossFadeInFixedTime(animationHash, 0.25f, 1);

        // 일정 시간 후 플래그를 리셋합니다. (애니메이션 완료 시점에 맞게 조정 필요)
        StartCoroutine(ResetRemoteWeaponChangeFlag());
    }

    private IEnumerator ResetRemoteWeaponChangeFlag()
    {
        yield return new WaitForSeconds(0.5f); // 필요에 따라 지연 시간 조정
        IsProcessingRemoteWeaponChange = false;
    }

    #region AnimIKFunctions
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

    void ApplyGunOffsetRelativeToParent(int handId, int applyOffset)
    {
        var currentSlot = GETCurrentSlot;
        if (currentSlot != null)
            currentSlot.ApplyHandOffset(handId, applyOffset == 1);
    }

    void ApplyGunPositionOffsetInHands(float active)
    {
        var currentWeapon = GETCurrentWeapon;
        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            offsetForGun.localPosition = rangedWeapon.InHandsPositionOffset * active;
        }
    }

    void ApplyRightHandIkWeight(float weight) => rightHandIK.weight = weight;

    void ApplyLeftHandIkWeight(float weight) => leftHandIK.weight = weight;

    void ApplyGunParent(float handActive)
    {
        var currentSlot = GETCurrentSlot;
        if (currentSlot != null)
            currentSlot.HandActive = handActive;
    }

    void ApplyGunActiveWeight(float weight)
    {
        var currentSlot = GETCurrentSlot;
        if (currentSlot != null)
            currentSlot.weight = weight;
    }

    void ApplyHandsIKTarget(int handId, string pointName)
    {
        var handIk = handId > 0 ? leftHandIK : rightHandIK;
        var currentWeapon = GETCurrentWeapon;

        if (currentWeapon == null) return;

        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            // Use the cached dictionary for a fast lookup
            if (Enum.TryParse<WeaponPoint.PointType>(pointName, out var pointType))
            {
                if (rangedWeapon.WeaponPointsDict.TryGetValue(pointType, out var targetTransform))
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
    }
    #endregion
}
