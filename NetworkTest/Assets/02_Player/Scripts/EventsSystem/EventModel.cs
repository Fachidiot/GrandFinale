using System.Collections.Generic;


[System.Serializable]
public class EventModel
{
    public int funcId; // event id
    public string funcName; // event name
    public List<string> parameters = new List<string>(); // event parameters
    public float time; // event time if event in update
    public bool done;
}

