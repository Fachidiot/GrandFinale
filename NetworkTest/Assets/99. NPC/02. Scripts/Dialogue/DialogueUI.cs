using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    private DialogueService _dialogueService;
    private NPCBubbleUI _activeBubble; // 현재 활성화된 말풍선 참조

    private void Start()
    {
        _dialogueService = DialogueService.Instance;
        _dialogueService.OnLineStart += ShowLineInBubble;
        _dialogueService.OnDialogueEnd += HideBubble;
    }

    private void OnDestroy()
    {
        if (_dialogueService != null)
        {
            _dialogueService.OnLineStart -= ShowLineInBubble;
            _dialogueService.OnDialogueEnd -= HideBubble;
        }
    }

    private void Update()
    {
        // 대화 중에 F키를 누르면 다음 대사로 진행
        if (_dialogueService.IsDialogueActive && Input.GetKeyDown(KeyCode.F))
        {
            _dialogueService.ProceedToNextLine();
        }
    }

    private void ShowLineInBubble(Dialogue dialogue, GameObject speaker)
    {
        if (speaker == null) return;

        // 이전에 활성화된 말풍선이 있다면 숨김 처리
        if (_activeBubble != null) _activeBubble.Hide();

        var bubble = speaker.GetComponentInChildren<NPCBubbleUI>(true);
        if (bubble != null)
        {
            _activeBubble = bubble;
            // 화자 이름과 대사를 합쳐서 표시
            string fullText = $"{dialogue.speaker}: {dialogue.text}";
            _activeBubble.ShowMessage(fullText);
        }
    }

    private void HideBubble()
    {
        if (_activeBubble != null)
        {
            _activeBubble.Hide();
            _activeBubble = null;
        }
    }
}