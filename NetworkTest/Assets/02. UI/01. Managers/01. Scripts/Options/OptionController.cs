using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionController : MonoBehaviour
{
    [SerializeField] private GameObject optionPanel;
    public bool OptionOn { get; private set; }

    [SerializeField] private GameObject[] uiPanels;
    private TerminalManager terminalManager;

    void Start()
    {
        foreach (var panel in uiPanels)
        {
            panel.gameObject.SetActive(false);
        }
        optionPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TerminalCheck();
            // if (shortcutModal.isOn)
            //     shortcutModal.Close();
            if (-1 != IsOptionEnable())
                uiPanels[IsOptionEnable()].SetActive(false);
            // else if (inExitGameModal.isOn)
            //     inExitGameModal.Close();
            // else if (inEndGameModal.isOn)
            //     inEndGameModal.Close();
            else if (optionPanel.activeSelf)
                optionPanel.SetActive(false);
            // else if (inventoryUI.activeSelf)
            //     inventoryUI.SetActive(false);
            else if (terminalManager && terminalManager.IsTerminalActive)
            {
                terminalManager.ToggleTerminal();

                //  임시방편 마우스 focus해제되는 버그
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
                optionPanel.SetActive(true);
            OptionOn = optionPanel.activeSelf;

            GameManager.Instance.SetPause(OptionOn);
        }
    }

    private void TerminalCheck()
    {
        if (!terminalManager)
            terminalManager = FindObjectOfType<TerminalManager>();
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
    }
}
