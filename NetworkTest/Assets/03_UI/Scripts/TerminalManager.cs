using UnityEngine;
using TMPro;
using System.Text;

public class TerminalManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text terminalOutput;
    
    [Header("Dependencies")]
    [SerializeField] private RoomUIManager roomUIManager;

    private readonly StringBuilder outputLog = new StringBuilder();
    private bool isTerminalActive = false;

    private void Start()
    {
        if (terminalPanel == null || inputField == null || terminalOutput == null || roomUIManager == null)
        {
            Debug.LogError("TerminalManager is not configured correctly. Please assign all fields in the inspector.");
            gameObject.SetActive(false);
            return;
        }

        inputField.onSubmit.AddListener(OnSubmitCommand);
        terminalPanel.SetActive(false); // Start with the terminal closed
    }

    private void Update()
    {
        // Check for Escape key to close the terminal
        if (isTerminalActive && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleTerminal();
        }
    }

    public void ToggleTerminal()
    {
        isTerminalActive = !isTerminalActive;
        
        // Pause the game to stop player movement and unlock cursor
        GameManager.Instance.SetPause(isTerminalActive);
        
        // Activate/deactivate the terminal UI
        terminalPanel.SetActive(isTerminalActive);

        if (isTerminalActive)
        {
            // Clear the log and show welcome message every time it's opened
            ExecuteClear();
            PrintWelcomeMessage();
            inputField.ActivateInputField();
        }
    }

    private void PrintWelcomeMessage()
    {
        AppendToLog("Welcome to the terminal.");
        AppendToLog("Type 'help' to see a list of available commands.");
        UpdateOutput();
    }

    private void OnSubmitCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        AppendToLog($"> {command}");
        ParseCommand(command.Trim().ToLower());
        
        inputField.text = "";
        inputField.ActivateInputField(); // Keep the input field focused
        UpdateOutput();
    }

    private void ParseCommand(string command)
    {
        string[] parts = command.Split(' ');
        string commandWord = parts[0];

        switch (commandWord)
        {
            case "help":
                ExecuteHelp();
                break;
            case "planets":
                ExecutePlanets();
                break;
            case "goto":
                ExecuteGoto(parts);
                break;
            case "launch":
                ExecuteLaunch();
                break;
            case "clear":
                ExecuteClear();
                break;
            case "exit":
                ToggleTerminal(); // Add exit command
                break;
            default:
                AppendToLog($"Unknown command: '{commandWord}'");
                break;
        }
    }

    private void ExecuteHelp()
    {
        AppendToLog("Available commands:");
        AppendToLog("  help - Shows this message.");
        AppendToLog("  planets - Lists available planets.");
        AppendToLog("  goto [planet-name] - Selects a planet for travel.");
        AppendToLog("  launch - Launches the game to the selected planet (host only).");
        AppendToLog("  clear - Clears the terminal screen.");
        AppendToLog("  exit - Closes the terminal.");
    }

    private void ExecutePlanets()
    {
        AppendToLog("Available planets:");
        AppendToLog("  - Planet_1");
        AppendToLog("  - Planet_2");
    }

    private void ExecuteGoto(string[] parts)
    {
        if (parts.Length < 2)
        {
            AppendToLog("Usage: goto [planet-name]");
            return;
        }

        string planetName = parts[1];
        int planetId = -1;

        if (planetName == "planet_1")
        {
            planetId = 0; // Assuming ID 0 for Planet_1
        }
        else if (planetName == "planet_2")
        {
            planetId = 1; // Assuming ID 1 for Planet_2
        }
        else
        {
            AppendToLog($"Unknown planet: '{planetName}'");
            return;
        }

        AppendToLog($"Selecting planet '{planetName}'...");
        roomUIManager.OnPlanetSelect(planetId);
    }

    private void ExecuteLaunch()
    {
        AppendToLog("Attempting to launch...");
        roomUIManager.OnLaunchGameClicked();
    }

    private void ExecuteClear()
    {
        outputLog.Clear();
    }

    private void AppendToLog(string line)
    {
        outputLog.AppendLine(line);
    }

    private void UpdateOutput()
    {
        terminalOutput.text = outputLog.ToString();
    }
}