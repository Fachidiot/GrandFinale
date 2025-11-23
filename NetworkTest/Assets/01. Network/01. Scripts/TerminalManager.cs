using UnityEngine;
using TMPro;
using System.Text;
using Cinemachine;
using Newtonsoft.Json.Linq;
using Steamworks;
using System.Collections;

public class TerminalManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text terminalOutput;

    // [Header("Dependencies")] - No longer serialized
    // [SerializeField] private ServerRoomManager serverRoomManager;

    [Header("Interaction Settings")]
    [SerializeField] private Transform playerStandPosition;
    [SerializeField] private CinemachineVirtualCamera terminalVirtualCamera;

    private readonly StringBuilder outputLog = new StringBuilder();
    private bool isTerminalActive = false;
    public bool IsTerminalActive { get { return isTerminalActive; } }

    private IPlayerControllable activePlayer;
    private CameraSwitcher activeCameraSwitcher;

    // --- Lazy-Loading Implementation ---
    private ServerRoomManager _serverRoomManager;
    private ServerRoomManager ServerRoomManager
    {
        get
        {
            if (_serverRoomManager == null)
            {
                _serverRoomManager = ServerRoomManager.Instance;
            }
            return _serverRoomManager;
        }
    }
    // ------------------------------------

    private void Start()
    {
        if (terminalPanel == null || inputField == null || terminalOutput == null || playerStandPosition == null || terminalVirtualCamera == null)
        {
            Debug.LogError("TerminalManager is not configured correctly. Please assign all UI and camera fields in the inspector.");
            gameObject.SetActive(false);
            return;
        }

        inputField.onSubmit.AddListener(OnSubmitCommand);
        terminalPanel.SetActive(false); // Start with the terminal closed
    }


    public void ToggleTerminal()
    {
        isTerminalActive = !isTerminalActive;

        GameManager.Instance.SetPause(isTerminalActive);
        terminalPanel.SetActive(isTerminalActive);

        if (isTerminalActive)
        {
            // Clear the interact text when the terminal is activated
            UIEvents.InteractableFocusChanged("");

            activePlayer = PlayerManager.Instance.LocalPlayer;
            if (activePlayer == null || activePlayer.gameObject == null)
            {
                Debug.LogError("Terminal cannot be activated: Local player not found.");
                isTerminalActive = false; // Revert state
                GameManager.Instance.SetPause(false);
                terminalPanel.SetActive(false);
                return;
            }

            activePlayer.GetComponentInChildren<WeaponController>().EquipWeapon(5);
            activeCameraSwitcher = activePlayer.GetComponentInChildren<CameraSwitcher>();
            if (activeCameraSwitcher != null)
            {
                activeCameraSwitcher.enabled = false;
            }

            activePlayer.transform.position = playerStandPosition.position;

            Animator playerAnimator = activePlayer.GetComponentInChildren<Animator>();
            CharacterMove playerCharacterMove = activePlayer.GetComponent<CharacterMove>();
            if (playerAnimator != null && playerCharacterMove != null)
            {
                playerAnimator.SetFloat(playerCharacterMove.horizontalInputID, 0);
                playerAnimator.SetFloat(playerCharacterMove.verticalInputID, 0);
                playerAnimator.SetBool(playerCharacterMove.sprintID, false);
                playerAnimator.SetBool(playerCharacterMove.crouchID, false);
                playerAnimator.SetBool(playerCharacterMove.rollID, false);
            }

            terminalVirtualCamera.Priority = 10;

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
            activePlayer = null;
            activeCameraSwitcher = null;
        }

        //  임시방편 마우스 focus해제되는 버그
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void PrintWelcomeMessage()
    {
        AppendToLog("Welcome to the terminal.");
        AppendToLog("Type 'help' to see a list of available commands.");
        UpdateOutput();
    }

    private void OnSubmitCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;
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
            case "help": ExecuteHelp(); break;
            case "planets": ExecutePlanets(); break;
            case "goto": ExecuteGoto(parts); break;
            case "clear": ExecuteClear(); break;
            case "exit": ToggleTerminal(); break;
            default: AppendToLog($"Unknown command: '{commandWord}'"); break;
        }
    }

    private void ExecuteHelp()
    {
        AppendToLog("Available commands:");
        AppendToLog("  help - Shows this message.");
        AppendToLog("  planets - Lists available planets.");
        AppendToLog("  goto [planet-name] - Selects a planet for travel.");
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
            case "planet_1": case "Planet_1": planetId = 1; break;
            case "planet_2": case "Planet_2": planetId = 2; break;
            default: AppendToLog($"Unknown planet: '{planetName}'"); return;
        }

        AppendToLog($"Proposing planet '{planetName}'...");
        ProposePlanet(planetId);
    }

    private void ProposePlanet(int planetId)
    {
        if (NetworkManager.Instance == null || ServerRoomManager == null)
        {
            AppendToLog("Error: Network systems not available.");
            return;
        }

        if (NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            ServerRoomManager.SelectPlanet(planetId);
            AppendToLog("Planet selected as host.");
        }
        else // Client
        {
            JObject message = new JObject { { "type", "propose_planet" }, { "planet_id", planetId } };
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