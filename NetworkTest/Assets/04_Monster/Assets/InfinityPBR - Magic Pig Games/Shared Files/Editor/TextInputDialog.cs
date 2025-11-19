using System.Linq;
using UnityEditor;
using UnityEngine;
using static InfinityPBR.InfinityEditor;

public class TextInputDialog : EditorWindow
{
    private string inputText = "";
    private string message = ""; // Field to store the message
    private System.Action<string> onOk;
    private System.Action onCancel;

    // Method to open the dialog
    public static void ShowDialog(string title, string message, string defaultValue, System.Action<string> onOk, System.Action onCancel, int width = 500, int height = 200)
    {
        var window = ScriptableObject.CreateInstance<TextInputDialog>();
        window.titleContent = new GUIContent(title);
        window.inputText = defaultValue;
        window.message = message; // Store the message
        window.onOk = onOk;
        
        window.onCancel = onCancel;
        
        window.minSize = new Vector2(width, height);
        window.maxSize = window.minSize;

        // give it some initial size, ShowUtility will cause centering
        window.position = new Rect(0, 0, width, height);
        window.ShowUtility();
    }

    private void OnGUI()
    {
        Label($"{message}", false, true, true);
        inputText = TextField(inputText);

        GUILayout.FlexibleSpace();

        StartRow();
        if (Button("Cancel"))
        {
            onCancel?.Invoke();
            Close();
        }
        
        if (Button("OK"))
        {
            onOk?.Invoke(inputText);
            Close();
        }
        EndRow();
    }
    
    // Helper method to find the main Editor window
    private static EditorWindow GetMainWindow()
    {
        // This is an approximation to find the main window. You might need to adjust this based on your setup.
        var mainWindow = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(window => window.GetType().Name == "MainWindow");
        return mainWindow ?? EditorWindow.GetWindow<SceneView>();
    }
    

}
