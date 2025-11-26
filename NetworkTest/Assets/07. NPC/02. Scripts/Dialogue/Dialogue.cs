using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 하나의 대사 정보를 담는 클래스
/// </summary>

[System.Serializable]
public class Dialogue
{
    public int id;                 // 대사의 고유 ID
    public string speaker;         // 말하는 캐릭터 이름
    [TextArea] public string text; // 대사 내용
    public string choices;         // 선택지 텍스트: "선택지1:3,선택지2:4"
    public int nextId;             // 다음 대사 ID (선택지 없을 때)
    public string tag;             // 대사에 포함된 태그 (WAIT, FLAG_ 등)
    public string type;            // System, Player 등

}
