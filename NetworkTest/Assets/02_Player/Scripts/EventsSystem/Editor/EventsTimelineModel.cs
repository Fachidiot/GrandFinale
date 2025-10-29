using UnityEditor;
using UnityEngine;

public class EventsTimelineModel //timeline visualization
{
    private readonly Color _bottomBorderColor = new Color(0.15f, 0.15f, 0.15f);
    public void View(Rect timeLineBody, Rect timeLineRect, Color pointsColor, float normalTime)
    {
        GUIStyle myStyle = new GUIStyle
        {
            fontSize = 10,
            normal =
                {
                    textColor = pointsColor
                }
        };

        EditorGUI.DrawRect(new Rect(timeLineRect.xMin, timeLineRect.yMax - 9f, 1.5f, 12), pointsColor);
        GUI.Label(new Rect(timeLineRect.xMin + 3f, timeLineRect.yMax - 7f, 1, 5), "0:00", myStyle);

        EditorGUI.DrawRect(new Rect(timeLineRect.xMin + timeLineRect.width / 4, timeLineRect.yMax - 6f, 1.5f, 9), pointsColor);
        GUI.Label(new Rect(timeLineRect.xMin + 3f + timeLineRect.width / 4, timeLineRect.yMax - 8f, 1, 5), "0:25", myStyle);

        EditorGUI.DrawRect(new Rect(timeLineRect.xMin + timeLineRect.width / 2, timeLineRect.yMax - 6f, 1.5f, 9), pointsColor);
        GUI.Label(new Rect(timeLineRect.xMin + 3f + timeLineRect.width / 2, timeLineRect.yMax - 8f, 1, 5), "0:50", myStyle);

        EditorGUI.DrawRect(new Rect(timeLineRect.xMax - timeLineRect.width / 4, timeLineRect.yMax - 6f, 1.5f, 9), pointsColor);
        GUI.Label(new Rect((timeLineRect.xMax + 3f) - timeLineRect.width / 4, timeLineRect.yMax - 8f, 1, 5), "0:75", myStyle);

        EditorGUI.DrawRect(new Rect(timeLineRect.xMax, timeLineRect.yMax - 9f, 1.5f, 12), pointsColor);
        GUI.Label(new Rect(timeLineRect.xMax + 3f, timeLineRect.yMax - 8f, 1, 5), "1:00", myStyle);

        // cursor
        EditorGUI.DrawRect(new Rect(Mathf.Lerp(timeLineRect.xMin, timeLineRect.xMax, normalTime), timeLineRect.y, 1.5f, 20), Color.white);

        //bottom Border
        EditorGUI.DrawRect(new Rect(timeLineBody.xMin, timeLineBody.yMax + 1f, timeLineBody.width, 2), _bottomBorderColor);
    }
}

