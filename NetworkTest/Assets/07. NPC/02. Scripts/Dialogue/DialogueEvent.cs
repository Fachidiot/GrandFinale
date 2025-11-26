using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 대사 라인을 담는 대화 이벤트 클래스 (ScriptableObject)
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueEvent", menuName = "Dialogue/DialogueEvent")]
public class DialogueEvent : ScriptableObject
{
    public string eventName;              // 이벤트 이름 (예: intro_1)
    public List<Dialogue> lines;          // 이 이벤트에 포함된 모든 대사
}
