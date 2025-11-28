using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Cinemachine;

public class MainMenuUIManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera menuCamera;
    [SerializeField] private CinemachineVirtualCamera roomCamera;

    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject roomPanel;

    [SerializeField] private GameObject localCustomView;
    [SerializeField] private GameObject clientCustomView;

    private OptionController optionController;

    public void OnRoomButtonClicked()
    {
        menuCamera.Priority = 0;
        roomCamera.Priority = 1;
        menuPanel.SetActive(false);
        roomPanel.SetActive(true);
    }

    public void OnMenuButtonClicked()
    {
        menuCamera.Priority = 1;
        roomCamera.Priority = 0;
        menuPanel.SetActive(true);
        roomPanel.SetActive(false);
    }

    public void OnMultiplayerButtonClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetPause(false);
        // The new flow is to create a Steam lobby, which then handles connection and scene loading.
        NetworkManager.Instance.CreateSteamLobby();
    }

    public void OnSingleplayerButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPause(false);
            GameManager.Instance.StartOffline();
            NetworkManager.Instance.SetMode(NetworkMode.SinglePlayer);
            ServerRoomManager.Instance.Initialize(NetworkMode.SinglePlayer);
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