using UnityEngine;
using System.Collections.Generic;

public static class SynthesisProbabilityTable
{
    public static int GetTier(string grade) => grade switch
    {
        "Common" => 0,
        "Uncommon" => 1,
        "Rare" => 2,
        "Epic" => 3,
        "Legendary" => 4,
        "Mythic" => 5,
        _ => -1
    };

    public static string GetGradeString(int tier) => tier switch
    {
        0 => "Common",
        1 => "Uncommon",
        2 => "Rare",
        3 => "Epic",
        4 => "Legendary",
        5 => "Mythic",
        _ => "Unknown"
    };

    public static (float fail, float next, float jackpot) GetProbabilities(string currentGrade, int relicCount, List<RelicData> boosters)
    {
        int tier = GetTier(currentGrade);
        if (tier == -1 || tier >= 5) return (0, 0, 0);

        float next = 0, jackpot = 0;

        // 1. 유물 개수별 기본 확률
        if (tier < 4) // Common ~ Epic
        {
            if (relicCount == 1) { next = 35; jackpot = 5; }
            else if (relicCount == 2) { next = 60; jackpot = 10; }
            else if (relicCount == 3) { next = 85; jackpot = 15; }
            else if (relicCount >= 4) { next = 80; jackpot = 20; }
        }
        else if (tier == 4) // Legendary
        {
            if (relicCount == 1) next = 40;
            else if (relicCount == 2) next = 70;
            else if (relicCount >= 3) next = 100;
        }

        // 2. 재료 보너스
        foreach (var mat in boosters)
        {
            switch (mat.grade)
            {
                case "Common": next += 5f; jackpot += 1f; break;
                case "Rare": next += 10f; jackpot += 3f; break;
                case "Epic": next += 15f; jackpot += 5f; break;
            }
        }

        float totalSuccess = Mathf.Min(next + jackpot, 100f);
        if (jackpot > 100) jackpot = 100;
        next = totalSuccess - jackpot;

        return (100f - totalSuccess, next, jackpot);
    }
}