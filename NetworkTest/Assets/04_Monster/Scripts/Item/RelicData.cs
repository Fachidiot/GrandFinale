// Assets/Scripts/Data/RelicData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "REL_", menuName = "Data/Relic")]
public class RelicData : ScriptableObject
{
    [Header("시트 원본 정보")]
    public string itemID;
    public string itemName;
    public ItemType itemTypeEnum;
    public EquipmentSlot equipmentSlot;

    public string grade;
    public int maxStack;

    [TextArea(3, 5)]
    public string description;

    public int price;

    [Header("리소스 경로")]
    public string iconPath; // Icons/Relics/AuxHeart

    [Header("★핵심 연동★")]
    public AbilityData grantedAbility;
}