using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Reflection;
using Michsky.MUIP; // For ModalWindowManager

[CustomEditor(typeof(OptionManager))]
public class OptionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        OptionManager optionManager = (OptionManager)target;

        if (GUILayout.Button("Automatically Setup Shortcut Buttons"))
        {
            SetupShortcutButtons(optionManager);
        }
    }

    private void SetupShortcutButtons(OptionManager optionManager)
    {
        // Get the private field m_ShortcutModal
        FieldInfo modalField = typeof(OptionManager).GetField("m_ShortcutModal", BindingFlags.Instance | BindingFlags.NonPublic);
        ModalWindowManager shortcutModal = (ModalWindowManager)modalField.GetValue(optionManager);

        if (shortcutModal == null)
        {
            Debug.LogError("m_ShortcutModal is not assigned in the inspector!");
            return;
        }

        FieldInfo[] fields = typeof(OptionManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            if (field.FieldType == typeof(Button) && (field.Name.StartsWith("m_Key") || field.Name.StartsWith("m_Bending")))
            {
                Button button = (Button)field.GetValue(optionManager);
                if (button != null)
                {
                    string fieldName = field.Name.Substring(2); // Remove "m_"
                    if (field.Name.StartsWith("m_Key"))
                    {
                        fieldName = field.Name.Replace("m_Key", "");
                    }
                    else if (field.Name.StartsWith("m_Bending"))
                    {
                         fieldName = field.Name.Replace("m_Bending", "BENDING_");
                    }

                    // Special cases for names that don't directly match
                    if (fieldName == "MoveUp") fieldName = "UP";
                    if (fieldName == "MoveDown") fieldName = "DOWN";
                    if (fieldName == "MoveLeft") fieldName = "LEFT";
                    if (fieldName == "MoveRight") fieldName = "RIGHT";
                    if (fieldName == "UnArmed") fieldName = "UNARMED";
                    if (fieldName.StartsWith("Slot")) fieldName = fieldName.ToUpper();

                    try
                    {
                        OptionManager.KeyInput keyInput = (OptionManager.KeyInput)System.Enum.Parse(typeof(OptionManager.KeyInput), fieldName.ToUpper());

                        // Clear existing listeners
                        while (button.onClick.GetPersistentEventCount() > 0)
                        {
                            UnityEventTools.RemovePersistentListener(button.onClick, 0);
                        }

                        // 1. Add listener for ShortCutInput
                        UnityAction<int> shortcutAction = optionManager.ShortCutInput;
                        UnityEventTools.AddIntPersistentListener(button.onClick, shortcutAction, (int)keyInput);

                        // 2. Add listener for opening the modal window
                        UnityAction openModalAction = shortcutModal.Open;
                        UnityEventTools.AddVoidPersistentListener(button.onClick, openModalAction);
                        
                        EditorUtility.SetDirty(button);
                        Debug.Log($"Setup button '{button.name}' to call ShortCutInput and Open Modal.");
                    }
                    catch (System.ArgumentException)
                    {
                        Debug.LogWarning($"Could not find a matching KeyInput enum for button field '{field.Name}'. Tried to parse '{fieldName.ToUpper()}'.");
                    }
                }
            }
        }
    }
}
