using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameUIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text ammoCountText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text interactText;

    private WeaponController weaponController;

    void Start()
    {
        // ONLY AT TEST MODE
        // weaponController = FindAnyObjectByType<WeaponController>().GetComponent<WeaponController>();
    }

    public void SetInit(WeaponController weaponController)
    {
        Debug.Log($"{weaponController}");
        this.weaponController = weaponController;
    }

    public void SetHealthValue(float value)
    {
        if (!healthText)
            return;
        healthText.text = value.ToString();
    }

    public void SetInteractText(string _text)
    {
        if (interactText == null) return;

        if (string.IsNullOrEmpty(_text))
        {
            interactText.gameObject.SetActive(false);
        }
        else
        {
            interactText.text = _text;
            interactText.gameObject.SetActive(true);
        }
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
