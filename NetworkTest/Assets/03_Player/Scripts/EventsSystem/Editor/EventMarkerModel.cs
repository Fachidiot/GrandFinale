using UnityEditor;
using UnityEngine;


public class EventMarkerModel
{
    private float _normalizedPlayTime; //play time
    private bool _changedValue; // change event marker time?
    private EventModel _selectEvent;

    public void Show(Rect timeLineRect, Rect markerRect, EventModel eventModel, out EventModel selectEventModel, Color imageColor)
    {
        GUI.DrawTexture(markerRect, EditorGUIUtility.IconContent("d_Animation.EventMarker").image, ScaleMode.ScaleAndCrop, true, 0, imageColor, 0, 0);

        //if mouse button on marker body
        if (Event.current.type == EventType.MouseDown && markerRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.button == 0) //if left mouse button? select event
            {
                Event.current.Use();
                selectEventModel = eventModel;
            }
            else //if right mouse button? change event marker time
            {
                selectEventModel = null;
                _changedValue = true;
                _selectEvent = eventModel;
                _normalizedPlayTime = _selectEvent.time;
            }
        }
        else selectEventModel = null;

        //change event marker time
        if (_changedValue && timeLineRect.width > 1)
        {
            float b = timeLineRect.xMax - Event.current.mousePosition.x;

            float c = (float)System.Math.Round(1 - b / timeLineRect.width, 2);

            _normalizedPlayTime = Mathf.Clamp(c, 0, 1);

            _selectEvent.time = _normalizedPlayTime;
        }

        if (_changedValue && Event.current.button == 1 && Event.current.rawType == EventType.MouseUp)
        {
            Event.current.Use();
            _changedValue = false;
            _selectEvent = null;
        }
    }
}

