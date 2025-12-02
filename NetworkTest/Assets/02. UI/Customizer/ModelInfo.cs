using System;

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

    public bool Equals(ModelInfo other)
    {
        if (other == null) return false;
        return this.head == other.head &&
               this.body == other.body &&
               this.acc1 == other.acc1 &&
               this.acc2 == other.acc2;
    }
}
