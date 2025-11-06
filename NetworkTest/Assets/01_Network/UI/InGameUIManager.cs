using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InGameUIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text ammoCountText;
    [SerializeField] private TMP_Text healthText;

    private WeaponController weaponController;

    void Start()
    {
        // ONLY AT TEST MODE
        // weaponController = FindAnyObjectByType<WeaponController>().GetComponent<WeaponController>();
    }

    public void SetInit(WeaponController weaponController)
    {
        this.weaponController = weaponController;
    }

    public void SetHealthValue(float value)
    {
        if (!healthText)
            return;
        healthText.text = value.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (weaponController == null)
            return;
        if (weaponController.GETCurrentWeapon == null)
            return;

        ammoCountText.text = weaponController.GETCurrentWeapon.CurrentAmmo.ToString();
    }
}
