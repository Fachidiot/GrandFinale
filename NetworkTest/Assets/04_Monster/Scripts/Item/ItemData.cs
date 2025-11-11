using UnityEngine;
public enum ItemType
{
    // 장비 슬롯에 장착 가능한 아이템
    Weapon,
    Artifact,

    // 단순 소모품 또는 기타
    StatBoost,
    Skill,
    Material,
    Etc
}

[CreateAssetMenu(fileName = "New ItemData", menuName = "Item/Data", order = 1)]

public class ItemData : ScriptableObject
{
    // 모든 아이템이 공통으로 가질 데이터
    public string itemID;       // 아이템 고유 ID (예: "WEP_001")
    public string itemName;     // 아이템 이름 (예: "강철 검")
    public Sprite itemIcon;     // 인벤토리 등에서 보여줄 아이콘

    [TextArea(3, 10)] // 인스펙터에서 여러 줄로 입력 가능하게 함
    public string description;  // 아이템 설명

    public int maxStack;        // 최대 겹치기 수 (예: 포션은 99개, 무기는 1개)
    public int price;           // 상점 가격

    [Header("Item Type")]
    public ItemType itemType;
}