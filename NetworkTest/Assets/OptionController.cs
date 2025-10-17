using UnityEngine;

public class OptionController : MonoBehaviour
{
    [SerializeField] private GameObject optionPanel;
    public bool OptionOn { get; private set; }

    [SerializeField] private GameObject[] uiPanels;

    void Start()
    {
        foreach (var panel in uiPanels)
        {
            panel.gameObject.SetActive(false);
        }
        optionPanel.SetActive(false);

        OptionOn = false;
        Toggle();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
            else
                optionPanel.SetActive(true);
            OptionOn = optionPanel.activeSelf;

            GameManager.Instance.SetPause(OptionOn);
        }
    }

    void Toggle()
    {
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
}
