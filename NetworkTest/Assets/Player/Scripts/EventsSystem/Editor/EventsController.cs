using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


public class EventsController
{
    protected Color timeLineBackgroundColor = new Color(0.21f, 0.21f, 0.21f);
    protected Color timeLineElementsColor = new Color(0.5f, 0.5f, 0.5f);
    protected string[] eventsNames;
    protected EventInfo[] eventInfos;
    protected readonly GUIStyle parametersStyle = new GUIStyle();
    public virtual void Initialization()
    {
        eventInfos = typeof(EventsCenter).GetEvents(); // get evensInfo
        UpdateEventsNames();

        //reduce text size for convenience
        parametersStyle.fontSize = 11;
        parametersStyle.alignment = TextAnchor.MiddleRight;
        parametersStyle.normal.textColor = Color.white;
        parametersStyle.clipping = TextClipping.Clip;
    }

    void UpdateEventsNames()
    {
        List<String> eventsNamesList = new List<string>();
        eventsNamesList.AddRange(eventInfos.Select(i => i.Name));
        eventsNames = eventsNamesList.ToArray();
    }

    private static string ConvertParameterToStringType(ParameterInfo parameterInfo) // convert to string
    {
        var finalParam =
                $"{parameterInfo.Name}|" +
                $"{parameterInfo.ParameterType}|" +
                $"{GetDefaultValue(parameterInfo.ParameterType)}";

        return finalParam;
    }

    public static object[] ConvertParametersToObjectType(List<string> parameters) // convert from string to object
    {
        object[] parametersArray = new object[parameters.Count]; // create array

        for (int i = 0; i < parameters.Count; i++) // add parameter to array
        {
            var parameterAsObject = GetParameterAsObject(parameters[i]); // convert string parameter to object
            parametersArray[i] = parameterAsObject; // add parameter to array
        }
        return parametersArray;
    }
    private static object GetParameterAsObject(string parameter) // convert parameter value to object type
    {
        // split parameter [0: name | 1: parameter Type | 2: parameter value]
        var parameterFields = parameter.Split('|');

        if (parameterFields[1] == typeof(int).ToString())
            return int.Parse(parameterFields[2]);

        else if (parameterFields[1] == typeof(double).ToString())
            return Double.Parse(parameterFields[2]);

        else if (parameterFields[1] == typeof(float).ToString())
            return Single.Parse(parameterFields[2]);

        else if (parameterFields[1] == typeof(string).ToString())
            return parameterFields[2];

        else if (parameterFields[1] == typeof(bool).ToString())
            return parameterFields[2] == "True";

        else
            return null;
    }

    protected void AddEventToList(List<EventModel> eventsList, float normalTime)
    {
        eventsList.Add(new EventModel()
        {
            time = normalTime
        });
        ApplyDefaultParameterFields(eventsList.Last());
    }

    protected void ApplyDefaultParameterFields(EventModel eventModel)
    {
        var recyclableEvent = eventModel;
        recyclableEvent.parameters.Clear();

        var eventParameters =
                eventInfos[recyclableEvent.funcId].EventHandlerType.GetMethod("Invoke")?.GetParameters();

        if (eventParameters != null)
            foreach (var parameter in eventParameters)
                recyclableEvent.parameters.Add(ConvertParameterToStringType(parameter));

        recyclableEvent.funcName = eventsNames[recyclableEvent.funcId];
    }

    private static string GetDefaultValue(Type forType)
    {
        if (forType == typeof(int) | forType == typeof(float) | forType == typeof(double)) return default(int).ToString();
        else return "";
    }

    protected string GetParameterField(Rect labelRect, Rect fieldRect, string parameter)
    {
        string[] parameters = parameter.Split('|');
        EditorGUI.LabelField(labelRect, parameters[0], parametersStyle);

        if (parameters[1] == typeof(int).ToString())
            parameters[2] = EditorGUI.IntField(fieldRect, Convert.ToInt32(parameters[2])).ToString();

        else if (parameters[1] == typeof(bool).ToString())
            parameters[2] = EditorGUI.Toggle(fieldRect, parameters[2] == "True").ToString();

        else if (parameters[1] == typeof(string).ToString())
            parameters[2] = EditorGUI.TextField(fieldRect, parameters[2]);

        else if (parameters[1] == typeof(double).ToString())
            parameters[2] = EditorGUI.DoubleField(fieldRect,
                    Convert.ToDouble(parameters[2])).ToString(CultureInfo.InvariantCulture);

        else if (parameters[1] == typeof(float).ToString())
            parameters[2] = EditorGUI.FloatField(fieldRect,
                    Convert.ToSingle(parameters[2])).ToString(CultureInfo.CurrentCulture);

        var finParameter = "";

        for (int i = 0; i < parameters.Length; i++)
            finParameter += parameters[i] + (i < 2 ? "|" : "");

        return finParameter;
    }
}

