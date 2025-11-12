using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(DynamicButton))]
public class DynamicButtonEditor : UnityEditor.Editor
{
    /* Inspector�� �׸��� �Լ� */
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        //SerializedProperty boolVar = serializedObject.FindProperty("boolVar"); //Bool
        //SerializedProperty intVar = serializedObject.FindProperty("intVar"); //Int
        //SerializedProperty floatVar = serializedObject.FindProperty("floatVar"); //Float

        //SerializedProperty arrayVar = serializedObject.FindProperty("arrayVar"); // 
        //{
        //    SerializedProperty arrayElementVar = arrayVar.GetArrayElementAtIndex(i);
        //}

        //SerializedProperty structVar = serializedObject.FindProperty("structVar"); //����ü

        // Button Properties
        SerializedProperty interactableVar = serializedObject.FindProperty("interactable");
        SerializedProperty transitionVar = serializedObject.FindProperty("transition");
        SerializedProperty targetGraphicVar = serializedObject.FindProperty("targetGraphic");

        SerializedProperty normalColorVar = serializedObject.FindProperty("colors.normalColor");
        SerializedProperty highlightedColorVar = serializedObject.FindProperty("colors.highlightedColor");
        SerializedProperty pressedColorVar = serializedObject.FindProperty("colors.pressedColor");
        SerializedProperty selectedColorVar = serializedObject.FindProperty("colors.selectedColor");
        SerializedProperty disabledColorVar = serializedObject.FindProperty("colors.disabledColor");
        SerializedProperty colorMultiplierVar = serializedObject.FindProperty("colors.colorMultiplier");
        SerializedProperty fadeDurationVar = serializedObject.FindProperty("colors.fadeDuration");
        SerializedProperty navigationVar = serializedObject.FindProperty("navigation");
        SerializedProperty onClickVar = serializedObject.FindProperty("onClick");

        // AdditionalButton Properties
        SerializedProperty grapicVar = serializedObject.FindProperty("additionalGraphics");
        for (int i = 0; i < grapicVar.arraySize; ++i)
        {
            SerializedProperty arrayElementVar = grapicVar.GetArrayElementAtIndex(i);
        }

        SerializedProperty additionalNormalColorVar = serializedObject.FindProperty("additionalNormalColor");
        SerializedProperty additionalHighlightedColorVar = serializedObject.FindProperty("additionalHighlightedColor");
        SerializedProperty additionalPressedColorVar = serializedObject.FindProperty("additionalPressedColor");
        SerializedProperty additionalSelectedColorVar = serializedObject.FindProperty("additionalSelectedColor");
        SerializedProperty additionalDisabledColorVar = serializedObject.FindProperty("additionalDisabledColor");
    }
}
#endif