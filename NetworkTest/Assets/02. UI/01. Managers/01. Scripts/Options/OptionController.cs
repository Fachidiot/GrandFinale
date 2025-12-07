using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionController : MonoBehaviour
{
    [SerializeField] private GameObject optionPanel;
    public bool OptionOn { get; private set; }

    [SerializeField] private GameObject[] uiPanels;
    private TerminalManager terminalManager;
    private NpcInputSensor npcInputSensor;

    private PlayerInputs playerInputs;
    private IAction action;

    void Start()
    {
        foreach (var panel in uiPanels)
        {
            panel.gameObject.SetActive(false);
        }
        optionPanel.SetActive(false);
        playerInputs = GameManager.Instance.GetComponent<PlayerInputs>();
    }

    void Update()
    {
        if (playerInputs.GetEscape())
        {
            TerminalCheck();
            ActionCheck();
            ShopCheck();

            // if (shortcutModal.isOn)
            //     shortcutModal.Close();
            if (-1 != IsOptionEnable())
                uiPanels[IsOptionEnable()].SetActive(false);
            // else if (inExitGameModal.isOn)
            //     inExitGameModal.Close();
            // else if (inEndGameModal.isOn)
            //     inEndGameModal.Close();
            else if (optionPanel.activeSelf)
            {
                optionPanel.SetActive(false);
            }
            else if (npcInputSensor && npcInputSensor.IsActive)
            {
                npcInputSensor.ToggleNPC(null);
            }
            else if (terminalManager && terminalManager.IsTerminalActive)
            {
                terminalManager.ToggleTerminal();
            }
            else if (null != action && action.Value)
            {
                action.ReleaseAction();
            }
            else
            {
                optionPanel.SetActive(true);
            }
            OptionOn = optionPanel.activeSelf;

            GameManager.Instance.SetPause(!OptionOn && !GameManager.Instance.isMainMenu);
        }
    }

    private void TerminalCheck()
    {
        if (!terminalManager)
            terminalManager = FindObjectOfType<TerminalManager>();
    }

    private void ShopCheck()
    {
        if (!npcInputSensor)
            npcInputSensor = playerInputs.GetNPCSensor;
    }

    private void ActionCheck()
    {
        action = playerInputs.GetActionState();
    }

    public void Toggle()
    {
        OptionOn = !OptionOn;
        optionPanel.SetActive(OptionOn);
    }

    private int IsOptionEnable()
    {
        for (int i = 0; i < uiPanels.Length; ++i)
        {
            if (uiPanels[i].activeSelf)
                return i;
        }
        return -1;
    }

    public void OnLeaveRoomNetworkClicked()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.Disconnect();
        }

        if (NetworkManager.Instance.Mode == NetworkMode.SinglePlayer)
        {
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.ClearAllNetworkEntities();
        }

        SceneManager.LoadScene(GameManager.Instance.GameSettings.mainmenuScene);
        GameManager.Instance.isMainMenu = true;
    }
}
