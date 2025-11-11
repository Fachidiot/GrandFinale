using UnityEngine;
using TMPro;
using System.Text;
using Cinemachine;

public class TerminalManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text terminalOutput;

    [Header("Dependencies")]
    [SerializeField] private RoomUIManager roomUIManager;

    [Header("Interaction Settings")]
    [SerializeField] private Transform playerStandPosition;

    [SerializeField] private CinemachineVirtualCamera cinemachineVirtualCamera;

    private readonly StringBuilder outputLog = new StringBuilder();
    private bool isTerminalActive = false;
    public bool IsTerminalActive { get { return isTerminalActive; } }

    private PlayerInputs playerInputs;
    private GameObject activePlayer;
    private CameraSwitcher activeCameraSwitcher;

    private void Start()
    {
        if (terminalPanel == null || inputField == null || terminalOutput == null || roomUIManager == null || playerStandPosition == null)
        {
            Debug.LogError("TerminalManager is not configured correctly. Please assign all fields in the inspector.");
            gameObject.SetActive(false);
            return;
        }

        // Get PlayerInputs from GameManager
        playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();

        inputField.onSubmit.AddListener(OnSubmitCommand);
        terminalPanel.SetActive(false); // Start with the terminal closed
    }

    private void OnTriggerStay(Collider other)
    {
        if (isTerminalActive) return; // Don't do anything if terminal is already open

        if (other.CompareTag("Player"))
        {
            // Check if it's the local player by checking if the InputHandler is enabled
            InputHandler handler = other.GetComponentInChildren<InputHandler>();
            if (handler != null && handler.enabled)
            {
                if (playerInputs.GetInteract())
                {
                    ToggleTerminal(other.gameObject);
                }
            }
        }
    }

    public void ToggleTerminal(GameObject playerObject)
    {
        isTerminalActive = !isTerminalActive;

        GameManager.Instance.SetPause(isTerminalActive);
        terminalPanel.SetActive(isTerminalActive);

        if (isTerminalActive)
        {
            // --- Activating Terminal ---
            activePlayer = playerObject;
            if (activePlayer == null)
            {
                Debug.LogError("Terminal activated without a valid player object!");
                // Deactivate again as a fallback
                ToggleTerminal(null);
                return;
            }

            // Weapon Unarmed.
            activePlayer.GetComponentInChildren<WeaponController>().ToChange(5);

            // Find CameraSwitcher and toggle to FPV
            activeCameraSwitcher = activePlayer.GetComponentInChildren<CameraSwitcher>();
            if (activeCameraSwitcher != null && !activeCameraSwitcher.IsFirstPersonView)
            {
                activeCameraSwitcher.ViewChange(); // Assuming this toggles into FPV
            }

            // Move player to stand position
            activePlayer.transform.position = playerStandPosition.position;

            // Stop player animations
            Animator playerAnimator = activePlayer.GetComponentInChildren<Animator>();
            CharacterMove playerCharacterMove = activePlayer.GetComponent<CharacterMove>();
            if (playerAnimator != null && playerCharacterMove != null)
            {
                playerAnimator.SetFloat(playerCharacterMove.horizontalInputID, 0);
                playerAnimator.SetFloat(playerCharacterMove.verticalInputID, 0);
                playerAnimator.SetBool(playerCharacterMove.sprintID, false);
                playerAnimator.SetBool(playerCharacterMove.crouchID, false); // Assuming crouchID exists
                playerAnimator.SetBool(playerCharacterMove.rollID, false);   // Assuming rollID exists
            }

            cinemachineVirtualCamera.Priority = 10;

            // Setup UI
            ExecuteClear();
            PrintWelcomeMessage();
            inputField.ActivateInputField();
        }
        else
        {
            cinemachineVirtualCamera.Priority = 0;

            if (activeCameraSwitcher != null)
            {
                activeCameraSwitcher.ViewChange(); // Toggle back to previous view
            }

            // Clear references
            activePlayer = null;
            activeCameraSwitcher = null;
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
        inputField.ActivateInputField();
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
                ToggleTerminal(null);
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
            planetId = 0;
        }
        else if (planetName == "planet_2")
        {
            planetId = 1;
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
