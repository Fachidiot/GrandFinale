using UnityEngine;

public class OptionToggle : MonoBehaviour
{
    [SerializeField] private GameObject optionPanel;
    public bool OptionOn { get; private set; }

    void Start()
    {
        OptionOn = false;
        Toggle();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OptionOn = !OptionOn;
            Toggle();
        }
    }

    void Toggle()
    {
        optionPanel.SetActive(OptionOn);
    }
}
