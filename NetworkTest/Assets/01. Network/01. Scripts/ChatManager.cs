using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Steamworks;
using Newtonsoft.Json.Linq;

public class ChatManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TextMeshProUGUI chatLogText;
    [SerializeField] private int maxMessages = 20;

    private PlayerInputs playerInputs;
    private bool isChatOpen = false;
    private readonly List<string> chatMessages = new List<string>();

    #region Unity Lifecycle

    private void Start()
    {
        playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();
        
        // Ensure components are assigned
        if (chatPanel == null || chatInputField == null || chatLogText == null)
        {
            Debug.LogError("[ChatManager] UI components are not assigned in the inspector!");
            enabled = false;
            return;
        }

        // Add a listener to the input field's onSubmit event (fires on Enter key)
        chatInputField.onSubmit.AddListener((text) => SendMessage());
        
        // Start with chat closed
        chatPanel.SetActive(false);
        chatLogText.text = "";
    }

    private void OnEnable()
    {
        NetworkManager.OnJsonMessageReceived += OnChatMessageReceived;
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnJsonMessageReceived -= OnChatMessageReceived;
        }
    }

    private void Update()
    {
        // Use the chat open key to toggle the chat window
        if (playerInputs.GetChatOpen())
        {
            ToggleChat();
        }
    }

    #endregion

    #region Chat Logic

    private void ToggleChat()
    {
        isChatOpen = !isChatOpen;
        chatPanel.SetActive(isChatOpen);
        GameManager.Instance.SetPause(isChatOpen);

        if (isChatOpen)
        {
            chatInputField.ActivateInputField();
        }
        else
        {
            chatInputField.DeactivateInputField();
        }
    }

    /// <summary>
    /// Called when the user presses Enter in the chat input field.
    /// </summary>
    public void SendMessage()
    {
        string message = chatInputField.text;
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Construct the JSON message
        JObject chatMsg = new JObject
        {
            { "type", "chat_message" },
            { "sender", CustomSteamManager.Instance.PlayerName },
            { "text", message }
        };

        // Broadcast it via NetworkManager
        NetworkManager.Instance.BroadcastJsonMessage(chatMsg);

        // Clear the input field and keep it focused for the next message
        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    /// <summary>
    /// Called when a chat message is received from the network.
    /// </summary>
    private void OnChatMessageReceived(CSteamID sender, string jsonMsg)
    {
        JObject msg = JObject.Parse(jsonMsg);
        string type = msg["type"]?.ToString();

        if (type == "chat_message")
        {
            string senderName = msg["sender"]?.ToString() ?? "Unknown";
            string text = msg["text"]?.ToString() ?? "";
            AddMessageToLog(senderName, text);
        }
    }

    /// <summary>
    /// Adds a new message to the chat log UI.
    /// </summary>
    private void AddMessageToLog(string sender, string message)
    {
        if (chatLogText == null) return;

        string formattedMessage = $"<b>{sender}:</b> {message}";
        chatMessages.Add(formattedMessage);

        // Ensure the log doesn't exceed the max message count
        if (chatMessages.Count > maxMessages)
        {
            chatMessages.RemoveAt(0);
        }

        // Update the UI text
        chatLogText.text = string.Join("\n", chatMessages);
    }

    #endregion
}
