using Michsky.MUIP;
using UnityEngine;
using System;

public class UIManager : MonoBehaviour
{
    private static UIManager m_Instance;
    public static UIManager Instance {get{return m_Instance;}}
    [Header("Menu UI Panels")]
    [SerializeField] private GameObject menuUIParent;
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject characterMakeUI;
    [SerializeField] private ModalWindowManager startGameModal;
    [SerializeField] private ModalWindowManager exitGameModal;

    [Header("Game UI Panels")]
    [SerializeField] private GameObject gameUIParent;
    [SerializeField] private GameObject HUD_UI;
    [SerializeField] private GameObject mobile_UI;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameInfo pause_GameInfo;
    [SerializeField] private ModalWindowManager inEndGameModal;
    [SerializeField] private ModalWindowManager inExitGameModal;

    [Header("Option UI Panels")]
    [SerializeField] private GameObject optionUI;
    [SerializeField] private GameObject[] optionUIs;
    [SerializeField] private ModalWindowManager shortcutModal;

    private GameState uiState;
    private PlayerInputs input;

    private void Awake()
    {
        if (Instance == null)
        {
            m_Instance = this;
            DontDestroyOnLoad(this);
        }
        else
            Destroy(gameObject);

        GameManager.Instance.TryGetComponent<PlayerInputs>(out input);
    }

    private void Update()
    {
        if (uiState != GameManager.Instance.CurrentState)
        {   // Panel 초기화
            uiState = GameManager.Instance.CurrentState;

            if (GameState.Title == uiState)
            {
                menuUIParent.SetActive(true);
                gameUIParent.SetActive(false);
                InitMenuUI();
            }
            else
            {
                menuUIParent.SetActive(false);
                gameUIParent.SetActive(true);
                InitGameUI();
            }
        }

        Inputs();
    }

    private void InitGameUI()
    {
        mobile_UI.SetActive(true);
        inventoryUI.SetActive(false);
        pauseUI.SetActive(false);
        optionUI.SetActive(false);
        foreach (GameObject obj in optionUIs)
        {
            obj.SetActive(false);
        }
    }

    private void InitMenuUI()
    {
        titleUI.SetActive(true);
        characterMakeUI.SetActive(false);
        optionUI.SetActive(false);
        foreach (GameObject obj in optionUIs)
        {
            obj.SetActive(false);
        }
    }

    private void Inputs()
    {
        switch (GameManager.Instance.CurrentState)
        {
            case GameState.Title:
                if (input.GetEscape())
                {
                    if (shortcutModal.isOn)
                        shortcutModal.Close();
                    else if (-1 != IsOptionEnable())
                        optionUIs[IsOptionEnable()].SetActive(false);
                    else if (optionUI.activeSelf)
                        optionUI.SetActive(false);
                    else if (startGameModal.isOn)
                        startGameModal.Close();
                    else if (characterMakeUI.activeSelf)
                    {
                        characterMakeUI.SetActive(false);
                        titleUI.SetActive(true);
                    }
                    else if (exitGameModal.isOn)
                        exitGameModal.Close();
                    else
                        exitGameModal.Open();
                }
                break;
            case GameState.Game:
                if (input.GetInventory() && !pauseUI.activeSelf)
                    InventoryToggle();
                if (input.GetEscape())
                {
                    if (shortcutModal.isOn)
                        shortcutModal.Close();
                    else if (-1 != IsOptionEnable())
                        optionUIs[IsOptionEnable()].SetActive(false);
                    else if (optionUI.activeSelf)
                        optionUI.SetActive(false);
                    else if (inExitGameModal.isOn)
                        inExitGameModal.Close();
                    else if (inEndGameModal.isOn)
                        inEndGameModal.Close();
                    else if (pauseUI.activeSelf)
                        pauseUI.SetActive(false);
                    else if (inventoryUI.activeSelf)
                        inventoryUI.SetActive(false);
                    else
                        pauseUI.SetActive(true);
                }
                break;
        }
    }

    public void InventoryToggle()
    {
        if (inventoryUI.activeSelf)
            inventoryUI.SetActive(false);
        else
            inventoryUI.SetActive(true);
    }

    private int IsOptionEnable()
    {
        for (int i = 0; i < optionUIs.Length; ++i)
        {
            if (optionUIs[i].activeSelf)
                return i;
        }
        return -1;
    }
}