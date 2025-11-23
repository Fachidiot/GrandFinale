using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickupOffline : WeaponPickup
{
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private Transform rayCastStartPoint;
    [SerializeField] private float raycastLengh = 1.5f;
    [SerializeField] private LayerMask weaponLayers;

    void Update()
    {
        Debug.DrawLine(rayCastStartPoint.position, rayCastStartPoint.position + rayCastStartPoint.TransformDirection(Vector3.forward) * raycastLengh);
    }

    public override void PickupCheck()
    {
        if (Physics.Raycast(rayCastStartPoint.position, rayCastStartPoint.TransformDirection(Vector3.forward), out RaycastHit hit, raycastLengh, weaponLayers))
        {
            BaseWeapon detectedWeapon = null;

            if (hit.transform.CompareTag("Weapon")) detectedWeapon = hit.transform.GetComponent<BaseWeapon>(); // Check for BaseWeapon

            if (detectedWeapon == null) return;
            
            // Try to pick up into the currently active slot
            int targetSlotIndex = weaponController.currentWeaponSlotIndex;

            // If current slot is unarmed (0) or if the current weapon in that slot is different from the detected one
            if (targetSlotIndex == 0 || weaponController.GETCurrentWeapon.WeaponID != detectedWeapon.WeaponID)
            {
                // If there's already a weapon in the target slot, drop it first
                if (weaponController.GetWeaponInSlot(targetSlotIndex) != null && targetSlotIndex > 0)
                {
                    weaponController.DropWeapon(targetSlotIndex);
                }
                weaponController.PickupWeapon(detectedWeapon, targetSlotIndex);
            }
            else // If the current slot has the same type of weapon, try to find an empty slot
            {
                int emptySlot = weaponController.FindEmptySlot();
                if (emptySlot != -1)
                {
                    weaponController.PickupWeapon(detectedWeapon, emptySlot);
                }
                else
                {
                    Debug.Log("[WeaponPickupOffline] No empty slot found.");
                }
            }
            weaponController.animator.Play("GunPickUp", 1); // Play pickup animation
        }
    }
}
