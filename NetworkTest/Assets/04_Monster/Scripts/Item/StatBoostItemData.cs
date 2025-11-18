using UnityEngine;

// [CreateAssetMenu]를 사용해, 유니티 에디터에서 "Create > Item > Stat Boost" 메뉴를 만듭니다.
[CreateAssetMenu(fileName = "NewStatBoostItem", menuName = "Item/Stat Boost")]
public class StatBoostItemData : ItemData
{
    [Header("스탯 상승치")]
    public float attackPowerIncrease;
    public float maxHealthIncrease;
    public float moveSpeedIncrease;

    // 방어력, 쿨다운 감소 등 원하는 스탯 추가

/*    public override ItemType GetItemType()
    {
        return ItemType.StatBoost;
    }*/
}