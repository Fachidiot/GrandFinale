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

    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();

        if (playerStats != null)
        {
            // 돈이 바뀌면 UI도 바뀌도록 이벤트 연결
            playerStats.OnStatsChanged += UpdateCashText;
            UpdateCashText();
        }
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
}
