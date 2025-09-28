using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionInitialize : MonoBehaviour
{
    [SerializeField] private GameObject[] uiPanels;
    [SerializeField] private GameObject pausePanel;

    void Start()
    {
        foreach (var panel in uiPanels)
        {
            panel.gameObject.SetActive(false);
        }
        pausePanel.SetActive(false);
    }
}
