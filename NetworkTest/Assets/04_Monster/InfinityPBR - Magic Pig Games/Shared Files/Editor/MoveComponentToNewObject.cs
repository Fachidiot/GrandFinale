using UnityEngine;
using UnityEditor;

public class MoveComponentToNewObject : MonoBehaviour
{
    [MenuItem("CONTEXT/Component/Move Component to New Object")]
    private static void MoveToNewObject(MenuCommand command)
    {
        // Step 1: Reference to the component to move
        Component componentToMove = command.context as Component;
        if (componentToMove == null)
        {
            Debug.LogError("Component is null.");
            return;
        }

        // Step 2: Create a new child GameObject
        GameObject parentObject = componentToMove.gameObject;
        GameObject newChildObject = new GameObject($"{componentToMove.GetType().Name}");
        newChildObject.transform.SetParent(parentObject.transform.parent);
        newChildObject.transform.localPosition = Vector3.zero; // Position it at the parent's location

        // Step 3: Copy the component to the new child object
        Component newComponent = newChildObject.AddComponent(componentToMove.GetType());
        if (newComponent != null)
        {
            UnityEditorInternal.ComponentUtility.CopyComponent(componentToMove);
            UnityEditorInternal.ComponentUtility.PasteComponentValues(newComponent);
        }

        // Step 4: Remove the component from the original parent object
        DestroyImmediate(componentToMove);

        // Step 5: Select the new object and highlight it for renaming
        Selection.activeGameObject = newChildObject;
        EditorGUIUtility.PingObject(newChildObject);
    }
    
    [MenuItem("CONTEXT/Component/Move Component to New Child Object")]
    private static void MoveToNewChildObject(MenuCommand command)
    {
        // Step 1: Reference to the component to move
        Component componentToMove = command.context as Component;
        if (componentToMove == null)
        {
            Debug.LogError("Component is null.");
            return;
        }

        // Step 2: Create a new child GameObject
        GameObject parentObject = componentToMove.gameObject;
        GameObject newChildObject = new GameObject($"{componentToMove.GetType().Name}");
        newChildObject.transform.SetParent(parentObject.transform);
        newChildObject.transform.localPosition = Vector3.zero; // Position it at the parent's location

        // Step 3: Copy the component to the new child object
        Component newComponent = newChildObject.AddComponent(componentToMove.GetType());
        if (newComponent != null)
        {
            UnityEditorInternal.ComponentUtility.CopyComponent(componentToMove);
            UnityEditorInternal.ComponentUtility.PasteComponentValues(newComponent);
        }

        // Step 4: Remove the component from the original parent object
        DestroyImmediate(componentToMove);

        // Step 5: Select the new object and highlight it for renaming
        Selection.activeGameObject = newChildObject;
        EditorGUIUtility.PingObject(newChildObject);
    }
}