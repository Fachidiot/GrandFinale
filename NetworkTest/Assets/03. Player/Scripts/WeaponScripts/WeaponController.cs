using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.VisualScripting; // Assuming this is needed for something, if not, remove.

public class WeaponController : MonoBehaviour
{
    public Animator animator;
    public EventsCenter eventsCenter;

    [Header("Inventory Slots")]
    [SerializeField] private SlotController[] _slotControllers; // Physical slots on the rig
    [SerializeField] private BaseWeapon[] _weaponInventory = new BaseWeapon[5]; // Logical inventory of IWeapons

    public int currentWeaponSlotIndex = 0; // 0 for unarmed, 1-based for actual weapons
    private BaseWeapon _unarmedWeapon; // A special weapon for the unarmed state

    public SlotController GETCurrentSlot
    {
        get
        {
            if (currentWeaponSlotIndex <= 0 || currentWeaponSlotIndex > _slotControllers.Length)
            {
                return null; // No physical slot for unarmed or invalid index
            }
            return _slotControllers[currentWeaponSlotIndex - 1];
        }
    }
    public IWeapon GETCurrentWeapon
    {
        get
        {
            if (currentWeaponSlotIndex == 0) return _unarmedWeapon;
            if (currentWeaponSlotIndex <= 0 || currentWeaponSlotIndex > _weaponInventory.Length)
            {
                return null;
            }
            return _weaponInventory[currentWeaponSlotIndex - 1];
        }
    }

    public TwoBoneIK rightHandIK;
    public TwoBoneIK leftHandIK;
    public Transform offsetForGun; // transform forOffset
    public GetActualTransform aimPointEffector;

    public bool IsProcessingRemoteWeaponChange { get; private set; } = false;
    public int nextWeaponSlotIndex; // Added for GunChange_SMB to read

    [Header("Gun Detection")]
    public float detectionLength;   //raycast Length
    public Transform detectionStartPoint; //raycast start point
    public LayerMask detectionLayer; //weapons layer
    public float pickUpInputLong;
    //if the weapon has slot type 1, then with a short press the weapon will rise to slot one and with a long press into slot 2
    private IEnumerator _pickUpInputCor; // This was here, keeping it for now, evaluate if needed

    // shoot event, called when fired
    public delegate void Shoot(); // This was here, keeping it for now, evaluate if needed
    public event Shoot OnShoot; // This was here, keeping it for now, evaluate if needed

    // New event for weapon equipped
    public event Action<int> OnWeaponEquipped;


    public bool changed; // true if weapon changed (from GunChange_SMB)
    public bool canShoot; // from GunChange_SMB

    private void Awake()
    {
        _unarmedWeapon = gameObject.AddComponent<UnarmedWeapon>();
    }

    void OnEnable()
    {
        // Populate _weaponInventory and initialize weapons
        if (_slotControllers != null && _weaponInventory.Length != _slotControllers.Length)
        {
            _weaponInventory = new BaseWeapon[_slotControllers.Length];
        }

        for (int i = 0; i < _slotControllers.Length; i++)
        {
            if (_slotControllers[i] != null)
            {
                var weapon = _slotControllers[i].GetComponentInChildren<BaseWeapon>(true); // true to find inactive
                if (weapon != null)
                {
                    _weaponInventory[i] = weapon;
                    weapon.WeaponSlotIndex = i + 1; // 1-based index
                    weapon.Unequip(); // Ensure all weapons are unequipped initially
                }
                else
                {
                    _weaponInventory[i] = null;
                }
            }
        }
        
        // Ensure unarmed weapon is active as default
        _unarmedWeapon.Equip();

        var gunchangeSMBs = animator.GetBehaviours<GunChange_SMB>(); // get gunchange state machine behaviours from animator
        foreach (var gunchangeSMB in gunchangeSMBs)
        {
            gunchangeSMB.setWeaponController(this); // set this as gunchangers in state machine behaviours from animator
        }

        eventsCenter = animator.transform.GetComponent<EventsCenter>();
        if(eventsCenter == null) Debug.LogError("[WeaponController] EventsCenter not found on Animator's transform. IK/rigging events will not work.");

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
        if (eventsCenter != null) // Check for null in case OnEnable failed or script was destroyed
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
    }

