using System.Collections;
using System.Collections.Generic;
using Michsky.MUIP;
using UnityEngine;

public class CustomizeManager : MonoBehaviour
{
    [SerializeField] private GameObject customizePanel;

    [SerializeField] private HorizontalSelector isMaleSelector;
    [SerializeField] private HorizontalSelector headSelector;
    [SerializeField] private HorizontalSelector bodySelector;
    [SerializeField] private HorizontalSelector acce1Selector;
    [SerializeField] private HorizontalSelector acce2Selector;

    private PlayerCustomizer customizer;

    void Start()
    {
        customizer = PlayerCustomizer.Instance;
    }

    public void TogglePanel()
    {
        customizePanel.SetActive(!customizePanel.activeSelf);
    }

    public void ChangeMale(int isMale)
    {
        customizer.IsMale = isMale < 0 ? true : false;
    }

    public void ChangeHead(int head)
    {
        customizer.Test.head = head;
    }

    public void ChangeBody(int body)
    {
        customizer.Test.body = body;
    }

    public void ChangeAcc1(int acc1)
    {
        customizer.Test.acce1 = acc1;
    }

    public void ChangeAcc2(int acc2)
    {
        customizer.Test.acce2 = acc2;
    }
}
