using UnityEngine;
using System.Collections.Generic;

// 이 에셋은 'Monster_v1/Data' 폴더에 하나만 만들 것입니다.
[CreateAssetMenu(fileName = "MasterDatabase", menuName = "Monster_v1/Master Database")]
public class MasterDatabase : ScriptableObject
{
    // 파서가 이 리스트들을 자동으로 채워줄 것입니다.
    public List<AbilityData> allAbilities;
    public List<RelicData> allRelics;
}