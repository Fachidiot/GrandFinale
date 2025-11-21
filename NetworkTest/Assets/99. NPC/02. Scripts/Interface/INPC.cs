//NPC 공통 인터페이스
public enum NPCKind
{
    Companion,  //전투지원
    WanderShop, //떠돌이 상점
    SimpleTalk, //대화용
    Monster,    //쓸진모르겠는데 일단 몬스터
    QuestGiver, //퀘스트
    Event   
}

public interface INPC
{
    string Id { get; }
    NPCKind Kind { get; }   // 열거형을 프로퍼티로 노출
}
