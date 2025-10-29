using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


public class CurvesEventsView
{
    public StateMachineBehaviour selectedState;//selected animation state behavior to edit
    private ReorderableList _curvesList;//ReorderableList for editing eventslist

    private SerializedObject _serializedObject; //object with eventlist
    private bool canEditKey;//turns on when the animation playback time coincides with the time of the key in the curves

    public delegate void KeyframeSelect(float time);
    public event KeyframeSelect OnKeyframeSelected; //the event is called when the time changes by the time of the key in the curve

    private float _normalTime;  //playing time

    private string[] _eventsNames; //events names list

    public void TimeUpdate(float value) => _normalTime = value; //update play time

    public void MainBody()
    {
        //render list for editing

        if (_curvesList == null)
        {
            ReorderableCurvesList();
            return;
        }

        _serializedObject.Update();
        _curvesList.DoLayoutList();
        _serializedObject.ApplyModifiedProperties();

    }
    public void Initialization()
    {
        UpdateEventsNames(); // check events names
        ReorderableCurvesList();
    }
    void UpdateEventsNames()
    {
        var eventInfos = typeof(EventsCenter).GetEvents(); // GET events
        List<String> eventsNamesList = new List<string>();
        // add event name to list
        foreach (var eventInfo in eventInfos)
        {
            var eventParameters = eventInfo.EventHandlerType.GetMethod("Invoke")?.GetParameters();

            if (eventParameters == null | eventParameters.Length != 1) continue;

            if (eventParameters[0].ParameterType == typeof(float))
            {
                eventsNamesList.Add(eventInfo.Name);
            }
        }
        // copy list
        _eventsNames = eventsNamesList.ToArray();
    }

    private void ReorderableCurvesList()
    {
        _serializedObject = new SerializedObject(selectedState); //selected object as object with curves

        var listType = (selectedState as ICurveEventsList).CurveEvents;

        _curvesList = new ReorderableList(listType, typeof(CurveEventModel))
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Curve Events:"), // //title of the editor window
            elementHeight = EditorGUIUtility.singleLineHeight * 2 + 15, //element height
            onAddCallback = list =>//when adding a new item
            {
                CurveEventModel curveModel = new CurveEventModel
                {
                    eventName = _eventsNames[0],
                    animationCurve = new AnimationCurve()
                };
                curveModel.animationCurve.AddKey(0, 0);
                curveModel.animationCurve.AddKey(1, 0);
                listType.Add(curveModel); //adding a new curve to a list
                },
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.y += 3f;
                ElementBodyView(rect, listType[index]);
            }
        };

    }

    private void ElementBodyView(Rect rect, CurveEventModel curveModel)
    {
        var editingCurveModel = curveModel;
        int editKeyId = -1;

        for (int i = 0; i < editingCurveModel.animationCurve.keys.Length; i++)
        {
            //if the playback time coincides with the key time, select keyID
            if (Mathf.Abs(_normalTime - editingCurveModel.animationCurve.keys[i].time) < 0.0025f)
            {
                editKeyId = i;
            }
        }

        float elementHeight = EditorGUIUtility.singleLineHeight + 2;

        //set event name
        curveModel.funcID = EditorGUI.Popup(new Rect(rect.xMin, rect.yMin, 100, EditorGUIUtility.singleLineHeight),
                         curveModel.funcID, _eventsNames);

        curveModel.eventName = _eventsNames[curveModel.funcID];

        //prevKey button
        if (GUI.Button(new Rect(rect.xMin, rect.yMin + EditorGUIUtility.singleLineHeight + 2, 30, elementHeight), EditorGUIUtility.IconContent("Animation.PrevKey")))
        {
            float lastCheckTime = 0;
            foreach (var k in editingCurveModel.animationCurve.keys)
            {
                if (k.time < _normalTime && k.time > lastCheckTime)
                {
                    lastCheckTime = k.time;
                }
            }
            OnKeyframeSelected?.Invoke(lastCheckTime); // invoke the key selection event
        }
        //nextKey button
        if (GUI.Button(new Rect(rect.xMin + 30 + 2, rect.yMin + EditorGUIUtility.singleLineHeight + 2, 30, elementHeight), EditorGUIUtility.IconContent("Animation.NextKey")))
        {
            float lastCheckTime = 0;
            foreach (var k in editingCurveModel.animationCurve.keys)
            {
                if (k.time > _normalTime)
                {
                    lastCheckTime = k.time;
                    break;
                }
            }
            OnKeyframeSelected?.Invoke(lastCheckTime); // invoke the key selection event
        }


        if (editKeyId > -1)//if the time does not correspond to any key, then activate the button for adding a key
        {
            Keyframe[] keyframes = editingCurveModel.animationCurve.keys;
            float val = keyframes[editKeyId].value;

            val = EditorGUI.DelayedFloatField(new Rect(rect.xMin + 30 + 30 + 4, rect.yMin + EditorGUIUtility.singleLineHeight + 2, 50, EditorGUIUtility.singleLineHeight), val);

            keyframes[editKeyId].value = val;
            editingCurveModel.animationCurve.keys = keyframes;

            GUI.enabled = false;
            GUI.Button(new Rect(rect.xMin + 30 + 30 + 50 + 6, rect.yMin + EditorGUIUtility.singleLineHeight + 2, 30, elementHeight), EditorGUIUtility.IconContent("Animation.AddKeyframe"));
            GUI.enabled = true;
        }
        else // activate the ability to edit the key value
        {
            GUI.enabled = false;
            /* EditorGUI.DelayedDoubleField(
                    new Rect(rect.xMin + 30 + 30 + 4, rect.yMin + EditorGUIUtility.singleLineHeight + 2, 50, EditorGUIUtility.singleLineHeight),
                    System.Math.Round(editingCurveModel.animationCurve.Evaluate(_normalTime), 2)); */
            GUI.enabled = true;

            if (GUI.Button(new Rect(rect.xMin + 30 + 30 + 50 + 6, rect.yMin + EditorGUIUtility.singleLineHeight + 2, 30, elementHeight), EditorGUIUtility.IconContent("Animation.AddKeyframe")))
            {
                editingCurveModel.animationCurve.AddKey(_normalTime, editingCurveModel.animationCurve.Evaluate(_normalTime));
            }
        }

        var curveRect = new Rect(rect.xMin + 148, rect.yMin - 1, rect.width - 148, EditorGUIUtility.singleLineHeight * 2 + 2);

        editingCurveModel.animationCurve = EditorGUI.CurveField(curveRect, editingCurveModel.animationCurve);

        foreach (var key in editingCurveModel.animationCurve.keys)
            EditorGUI.DrawRect(new Rect(Mathf.Lerp(curveRect.xMin, curveRect.xMax, key.time) - 0.5f, curveRect.yMax - 7f, 1f, 7f), Color.white);

        EditorGUI.DrawRect(new Rect(Mathf.Lerp(curveRect.xMin, curveRect.xMax, _normalTime) - 0.75f, curveRect.y, 1.5f, curveRect.height), Color.white);
    }
}