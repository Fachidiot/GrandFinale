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

    void Awake()
    {
        // Subscribe to UI events
        UIEvents.OnInteractableFocusChanged += SetInteractText;
        UIEvents.OnPlayerInitialized += SetInit;
        NetworkManager.OnDisconnected += HandleDisconnection;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        UIEvents.OnInteractableFocusChanged -= SetInteractText;
        UIEvents.OnPlayerInitialized -= SetInit;
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnDisconnected -= HandleDisconnection;
        }
    }

    private void HandleDisconnection()
    {
        Debug.Log($"[InGameUIManager] Disconnected. Returning to main menu.");
        SceneManager.LoadScene(GameManager.Instance.GameSettings.mainmenuScene);
    }

    public void SetInit(WeaponController weaponController)
    {
        Debug.Log($"InGameUIManager received WeaponController: {weaponController}");
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
        {
            if (ammoCountText != null && ammoCountText.enabled)
            {
                ammoCountText.enabled = false;
            }
            return;
        }

        if (!ammoCountText.enabled)
        {
            ammoCountText.enabled = true;
        }

        if (weaponController.GETCurrentWeapon == null)
        {
            ammoCountText.text = "-";
            return;
        }

        RangedWeapon currentRangedWeapon = weaponController.GETCurrentWeapon as RangedWeapon;
        if (currentRangedWeapon != null)
        {
            ammoCountText.text = currentRangedWeapon.CurrentAmmo.ToString();
        }
        else
        {
            ammoCountText.text = "-"; // Display nothing for melee or unarmed
        }
    }
}
