using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainMenuUIManager : MonoBehaviour
{
    private Dictionary<string, Action<string>> messageHandlers;
    private string ip = "127.0.0.1";

    private void Awake()
    {
        InitializeMessageHandlers();
    }

    public void OnMultiplayerButtonClicked()
    {
        Debug.Log("Multiplayer button clicked. Starting host...");
        NetworkManager.Instance.StartHost(8080);
    }

    public void OnSingleplayerButtonClicked()
    {
        Debug.Log("Singleplayer button clicked. Starting offline mode...");
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
        Debug.Log("Settings button clicked. Opening settings...");
        // 여기에 설정 UI를 여는 코드를 추가합니다.
    }

    private void OnEnable()
    {
        NetworkManager.OnConnected += HandleConnection;
        NetworkManager.OnConnectionFailed += HandleConnectionFailed;
        NetworkManager.OnDisconnected += HandleDisconnection;
        NetworkManager.OnMessageReceived += HandleServerMessage;
    }

    private void OnDisable()
    {
        NetworkManager.OnConnected -= HandleConnection;
        NetworkManager.OnConnectionFailed -= HandleConnectionFailed;
        NetworkManager.OnDisconnected -= HandleDisconnection;
        NetworkManager.OnMessageReceived -= HandleServerMessage;
    }

    private void InitializeMessageHandlers()
    {
        messageHandlers = new Dictionary<string, Action<string>>
        {

        };
    }

    private void HandleServerMessage(string jsonMsg)
    {
        JObject response = JObject.Parse(jsonMsg);
        string type = response["type"]?.ToString();

        if (messageHandlers.TryGetValue(type, out var handler))
        {
            handler(jsonMsg);
        }
    }

    #region Connection and UI Panel Management

    private void HandleConnection()
    {
        if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            // 호스트로 시작했을 때의 로직 (예: 로비 씬으로 바로 이동)
            SceneManager.LoadScene("RoomScene");
        }
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