    void Start()
    {
        // Initial equip: Unarmed or first weapon if available
        if (_weaponInventory.Length > 0 && _weaponInventory[0] != null)
        {
            EquipWeapon(1); // Equip first weapon if it exists
        }
        else
        {
            EquipWeapon(0); // Equip unarmed by default
        }
    }

    public void EquipWeapon(int slotIndex)
    {
        if (changed || IsProcessingRemoteWeaponChange) // 'changed' is from GunChange_SMB
            return;
        if (currentWeaponSlotIndex == slotIndex) // Already equipped
            return;

        BaseWeapon newWeapon = null;
        if (slotIndex == 0) // Unarmed
        {
            newWeapon = _unarmedWeapon;
        }
        else if (slotIndex > 0 && slotIndex <= _weaponInventory.Length)
        {
            newWeapon = _weaponInventory[slotIndex - 1];
        }

        if (newWeapon == null)
        {
            Debug.LogWarning($"[WeaponController] No weapon found in slot {slotIndex}. Equipping unarmed.");
            EquipWeapon(0); // Fallback to unarmed
            return;
        }
        
        // Unequip current weapon
        GETCurrentWeapon?.Unequip();
        
        // Set the next weapon slot index for GunChange_SMB to read
        this.nextWeaponSlotIndex = slotIndex;

        // Play "Put" animation for the *current* active weapon's slot type
        if (currentWeaponSlotIndex > 0) // If not unarmed
        {
            int animationHash = Animator.StringToHash("PutSlot" + currentWeaponSlotIndex);
            animator.CrossFadeInFixedTime(animationHash, 0.25f, 1);
            // GunChange_SMB will handle calling FinalizeEquip for newWeapon.
        }
        else // Currently unarmed, directly grab new weapon (no "Put" animation needed)
        {
            FinalizeEquip(slotIndex); // Directly finalize equip
        }
        // This.currentWeaponSlotIndex will be set by FinalizeEquip
    }

    // This method is called by GunChange_SMB when the "Put" animation is finished.
    public void FinalizeEquip(int nextSlotIndex)
    {
        BaseWeapon newWeapon = null;
        if (nextSlotIndex == 0)
        {
            newWeapon = _unarmedWeapon;
        }
        else if (nextSlotIndex > 0 && nextSlotIndex <= _weaponInventory.Length)
        {
            newWeapon = _weaponInventory[nextSlotIndex - 1];
        }
        
        if (newWeapon == null)
        {
            Debug.LogWarning($"[WeaponController] No weapon found in slot {nextSlotIndex} during FinalizeEquip. Equipping unarmed.");
            FinalizeEquip(0); // Fallback to unarmed
            return;
        }

        this.currentWeaponSlotIndex = nextSlotIndex;
        newWeapon.Equip();
        changed = false; // Reset changed flag

        // Play the "Grab" animation for the new weapon
        int animationHash = Animator.StringToHash("GrabSlot" + nextSlotIndex);
        animator.CrossFadeInFixedTime(animationHash, 0.25f, 1);

        OnWeaponEquipped?.Invoke(currentWeaponSlotIndex); // Invoke event
    }

    public void StartAttack()
    {
        GETCurrentWeapon?.Attack();
    }

    public void RemoteAttack()
    {
        GETCurrentWeapon?.Attack();
    }

    // New placeholder methods for Drop and Pickup
    public void DropWeapon(int slotIndex)
    {
        if (slotIndex <= 0 || slotIndex > _weaponInventory.Length)
        {
            Debug.LogWarning($"[WeaponController] Invalid slot index {slotIndex} for dropping weapon.");
            return;
        }

        BaseWeapon weaponToDrop = _weaponInventory[slotIndex - 1];
        if (weaponToDrop == null || weaponToDrop == _unarmedWeapon)
        {
            Debug.Log($"[WeaponController] No weapon to drop in slot {slotIndex} or it's unarmed.");
            return;
        }
        
        // Unequip if currently equipped
        if (currentWeaponSlotIndex == slotIndex)
        {
            EquipWeapon(0); // Switch to unarmed
        }

        // Remove from inventory
        _weaponInventory[slotIndex - 1] = null;
        
        // Detach from slot controller
        if (_slotControllers[slotIndex - 1] != null)
        {
            weaponToDrop.transform.SetParent(null); // Detach from parent
            //_slotControllers[slotIndex - 1].ClearSlot(); // Custom method on SlotController to clean up
        }

        // TODO: Instantiate WorldWeapon prefab and network it
        Debug.Log($"[WeaponController] Dropped {weaponToDrop.WeaponName} from slot {slotIndex}.");
        Destroy(weaponToDrop.gameObject); // For now, just destroy
    }

