using UnityEngine;
using TMPro;
using System.Text;
using Cinemachine;
using Newtonsoft.Json.Linq;
using Steamworks;

public class TerminalManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text terminalOutput;

    [Header("Dependencies")]
    [SerializeField] private ServerRoomManager serverRoomManager;

    [Header("Interaction Settings")]
    [SerializeField] private Transform playerStandPosition;

    [SerializeField] private CinemachineVirtualCamera terminalVirtualCamera;

    private readonly StringBuilder outputLog = new StringBuilder();
    private bool isTerminalActive = false;
    public bool IsTerminalActive { get { return isTerminalActive; } }

    private PlayerInputs playerInputs;
    private GameObject activePlayer;
    private CameraSwitcher activeCameraSwitcher;

    private void Start()
    {
        // serverRoomManager is now assigned at runtime
        if (terminalPanel == null || inputField == null || terminalOutput == null || playerStandPosition == null || terminalVirtualCamera == null)
        {
            Debug.LogError("TerminalManager is not configured correctly. Please assign all UI and camera fields in the inspector.");
            gameObject.SetActive(false);
            return;
        }

        // Get PlayerInputs from GameManager
        playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();

        // Find the persistent ServerRoomManager instance
        serverRoomManager = ServerRoomManager.Instance;
        if (serverRoomManager == null)
        {
            Debug.LogError("[TerminalManager] ServerRoomManager.Instance not found! The terminal will not function correctly.");
            gameObject.SetActive(false);
            return;
        }

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

            // Find CameraSwitcher disable it
            activeCameraSwitcher = activePlayer.GetComponentInChildren<CameraSwitcher>();
            if (activeCameraSwitcher != null)
            {
                activeCameraSwitcher.enabled = false;
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

            terminalVirtualCamera.Priority = 10;

            // Setup UI
            ExecuteClear();
            PrintWelcomeMessage();
            inputField.ActivateInputField();
        }
        else
        {
            terminalVirtualCamera.Priority = 0;

            if (activeCameraSwitcher != null)
            {
                activeCameraSwitcher.enabled = true;
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
            // case "launch":
            //     ExecuteLaunch();
            //     break;
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
        // AppendToLog("  launch - Launches the game to the selected planet (host only).");
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

        switch (planetName)
        {
            case "planet_1":
            case "Planet_1":
                planetId = 1;
                break;
            case "planet_2":
            case "Planet_2":
                planetId = 2;
                break;
            default:
                AppendToLog($"Unknown planet: '{planetName}'");
                return; // Return early if planet is unknown
        }

        AppendToLog($"Proposing planet '{planetName}'...");
        ProposePlanet(planetId);
    }

    private void ProposePlanet(int planetId)
    {
        if (NetworkManager.Instance == null || serverRoomManager == null)
        {
            AppendToLog("Error: Network systems not available.");
            return;
        }

        if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            serverRoomManager.SelectPlanet(planetId);
            AppendToLog("Planet selected as host.");
        }
        else // Client
        {
            JObject message = new JObject
            {
                { "type", "propose_planet" },
                { "planet_id", planetId }
            };

            CSteamID hostId = NetworkManager.Instance.LobbyHostID;
            if (hostId.IsValid())
            {
                NetworkManager.Instance.SendJsonMessage(hostId, message);
                AppendToLog($"Sent proposal for planet ID {planetId} to host.");
            }
            else
            {
                AppendToLog("Error: Could not send proposal, invalid host ID.");
            }
        }
    }

    private void ExecuteLaunch()
    {
        AppendToLog("Attempting to launch...");
        // serverRoomManager.OnLaunchGameClicked();
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
