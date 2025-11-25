using System.Collections;
using System.Collections.Generic;
using Michsky.MUIP;
using UnityEngine;

public class CustomizeManager : MonoBehaviour
{
    [SerializeField] private GameObject customizePanel;
    [SerializeField] private ModelCustom fModel;
    [SerializeField] private ModelCustom mModel;

    [SerializeField] private HorizontalSelector isMaleSelector;
    [SerializeField] private HorizontalSelector headSelector;
    [SerializeField] private HorizontalSelector bodySelector;
    [SerializeField] private HorizontalSelector acce1Selector;
    [SerializeField] private HorizontalSelector acce2Selector;

    private PlayerCustomizer customizer;
    private bool isMale;

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
        this.isMale = isMale < 0 ? true : false;

        customizer.IsMale = this.isMale;
        if (this.isMale)
        {
            mModel.transform.parent.gameObject.SetActive(true);
            fModel.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            mModel.transform.parent.gameObject.SetActive(false);
            fModel.transform.parent.gameObject.SetActive(true);
        }
    }

    public void ChangeHead(int head)
    {
        customizer.Test.head = head;

        if (isMale)
            mModel.ChangeHead(head);
        else
            fModel.ChangeHead(head);
    }

    public void ChangeBody(int body)
    {
        customizer.Test.body = body;

        if (isMale)
            mModel.ChangeHead(body);
        else
            fModel.ChangeHead(body);
    }

    public void ChangeAcc1(int acc1)
    {
        customizer.Test.acce1 = acc1;

        if (isMale)
            mModel.ChangeHead(acc1);
        else
            fModel.ChangeHead(acc1);
    }

    public void ChangeAcc2(int acc2)
    {
        customizer.Test.acce2 = acc2;

        if (isMale)
            mModel.ChangeHead(acc2);
        else
            fModel.ChangeHead(acc2);
    }
}
