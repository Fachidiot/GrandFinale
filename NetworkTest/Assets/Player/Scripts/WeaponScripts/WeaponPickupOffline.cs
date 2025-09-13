using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickupOffline : WeaponPickup
{
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private Transform rayCastStartPoint;
    [SerializeField] private float raycastLengh = 1.5f;
    [SerializeField] private LayerMask weaponLayers;


    void Start()
    {

    }



    void Update()
    {
        Debug.DrawLine(rayCastStartPoint.position, rayCastStartPoint.position + rayCastStartPoint.TransformDirection(Vector3.forward) * raycastLengh);
    }

    public override void PickupCheck()
    {
        //Debug.DrawLine(detectionStartPoint.position, detectionStartPoint.TransformDirection(Vector3.forward) * detectionLength, Color.red);

        if (Physics.Raycast(rayCastStartPoint.position, rayCastStartPoint.TransformDirection(Vector3.forward), out RaycastHit hit, raycastLengh, weaponLayers))
        {

            Weapon detectedGun = null;

            if (hit.transform.CompareTag("Weapon")) detectedGun = hit.transform.GetComponent<Weapon>();

            if (detectedGun == null) return;

            if (weaponController.GETCurrentWeapon.Type == detectedGun.Type)
            {
                RaiseTheGun(hit.transform, weaponController.activeID - 1);
                weaponController.animator.Play("GunPickUp", 1);
            }
            else
            {
                RaiseTheGun(hit.transform, (int)detectedGun.Type - 1);
            }
        }

    }

    void RaiseTheGun(Transform gun, int slotID)
    {
        var oldGun = weaponController.slots[slotID].GetComponentInChildren<Weapon>().transform;
        gun.GetComponent<Rigidbody>().isKinematic = true;
        gun.GetComponent<BoxCollider>().enabled = false;
        gun.SetParent(weaponController.slots[slotID].transform);
        gun.localPosition = Vector3.zero;
        gun.localRotation = Quaternion.identity;
        DropTheGun(oldGun);
    }

    private void DropTheGun(Transform gun)
    {
        gun.SetParent(null);
        gun.transform.position = transform.position + Vector3.up * 2 + (rayCastStartPoint.forward * 0.3f);

        if (gun.GetComponent<Rigidbody>() != null)
        {
            gun.GetComponent<BoxCollider>().enabled = true;
            gun.GetComponent<Rigidbody>().isKinematic = false;
            var up = rayCastStartPoint.up;
            gun.GetComponent<Rigidbody>().AddForce(rayCastStartPoint.forward * 2f + up * 3f, ForceMode.VelocityChange);
            gun.GetComponent<Rigidbody>().AddTorque(rayCastStartPoint.right * Random.Range(1, 7) + up * Random.Range(1, 7), ForceMode.VelocityChange);
        }
    }
}
