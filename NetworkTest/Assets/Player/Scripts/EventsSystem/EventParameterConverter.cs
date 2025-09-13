using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class EventParameterConverter
{
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

    private static string GetDefaultValue(Type forType)
    {
        if (forType == typeof(int) | forType == typeof(float) | forType == typeof(double)) return default(int).ToString();
        else return "";
    }
}
