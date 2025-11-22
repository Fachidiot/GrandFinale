using UnityEngine;

// 1. NPC 전용 상호작용 인터페이스
public interface INpcInteractable
{
    string GetPrompt();                     
    void OnInteract(GameObject interactor); 
}

// 2. NPC 종류
public enum NPCKind
{
    SimpleTalk, // 대화형
    Shop,       // 상점형
    Upgrade     // 업그레이드형
}

// 3. NPC 데이터 인터페이스
public interface INPC
{
    string Id { get; }
    NPCKind Kind { get; }
}