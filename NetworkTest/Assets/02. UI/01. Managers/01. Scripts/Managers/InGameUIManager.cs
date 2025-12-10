using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameUIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text ammoCountText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text interactText;
    // [SerializeField] private TMP_Text cashText;

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
#if UNITY_EDITOR
        weaponController = GameObject.FindObjectOfType<WeaponController>();
#endif
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
        // if (playerStats != null) playerStats.OnStatsChanged -= UpdateCashText;
    }

    private void HandleDisconnection()
    {
        Debug.Log($"[InGameUIManager] Disconnected. Returning to main menu.");
        SceneManager.LoadScene(GameManager.Instance.GameSettings.mainmenuScene);

        GameManager.Instance.isMainMenu = true;
        Destroy(NetworkManager.Instance.GetComponent<ServerRoomManager>());
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
            // playerStats.OnStatsChanged += UpdateCashText;
            // UpdateCashText();
        }
        else
        {
            Debug.LogError("PlayerStats couldnt find");
        }
    }

    public void SetHealthValue(float value)
    {
        if (!healthSlider)
            return;
        healthSlider.value = (value / 100);
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

    // public void UpdateCashText()
    // {
    //     if (cashText == null || playerStats == null) return;
    //     cashText.text = $"{playerStats.CurrentCurrency:N0} G";
    // }

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
            if (healthSlider != null && healthSlider.enabled)
            {
                healthSlider.enabled = false;
            }
            return;
        }
        if (!healthSlider.enabled)
        {
            healthSlider.enabled = true;
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
            healthSlider.value = 0;
        }
        else
        {
            healthSlider.value = (playerStats.CurrentHealth / 100);
        }
    }
}
