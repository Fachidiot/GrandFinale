using UnityEngine;

// ScriptableObject: 에셋 파일(.asset) 형태로 존재할 수 있는 데이터 컨테이너입니다.
[CreateAssetMenu(fileName = "NewBarkLines", menuName = "NPC/BarkLines")]
public class BarkLinesSO : ScriptableObject // ★ ScriptableObject 상속이 필수!
{
    [TextArea(3, 5)]
    public string[] lines; // ★ CSV 파서가 이 배열에 대사들을 채워넣을 겁니다.
}