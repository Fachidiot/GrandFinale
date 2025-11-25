using UnityEngine;
public enum ItemType
{
    Weapon, //무기
    Artifact, //유물
    Accessory, // 장신구
    Equipment, // 장비

    StatBoost,
    Skill,
    Material,
    Etc
}

public enum InventoryFilterType
{
    All,        // 전체
    Weapon,     // 무기
    Equipment,  // 장비
    Accessory,  // 장신구
    Relic,      // 유물
    Etc         // 기타
}

public enum WeaponType
{
    None,       // 무기가 아님 (방어구, 소모품 등)
    Rifle,      // 라이플 (돌격소총) - 슬롯 1, 2
    Sniper,     // 스나이퍼 - 슬롯 1, 2
    Shotgun,    // 샷건 - 슬롯 1, 2
    Melee,      // 근접 무기 - 슬롯 1, 2
    SMG,        // 기관단총 - 슬롯 3 전용
    Pistol      // 권총 - 슬롯 4 전용
}

public enum EquipmentSlot
{
    None,     // 장착 부위 없음 (재료, 유물, 무기 등)
    Head,
    Face,
    Necklace,
    Armor,
    Pants,
    Shoes
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

    [Header("Weapon Details")]
    public WeaponType weaponType;

    [Header("Equipment Info")]
    public EquipmentSlot equipmentSlot;
}