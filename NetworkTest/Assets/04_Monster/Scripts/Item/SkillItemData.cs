using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillItem", menuName = "Item/Skill")]
public class SkillItemData : ItemData
{
    //실제 입력으로 사용하는것 / 패시브 스킬 따로 종류 나눠야함.

    [Header("스킬 설정")]
    [Tooltip("사용 시 생성될 프리팹 (불덩이, 얼음 송곳 등)")]
    public GameObject skillPrefab;
    public float cooldown;
    public float manaCost;

/*    public override ItemType GetItemType()
    {
        return ItemType.Skill;
    }*/
}