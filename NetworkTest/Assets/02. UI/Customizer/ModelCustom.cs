using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelCustom : MonoBehaviour
{
    public List<GameObject> headModels;
    public List<GameObject> bodyModels;
    public List<GameObject> acceModels;

    private void InitialCheck()
    {
        if (headModels == null || headModels.Count == 0)
            Debug.LogError("ModelCustom: Head Models list is not assigned or empty.");
        if (bodyModels == null || bodyModels.Count == 0)
            Debug.LogError("ModelCustom: Body Models list is not assigned or empty.");
        if (acceModels == null || acceModels.Count == 0)
            Debug.LogError("ModelCustom: Accessory Models list is not assigned or empty.");
    }

    private void Awake()
    {
        InitialCheck();

        ModelInfo initialInfo = new ModelInfo(0, 0, -1, -1);
        ApplyModelInfo(initialInfo);
    }

    public void ApplyModelInfo(ModelInfo info)
    {
        // Deactivate all current models
        foreach (GameObject model in headModels)
        {
            if (model != null) model.SetActive(false);
        }
        foreach (GameObject model in bodyModels)
        {
            if (model != null) model.SetActive(false);
        }
        foreach (GameObject model in acceModels)
        {
            if (model != null) model.SetActive(false);
        }

        // Activate specified models using indices
        if (info.head >= 0 && info.head < headModels.Count && headModels[info.head] != null)
            headModels[info.head].SetActive(true);
        else
            Debug.LogWarning($"ModelCustom: Head model index {info.head} is out of bounds or model is null. Head model not activated.");

        if (info.body >= 0 && info.body < bodyModels.Count && bodyModels[info.body] != null)
            bodyModels[info.body].SetActive(true);
        else
            Debug.LogWarning($"ModelCustom: Body model index {info.body} is out of bounds or model is null. Body model not activated.");

        if (info.acce1 >= 0 && info.acce1 < acceModels.Count && acceModels[info.acce1] != null)
            acceModels[info.acce1].SetActive(true);
        else
            Debug.LogWarning($"ModelCustom: Accessory 1 model index {info.acce1} is out of bounds or model is null. Accessory 1 not activated.");

        if (info.acce2 >= 0 && info.acce2 < acceModels.Count && acceModels[info.acce2] != null)
            acceModels[info.acce2].SetActive(true);
        else
            Debug.LogWarning($"ModelCustom: Accessory 2 model index {info.acce2} is out of bounds or model is null. Accessory 2 not activated.");
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
        foreach (GameObject model in headModels)
        {
            if (model != null) model.SetActive(false);
        }

        if (body >= 0 && body < headModels.Count && headModels[body] != null)
            headModels[body].SetActive(true);
        else
            Debug.LogWarning($"ModelCustom: Head model index {body} is out of bounds or model is null. Head model not activated.");
    }

    public void ChangeAcc1(int acc1)
    {
        foreach (GameObject model in headModels)
        {
            if (model != null) model.SetActive(false);
        }

        if (acc1 >= 0 && acc1 < headModels.Count && headModels[acc1] != null)
            headModels[acc1].SetActive(true);
        else
            Debug.LogWarning($"ModelCustom: Head model index {acc1} is out of bounds or model is null. Head model not activated.");
    }

    public void ChangeAcc2(int acc2)
    {
        foreach (GameObject model in headModels)
        {
            if (model != null) model.SetActive(false);
        }

        if (acc2 >= 0 && acc2 < headModels.Count && headModels[acc2] != null)
            headModels[acc2].SetActive(true);
        else
            Debug.LogWarning($"ModelCustom: Head model index {acc2} is out of bounds or model is null. Head model not activated.");
    }
}
