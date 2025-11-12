using UnityEngine;

public class ViewingResistance : MonoBehaviour
{
    public WeaponController weaponController;
    public EventsCenter eventsCenter;
    public Transform pivot;
    public float resistanceForce;
    public float resistanceSmoothing;

    private void OnEnable()
    {
        eventsCenter.OnWeaponChange += WeaponChangeCheck;
    }
    private void OnDisable()
    {
        eventsCenter.OnWeaponChange -= WeaponChangeCheck;
    }

    void WeaponChangeCheck(bool changing)
    {
        if (!changing)
        {
            resistanceForce = weaponController.GETCurrentWeapon.ResistanceForce;
            resistanceSmoothing = weaponController.GETCurrentWeapon.ResistanceSmoothing;
        }
    }

    private void Update()
    {
        var vertical = -Input.GetAxis("Mouse Y") * resistanceForce;
        var horizontal = Input.GetAxis("Mouse X") * resistanceForce;

        pivot.localRotation = Quaternion.Lerp(
            pivot.localRotation,
            Quaternion.Euler(
                -Input.GetAxis("Mouse Y") * resistanceForce,
                Input.GetAxis("Mouse X") * resistanceForce,
                0),
                resistanceSmoothing * Time.deltaTime);
    }
}
