using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(RigGroup))]
public class RigGroup_Editor : Editor
{
	public ReorderableList _list;

	// Start is called before the first frame update
	private void OnEnable()
	{
		_list = new ReorderableList(serializedObject, serializedObject.FindProperty("jobsObjects"), true, true, true, true);

		_list.drawElementCallback = (Rect rect,int index,bool isActive,bool isFocused) =>
		{
			var element = _list.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;
			EditorGUI.PropertyField(
				new Rect(rect.x, rect.y, rect.width - 30, EditorGUIUtility.singleLineHeight),
				element.FindPropertyRelative("onRigComponent"), GUIContent.none);
			EditorGUI.PropertyField(
				new Rect(rect.x + (rect.width - 30), rect.y, rect.width - 90, EditorGUIUtility.singleLineHeight),
				element.FindPropertyRelative("mActive"), GUIContent.none);
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