    public void PickupWeapon(BaseWeapon weaponToPickup, int targetSlotIndex)
    {
        if (targetSlotIndex <= 0 || targetSlotIndex > _weaponInventory.Length)
        {
            Debug.LogWarning($"[WeaponController] Invalid slot index {targetSlotIndex} for picking up weapon.");
            return;
        }

        if (_weaponInventory[targetSlotIndex - 1] != null)
        {
            Debug.LogWarning($"[WeaponController] Slot {targetSlotIndex} is not empty. Cannot pick up {weaponToPickup.WeaponName}.");
            return;
        }

        // Add to inventory
        _weaponInventory[targetSlotIndex - 1] = weaponToPickup;
        weaponToPickup.WeaponSlotIndex = targetSlotIndex;

        // Attach to slot controller
        if (_slotControllers[targetSlotIndex - 1] != null)
        {
            weaponToPickup.transform.SetParent(_slotControllers[targetSlotIndex - 1].transform);
            weaponToPickup.transform.localPosition = Vector3.zero;
            weaponToPickup.transform.localRotation = Quaternion.identity;
        }
        
        // Equip if it's the first weapon or a better slot
        if (currentWeaponSlotIndex == 0) // If unarmed
        {
            EquipWeapon(targetSlotIndex);
        }

        Debug.Log($"[WeaponController] Picked up {weaponToPickup.WeaponName} into slot {targetSlotIndex}.");
    }

    public void BeginRemoteWeaponChange()
    {
        IsProcessingRemoteWeaponChange = true;
        StartCoroutine(ResetRemoteWeaponChangeFlag());
    }

    public int FindEmptySlot()
    {
        // Start from 0, which is the first slot.
        for (int i = 0; i < _weaponInventory.Length; i++)
        {
            if (_weaponInventory[i] == null)
            {
                return i + 1; // Return 1-based slot index
            }
        }
        return -1; // No empty slot found
    }

    public BaseWeapon GetWeaponInSlot(int slotIndex)
    {
        if (slotIndex <= 0 || slotIndex > _weaponInventory.Length)
        {
            return null;
        }
        return _weaponInventory[slotIndex - 1];
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
        if (!changing && currentWeapon != null)
        {
            if (currentWeapon is RangedWeapon rangedWeapon && rangedWeapon.AimPoint != null)
            {
                aimPointEffector.getFromTransform = rangedWeapon.AimPoint.transform;
            }
            else // For melee or unarmed, use a default aim point
            {
                aimPointEffector.getFromTransform = null; // Or player's camera forward
            }
        }
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
        if (currentWeapon != null)
        {
            if (currentWeapon is RangedWeapon rangedWeapon)
            {
                 offsetForGun.localPosition = rangedWeapon.InHandsPositionOffset * active; // Use weapon's local position
            }
            else // For other weapon types, this might be 0 or handled differently
            {
                offsetForGun.localPosition = Vector3.zero;
            }
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

        // For ranged weapons, use specific weapon points
        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            if (Enum.TryParse<WeaponPoint.PointType>(pointName, out var pointType))
            {
                if (rangedWeapon.WeaponPointsDict.TryGetValue(pointType, out var targetTransform))
                {
                    handIk.target = targetTransform;
                }
                else
                {
                    handIk.target = null;
                    Debug.LogWarning($"[WeaponController] Ranged weapon {rangedWeapon.WeaponName} does not have point type: {pointName}");
                }
            }
            else
            {
                Debug.LogWarning("Not a correct WeaponPoint Type: " + pointName);
                handIk.target = null;
            }
        }
        else // For melee or unarmed, use the weapon's GameObject transform as a default IK target
        {
            handIk.target = currentWeapon.WeaponGameObject.transform; // Target the weapon's transform
        }
    }
    private IEnumerator ResetRemoteWeaponChangeFlag()
    {
        yield return new WaitForSeconds(0.5f); // 필요에 따라 지연 시간 조정
        IsProcessingRemoteWeaponChange = false;
    }
}