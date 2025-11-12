using System.Collections.Generic;

public interface IEventsLists
{
    List<EventModel> EnterEvents { get; set; }
    List<EventModel> UpdateEvents { get; set; }
    List<EventModel> ExitEvents { get; set; }
}

