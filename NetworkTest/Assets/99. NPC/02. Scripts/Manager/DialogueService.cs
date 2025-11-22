using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueService
{
    // 싱글턴 패턴: 게임 내 어디서든 이 서비스에 쉽게 접근할 수 있도록 함
    private static DialogueService _instance;
    public static DialogueService Instance => _instance ?? (_instance = new DialogueService());

    // ===== 이벤트 (UI나 다른 시스템에 알림을 보내는 방송 채널) =====
    // 대사를 표시할 때, 대사 내용과 함께 누가 말하는지에 대한 정보(GameObject)도 함께 보냄
    public event Action<Dialogue, GameObject> OnLineStart;
    public event Action OnDialogueEnd;    // 대화 전체가 끝났다고 알림

    // ===== 상태 변수 =====
    public bool IsDialogueActive { get; private set; }
    private DialogueEvent _currentEvent;
    private int _currentLineIndex;
    private GameObject _currentSpeaker;

    /// <summary>
    /// 대화를 시작하는 외부 진입점 (DialogueModule이 호출)
    /// </summary>
    public void StartDialogue(DialogueEvent eventData, GameObject speaker)
    {
        if (eventData == null || eventData.lines.Count == 0) return;
        if (IsDialogueActive) return; // 대화가 이미 진행 중이면 중복 실행 방지

        IsDialogueActive = true;
        _currentEvent = eventData;
        _currentSpeaker = speaker;
        _currentLineIndex = 0;

        ShowCurrentLine();
    }

    /// <summary>
    /// 다음 대사로 진행 (DialogueUI가 플레이어 입력을 받으면 호출)
    /// </summary>
    public void ProceedToNextLine()
    {
        if (!IsDialogueActive) return;
        _currentLineIndex++;

        if (_currentLineIndex < _currentEvent.lines.Count)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void ShowCurrentLine()
    {
        OnLineStart?.Invoke(_currentEvent.lines[_currentLineIndex], _currentSpeaker);
    }

    private void EndDialogue()
    {
        IsDialogueActive = false;
        OnDialogueEnd?.Invoke();
        _currentEvent = null;
        _currentSpeaker = null;
    }
}