using System.Collections;
using System.Collections.Generic;
using Michsky.MUIP;
using UnityEngine;

public class CustomizeManager : MonoBehaviour
{
    [SerializeField] private GameObject customizePanel;
    [SerializeField] private ModelCustom fModel;
    [SerializeField] private ModelCustom mModel;
    public ModelCustom GetCurrentModel()
    {
        return isMale ? mModel : fModel;
    }

    [SerializeField] private HorizontalSelector isMaleSelector;
    [SerializeField] private HorizontalSelector headSelector;
    [SerializeField] private HorizontalSelector bodySelector;
    [SerializeField] private HorizontalSelector acce1Selector;
    [SerializeField] private HorizontalSelector acce2Selector;

    private PlayerCustomizer customizer;
    private bool isMale = true;

    void Start()
    {
        customizer = PlayerCustomizer.Instance;
        ActiveMale();
        ModelApply();
    }

    private void ModelApply()
    {
        fModel.ApplyModelInfo(customizer.LocalFemaleModelInfo);
        mModel.ApplyModelInfo(customizer.LocalMaleModelInfo);
    }

    public void TogglePanel()
    {
        customizePanel.SetActive(!customizePanel.activeSelf);
    }

    public void ChangeMale(int isMale)
    {
        this.isMale = isMale > 0 ? false : true;
        customizer.IsMale = this.isMale;

        if (this.isMale)
            ActiveMale();
        else
            ActiveFemale();
    }

    public void ChangeHead(int head)
    {
        if (isMale)
        {
            customizer.LocalMaleModelInfo.head = head;
            mModel.ChangeHead(head);
        }
        else
        {
            customizer.LocalFemaleModelInfo.head = head;
            fModel.ChangeHead(head);
        }
    }

    public void ChangeBody(int body)
    {
        if (isMale)
        {
            customizer.LocalMaleModelInfo.body = body;
            mModel.ChangeBody(body);
        }
        else
        {
            customizer.LocalFemaleModelInfo.body = body;
            fModel.ChangeBody(body);
        }
    }

    public void ChangeAcc1(int acc1)
    {
        if (isMale)
        {
            customizer.LocalMaleModelInfo.acc1 = acc1;
            mModel.ChangeAcc1(acc1);
        }
        else
        {
            customizer.LocalFemaleModelInfo.acc1 = acc1;
            fModel.ChangeAcc1(acc1);
        }
    }

    public void ChangeAcc2(int acc2)
    {
        if (isMale)
        {
            customizer.LocalMaleModelInfo.acc2 = acc2;
            mModel.ChangeAcc2(acc2);
        }
        else
        {
            customizer.LocalFemaleModelInfo.acc2 = acc2;
            fModel.ChangeAcc2(acc2);
        }
    }

    public void SaveData()
    {
        customizer.SaveModelInfo();
    }

    private void ActiveFemale()
    {
        mModel.transform.parent.gameObject.SetActive(false);
        fModel.transform.parent.gameObject.SetActive(true);

        headSelector.defaultIndex = 0;
        headSelector.items.Clear();
        for (int i = 0; i < fModel.headModels.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("머리 " + (i + 1)).ToString();
            headSelector.items.Add(item);
        }
        headSelector.index = customizer.LocalFemaleModelInfo.head;
        headSelector.UpdateUI();

        bodySelector.defaultIndex = 0;
        bodySelector.items.Clear();
        for (int i = 0; i < fModel.bodyModels.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("몸통 " + (i + 1)).ToString();
            bodySelector.items.Add(item);
        }
        bodySelector.index = customizer.LocalFemaleModelInfo.body;
        bodySelector.UpdateUI();

        acce1Selector.defaultIndex = 0;
        acce1Selector.items.Clear();
        for (int i = 0; i < fModel.acc1Models.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("장신구1 " + (i + 1)).ToString();
            acce1Selector.items.Add(item);
        }
        acce1Selector.index = customizer.LocalFemaleModelInfo.acc1;
        acce1Selector.UpdateUI();

        acce2Selector.defaultIndex = 0;
        acce2Selector.items.Clear();
        for (int i = 0; i < fModel.acc2Models.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("장신구2 " + (i + 1)).ToString();
            acce2Selector.items.Add(item);
        }
        acce2Selector.index = customizer.LocalFemaleModelInfo.acc2;
        acce2Selector.UpdateUI();
    }

    private void ActiveMale()
    {
        mModel.transform.parent.gameObject.SetActive(true);
        fModel.transform.parent.gameObject.SetActive(false);

        headSelector.defaultIndex = 0;
        headSelector.items.Clear();
        for (int i = 0; i < mModel.headModels.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("머리 " + (i + 1)).ToString();
            headSelector.items.Add(item);
        }
        headSelector.index = customizer.LocalMaleModelInfo.head;
        headSelector.UpdateUI();

        bodySelector.defaultIndex = 0;
        bodySelector.items.Clear();
        for (int i = 0; i < mModel.bodyModels.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("몸통 " + (i + 1)).ToString();
            bodySelector.items.Add(item);
        }
        bodySelector.index = customizer.LocalMaleModelInfo.body;
        bodySelector.UpdateUI();

        acce1Selector.defaultIndex = 0;
        acce1Selector.items.Clear();
        for (int i = 0; i < mModel.acc1Models.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("장신구1 " + (i + 1)).ToString();
            acce1Selector.items.Add(item);
        }
        acce1Selector.index = customizer.LocalMaleModelInfo.acc1;
        acce1Selector.UpdateUI();

        acce2Selector.defaultIndex = 0;
        acce2Selector.items.Clear();
        for (int i = 0; i < mModel.acc2Models.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("장신구2 " + (i + 1)).ToString();
            acce2Selector.items.Add(item);
        }
        acce2Selector.index = customizer.LocalMaleModelInfo.acc2;
        acce2Selector.UpdateUI();
    }
}
