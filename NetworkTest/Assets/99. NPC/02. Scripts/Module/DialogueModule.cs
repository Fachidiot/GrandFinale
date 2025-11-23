// DialogueModule.cs

using UnityEngine;

public class DialogueModule : MonoBehaviour
{
    [SerializeField]
    private DialogueEvent dialogueEvent;

    public void StartDialogue()
    {
        if (dialogueEvent == null)
        {
            Debug.LogWarning($"{name}의 DialogueModule에 DialogueEvent가 없습니다.");
            return;
        }

    }
}