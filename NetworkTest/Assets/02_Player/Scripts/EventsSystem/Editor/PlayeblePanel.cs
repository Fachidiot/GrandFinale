using System.Globalization;
using UnityEditor;
using UnityEngine;


public class PlayTimeLinePanel
{

    public delegate void PlayTimeNormalized(float value);
    public event PlayTimeNormalized OnPlayTimeNormalized;
    private float _normalizedPlayTime;
    private bool _changedValue;
    private float stripWidth = 2f;

    public void SetTime(float value)
    {
        _normalizedPlayTime = value;
        OnPlayTimeNormalized?.Invoke(_normalizedPlayTime);
    }

    public void PlayTimeLine()
    {
        var centeredStyle = GUI.skin.GetStyle("Label");
        centeredStyle.alignment = TextAnchor.MiddleCenter;

        EditorGUILayout.BeginHorizontal("box");
        GUI.enabled = false;

        Rect timeLineRect = EditorGUILayout.GetControlRect(GUILayout.Height(20f), GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(timeLineRect, new Color(0.1f, 0.1f, 0.1f));

        Rect pointerRect = new Rect(Mathf.Lerp(timeLineRect.xMin, timeLineRect.xMax, _normalizedPlayTime), timeLineRect.yMin, stripWidth, timeLineRect.height);
        GUILayout.Label(System.Math.Round(_normalizedPlayTime, 2).ToString(CultureInfo.InvariantCulture), GUILayout.Width(30f), GUILayout.Height(20f));
        GUI.enabled = true;

        EditorGUI.DrawRect(pointerRect, new Color(0.5f, 0.5f, 0.5f));

        centeredStyle.alignment = TextAnchor.UpperLeft;

        EditorGUILayout.EndHorizontal();


        if (Event.current.type == EventType.MouseDown && timeLineRect.Contains(Event.current.mousePosition))
        {
            _changedValue = true;
        }

        if (_changedValue && timeLineRect.width > 1)
        {
            float b = timeLineRect.xMax - Event.current.mousePosition.x;
            float c = (float)System.Math.Round(1 - b / timeLineRect.width, 2);
            _normalizedPlayTime = Mathf.Clamp(c, 0, 1);
            OnPlayTimeNormalized?.Invoke(_normalizedPlayTime);
        }

        if (Event.current.rawType == EventType.MouseUp && _changedValue)
        {
            _changedValue = false;
        }

        EditorGUIUtility.AddCursorRect(timeLineRect, MouseCursor.Link);

    }
}
