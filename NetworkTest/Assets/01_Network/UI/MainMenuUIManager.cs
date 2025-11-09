using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainMenuUIManager : MonoBehaviour
{
    private OptionController optionController;

    public void OnMultiplayerButtonClicked()
    {
        // The new flow is to create a Steam lobby, which then handles connection and scene loading.
        NetworkManager.Instance.CreateSteamLobby();
    }

    public void OnSingleplayerButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartOffline();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null in MainMenuUIManager.OnSingleplayerButtonClicked()");
        }
    }

    public void OnSettingsButtonClicked()
    {
        if (!optionController)
            optionController = FindObjectOfType<OptionController>();
        optionController.Toggle();
    }

    public void OnExitButtonClicked()
    {
        Application.Quit();
    }

    private void OnEnable()
    {
        NetworkManager.OnConnected += HandleConnection;
        NetworkManager.OnConnectionFailed += HandleConnectionFailed;
        NetworkManager.OnDisconnected += HandleDisconnection;
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnConnected -= HandleConnection;
            NetworkManager.OnConnectionFailed -= HandleConnectionFailed;
            NetworkManager.OnDisconnected -= HandleDisconnection;
        }
    }

    #region Connection and UI Panel Management

    private void HandleConnection()
    {
        // This logic is now handled by NetworkManager's OnLobbyCreated/OnLobbyEnter callbacks.
        // This handler can be used for UI changes on the main menu if needed, e.g., showing a "Connected" status.
        Debug.Log("[MainMenuUIManager] NetworkManager connected.");
    }

    private void HandleConnectionFailed(string errorMessage)
    {
        Debug.Log("Connection failed. Please check the server and try again.");
        Debug.Log($"Connection Failed: {errorMessage}");
    }

    private void HandleDisconnection()
    {
        Debug.Log("Disconnected. You can try connecting again.");
        Debug.Log("Disconnected from server.");
    }

    #endregion
}