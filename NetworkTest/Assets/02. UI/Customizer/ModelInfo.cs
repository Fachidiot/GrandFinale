using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ModelInfo", menuName = "Fachidiot/ModelInfo", order = 0)]
[Serializable]
public class ModelInfo : ScriptableObject
{
    public int head;
    public int body;
    public int acce1;
    public int acce2;

    public ModelInfo(int head, int body, int acce1, int acce2)
    {
        this.head = head;
        this.body = body;
        this.acce1 = acce1;
        this.acce2 = acce2;
    }
}
