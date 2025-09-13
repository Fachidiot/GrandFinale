using UnityEditor;
using UnityEngine;

public class GUISplitWindow
{
    private string textOnSplit = "Text On Split"; //splitter bar text
    private bool _resize; //true if you resized window
    private const float ResizePanelHeight = 20f; ///splitter bar
    private float _heightPercent = 0.6f; //top window size in percent

    private Vector2 _scrollPos;

    public void StartToSplit()
    {
        _scrollPos =
                EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height * _heightPercent));

    }
    public void Split()
    {
        EditorGUILayout.EndScrollView();
        ResizePanel();
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
    }
    public void EndSplit()
    {
        EditorGUILayout.EndVertical();
    }

    void ResizePanel()
    {
        Rect resizeHandleRect = new Rect(0, Screen.height * _heightPercent, Screen.width, ResizePanelHeight);
        EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeVertical);
        EditorGUI.DrawRect(resizeHandleRect, new Color(0.1f, 0.1f, 0.1f));
        GUILayout.Label(textOnSplit);
        if (Event.current.type == EventType.MouseDown && resizeHandleRect.Contains(Event.current.mousePosition))
        {
            _resize = true;
        }
        if (_resize)
        {
            _heightPercent = (Event.current.mousePosition.y - ResizePanelHeight / 2) / Screen.height;
        }

        if (Event.current.rawType == EventType.MouseUp && _resize)
            _resize = false;

        _heightPercent = Mathf.Clamp(_heightPercent, 0.1f, 0.9f);
    }
}
