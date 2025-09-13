using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(RigBase))]
public class RigBase_Editor : Editor
{
	private ReorderableList _list;
	// Start is called before the first frame update
	private void OnEnable()
	{
		_list = new ReorderableList(serializedObject, serializedObject.FindProperty("RigGroupObjects"), true, true, true, true);
		_list.drawElementCallback = (rect, index, isActive, isFocused) =>
		{
			var element = _list.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;
			EditorGUI.PropertyField(
				new Rect(rect.x, rect.y, rect.width - 30, EditorGUIUtility.singleLineHeight),
				element.FindPropertyRelative("m_Object"), GUIContent.none);
			EditorGUI.PropertyField(
				new Rect(rect.x + (rect.width - 30), rect.y, rect.width - 90, EditorGUIUtility.singleLineHeight),
				element.FindPropertyRelative("m_Active"), GUIContent.none);
		};

	}
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.Separator();
		_list.DoLayoutList();
		serializedObject.ApplyModifiedProperties();
	}
}
