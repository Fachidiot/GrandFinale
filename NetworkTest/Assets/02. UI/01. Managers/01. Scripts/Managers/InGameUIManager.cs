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
    [SerializeField] private TMP_Text cashText;

    private WeaponController weaponController;
    private PlayerStats playerStats;

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
        if (playerStats != null) playerStats.OnStatsChanged -= UpdateCashText;
    }

    private void HandleDisconnection()
    {
        Debug.Log($"[InGameUIManager] Disconnected. Returning to main menu.");
        SceneManager.LoadScene(GameManager.Instance.GameSettings.mainmenuScene);
    }

    public void SetInit(WeaponController weaponController, PlayerStats playerStats)
    {
        Debug.Log($"InGameUIManager received WeaponController: {weaponController}");
        this.weaponController = weaponController;
        Debug.Log($"InGameUIManager received PlayerStats: {playerStats}");
        this.playerStats = playerStats;

        if (playerStats != null)
        {
            // 돈이 바뀌면 UI도 바뀌도록 이벤트 연결
            playerStats.OnStatsChanged += UpdateCashText;
            UpdateCashText();
        }
        else
        {
            Debug.LogError("PlayerStats couldnt find");
        }
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

    public void UpdateCashText()
    {
        if (cashText == null || playerStats == null) return;
        cashText.text = $"{playerStats.CurrentCurrency:N0} G";
    }

    // Update is called once per frame
    void Update()
    {
         if (weaponController == null)
        {
            gameObject.SetActive(false);
        }
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
        WeaponUI();

        if (playerStats == null)
        {
            if (healthText != null && healthText.enabled)
            {
                healthText.enabled = false;
            }
            return;
        }
        if (!healthText.enabled)
        {
            healthText.enabled = true;
        }
        PlayerStatsUI();
    }

    private void WeaponUI()
    {
        IWeapon currentWeapon = weaponController.GETCurrentWeapon;
        if (currentWeapon == null)
        {
            ammoCountText.text = "-";
            return;
        }

        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            ammoCountText.text = rangedWeapon.CurrentAmmo.ToString();
        }
        else
        {
            ammoCountText.text = "-"; // Melee weapons or other non-ranged
        }
    }

    private void PlayerStatsUI()
    {
        if (0 >= playerStats.CurrentHealth)
        {
            healthText.text = "0";
        }
        else
        {
            healthText.text = playerStats.CurrentHealth.ToString();
        }
    }
}
