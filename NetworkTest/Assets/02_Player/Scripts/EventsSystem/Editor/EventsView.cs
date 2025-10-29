using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


public class EventsView : EventsController
{
    public StateMachineBehaviour selectedState; //selected animation state behavior to edit
    private ReorderableList _eventsList; //ReorderableList for editing eventslist
    private float _normalTime; //playing time
    readonly EventsTimelineModel _timeLineModel = new EventsTimelineModel(); // timeline visual
    private int _toolbarInt; // select bar (start | update | end)
    private int _toolbar; // last select toolbarID
    readonly string[] _toolbarStrings = { "State enter", "State update", "State exit" };
    private SerializedObject _serializedObject; //object with eventlist
    private EventModel _selectEvent; // selected event for edit
    private readonly EventMarkerModel _eventMarker = new EventMarkerModel(); // event marker in update event visual

    public void TimeUpdate(float time) => _normalTime = time; //update play time

    public override void Initialization()
    {
        base.Initialization();
        //init editor for event list
        ReorderableEventsList();
    }

    public void MainBody()
    {
        if (_eventsList == null)
        {
            ReorderableEventsList();
            return;
        }

        _toolbarInt = GUILayout.Toolbar(_toolbarInt, _toolbarStrings);

        if (_toolbarInt == 0 | _toolbarInt == 2)
        {
            if (_toolbar != _toolbarInt) // if tool bar change? update reorderable list
            {
                ReorderableEventsList();
                _toolbar = _toolbarInt;
            }

            _serializedObject.Update();
            _eventsList.DoLayoutList();
            _serializedObject.ApplyModifiedProperties();
        }
        else
        {
            _toolbar = _toolbarInt;
            UpdateEventsBody();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void UpdateEventsBody()
    {
        EditorGUILayout.BeginHorizontal();

        //add event button
        if (GUILayout.Button(EditorGUIUtility.IconContent("Animation.AddEvent"), GUILayout.Width(25)))
        {
            if (selectedState is IEventsLists eventsLists) AddEventToList(eventsLists.UpdateEvents, _normalTime);
        }

        //timeline in update window
        Rect timeLineBody = EditorGUILayout.GetControlRect(GUILayout.Height(17f), GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(timeLineBody, timeLineBackgroundColor);
        Rect timeLineRect = new Rect(timeLineBody.x + timeLineBody.width * 0.01f, timeLineBody.yMin, timeLineBody.width * 0.9f, 15);

        _timeLineModel.View(timeLineBody, timeLineRect, timeLineElementsColor, _normalTime);

        EditorGUILayout.EndHorizontal();

        //if selected state have a "IEventsList"
        if (selectedState is IEventsLists iel)
            if (iel.UpdateEvents.Count > 0)
                ShowEventMarkers(timeLineRect);

        if (_selectEvent == null)
        {
            EditorGUILayout.HelpBox("Select event", MessageType.None);
        }
    }

    //event marker in update events
    public void ShowEventMarkers(Rect timeLineRect)
    {
        foreach (var eventModel in (selectedState as IEventsLists).UpdateEvents.ToArray())
        {
            Rect markerRect = new Rect(Mathf.Lerp(timeLineRect.xMin, timeLineRect.xMax, eventModel.time) - 5f, timeLineRect.y, 10, 20);

            _eventMarker.Show(timeLineRect, markerRect, eventModel, out EventModel selectEventModel, Equals(_selectEvent, eventModel) ? Color.yellow : Color.white);

            if (selectEventModel != null)
            {
                _selectEvent = selectEventModel;
            }
        }

        if (_selectEvent == null) return;

        EditorGUILayout.BeginHorizontal();

        //delete button
        if (GUILayout.Button(EditorGUIUtility.IconContent("d_winbtn_mac_min_h"),
                parametersStyle, GUILayout.Height(22f)))
        {
            (selectedState as IEventsLists).UpdateEvents.Remove(_selectEvent);
            _selectEvent = null;
            return;
        }

        var prePopup = _selectEvent.funcId;
        _selectEvent.funcId = EditorGUILayout.Popup(_selectEvent.funcId,
                eventsNames, GUILayout.ExpandWidth(true));

        if (prePopup != _selectEvent.funcId)
            ApplyDefaultParameterFields(_selectEvent);

        Rect parametersRect = GUILayoutUtility.GetRect(200,
                EditorGUIUtility.singleLineHeight * _selectEvent.parameters.Count);

        var id = (selectedState as IEventsLists).UpdateEvents.FindIndex(x => x == _selectEvent);
        for (int i = 0; i < _selectEvent.parameters.Count; i++)
        {
            Rect parameterRect = new Rect(parametersRect.xMax - 200, parametersRect.y + EditorGUIUtility.singleLineHeight * i, 200, EditorGUIUtility.singleLineHeight);

            ParameterField(parameterRect, _selectEvent.parameters[i], id, i);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ReorderableEventsList()
    {
        // change edit list from tool bar
        var listType = _toolbarInt == 0 ?
                (selectedState as IEventsLists).EnterEvents : (selectedState as IEventsLists).ExitEvents;

        _serializedObject = new SerializedObject(selectedState);
        _eventsList = new ReorderableList(listType, typeof(EventModel))
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, _toolbarInt == 0 ? "EnterEvents" : "ExitEvents"),

            onAddCallback = list =>
            {
                if (eventInfos.Length > 0)
                    AddEventToList(listType, 0);
            },

            elementHeightCallback = delegate (int index)
            {
                var elementHeight = EditorGUIUtility.singleLineHeight * listType[index].parameters.Count;

                var margin = 15;
                return elementHeight + margin;
            },

            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                int prePopup = listType[index].funcId;
                listType[index].funcId =
                        EditorGUI.Popup(new Rect(rect.xMin, rect.yMin, 100, EditorGUIUtility.singleLineHeight),
                                listType[index].funcId, eventsNames);

                if (prePopup != listType[index].funcId)
                    ApplyDefaultParameterFields(listType[index]);

                for (int i = 0; i < listType[index].parameters.Count; i++)
                {
                    Rect objectFieldRect = new Rect(rect.xMax - 200, rect.y + EditorGUIUtility.singleLineHeight * i,
                            200, EditorGUIUtility.singleLineHeight);

                    ParameterField(objectFieldRect, listType[index].parameters[i], index, i);

                        //bottom line
                        EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - 8f, rect.width, 1),
                            new Color(0.15f, 0.15f, 0.15f));
                }
            }
        };


    }

    void ParameterField(Rect r, string parameter, int eventIndex, int parameterID)
    {
        Rect lab = new Rect(r.xMax - r.width, r.y, r.width * 0.38f, r.height);
        Rect field = new Rect(r.xMax - r.width * 0.6f, r.y, r.width * 0.6f, r.height * 0.95f);

        var listType = _toolbarInt == 0 ?
                (selectedState as IEventsLists).EnterEvents : _toolbarInt == 1 ?
                        (selectedState as IEventsLists).UpdateEvents : (selectedState as IEventsLists).ExitEvents;

        listType[eventIndex].parameters[parameterID] = GetParameterField(lab, field, parameter);
    }
}


