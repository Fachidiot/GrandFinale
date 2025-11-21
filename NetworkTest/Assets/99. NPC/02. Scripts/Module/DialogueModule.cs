// DialogueModule.cs

using UnityEngine;

public class DialogueModule : MonoBehaviour
{
    [SerializeField]
    private DialogueEvent dialogueEvent; // 이 NPC가 사용할 대화 데이터

    // 외부(주로 Brain)에서 호출할 메서드
    public void StartDialogue()
    {
        if (dialogueEvent == null)
        {
            Debug.LogWarning($"{name}의 DialogueModule에 DialogueEvent가 없습니다.");
            return;
        }

        // 앞으로 만들 DialogueService에 대화 시작을 요청
        // DialogueService.Instance.StartDialogue(dialogueEvent);

        Debug.Log($"DialogueService에 '{dialogueEvent.name}' 대화 시작을 요청합니다.");
    }
}