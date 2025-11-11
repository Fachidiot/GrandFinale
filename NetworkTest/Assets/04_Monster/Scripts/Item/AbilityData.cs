// Assets/Scripts/Data/AbilityData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "ABIL_", menuName = "Data/Ability")]
public class AbilityData : ScriptableObject
{
    [Header("시트 원본 정보")]
    public string abilityID;
    public string abilityName;
    public string activationType; // Passive, Active
    public string abilityLogicID; // Stat_Add, Projectile, Aura...

    [Header("능력 파라미터 (Key, Value A, B, C)")]
    public string param_Key;    // 스탯명("MaxHealth"), 주 수치("25"), 타입("InfiniteAmmo") 등
    public string param_ValueA; // 보조 수치 1 (범위, 쿨타임 등)
    public string param_ValueB; // 보조 수치 2 (지속시간, 속도 등)
    public string param_ValueC; // 보조 수치 3 (기타, 틱레이트 등)

    [Header("리소스 경로")]
    public string resourcePath; // Prefabs/VFX/HealAura
}