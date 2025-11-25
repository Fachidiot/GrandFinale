using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ModelInfo", menuName = "Fachidiot/ModelInfo", order = 0)]
[Serializable]
public class ModelInfo
{
    public int head;
    public int body;
    public int acc1;
    public int acc2;

    public ModelInfo()
    {
        head = 0; body = 0; acc1 = 0; acc2 = 0;
    }

    public ModelInfo(int head, int body, int acc1, int acc2)
    {
        this.head = head;
        this.body = body;
        this.acc1 = acc1;
        this.acc2 = acc2;
    }
}
