using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelCustom : MonoBehaviour
{
    public ModelInfo initInfo;

    public List<GameObject> headModels;
    public List<GameObject> bodyModels;
    public List<GameObject> acc1Models;
    public List<GameObject> acc2Models;

    private void InitialCheck()
    {
        if (headModels == null || headModels.Count == 0)
            Debug.LogError("ModelCustom: Head Models list is not assigned or empty.");
        if (bodyModels == null || bodyModels.Count == 0)
            Debug.LogError("ModelCustom: Body Models list is not assigned or empty.");
        if (acc1Models == null || acc1Models.Count == 0)
            Debug.LogError("ModelCustom: Accessory Models list is not assigned or empty.");
        if (acc2Models == null || acc2Models.Count == 0)
            Debug.LogError("ModelCustom: Accessory Models list is not assigned or empty.");
    }

    private void Awake()
    {
        InitialCheck();
        ApplyModelInfo(initInfo);
    }

    public void ApplyModelInfo(ModelInfo info)
    {
        ChangeHead(info.head);
        ChangeBody(info.body);
        ChangeAcc1(info.acc1);
        ChangeAcc2(info.acc2);
    }

    public void ChangeHead(int head)
    {
        foreach (GameObject model in headModels)
        {
            if (model != null) model.SetActive(false);
        }

        if (head >= 0 && head < headModels.Count && headModels[head] != null)
            headModels[head].SetActive(true);
        else
            Debug.LogWarning($"ModelCustom: Head model index {head} is out of bounds or model is null. Head model not activated.");
    }

    public void ChangeBody(int body)
    {
        foreach (GameObject model in bodyModels)
        {
            if (model != null) model.SetActive(false);
        }

        if (body >= 0 && body < bodyModels.Count && bodyModels[body] != null)
            bodyModels[body].SetActive(true);
        else
            Debug.LogWarning($"ModelCustom: Head model index {body} is out of bounds or model is null. Head model not activated.");
    }

    public void ChangeAcc1(int acc1)
    {
        foreach (GameObject model in acc1Models)
        {
            if (model != null) model.SetActive(false);
        }

        if (acc1 >= 0 && acc1 < acc1Models.Count && acc1Models[acc1] != null)
            acc1Models[acc1].SetActive(true);
        else if (acc1Models[acc1] != null)
            Debug.LogWarning($"ModelCustom: Head model index {acc1} is out of bounds or model is null. Head model not activated.");
    }

    public void ChangeAcc2(int acc2)
    {
        foreach (GameObject model in acc2Models)
        {
            if (model != null) model.SetActive(false);
        }

        if (acc2 >= 0 && acc2 < acc2Models.Count && acc2Models[acc2] != null)
            acc2Models[acc2].SetActive(true);
        else if (acc2Models[acc2] != null)
            Debug.LogWarning($"ModelCustom: Head model index {acc2} is out of bounds or model is null. Head model not activated.");
    }
}
