using System;
using TMPro;
using UnityEngine;

public class SelectLevel : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private Difficulty difficulty = Difficulty.Easy;

    public void Prev()
    {
        if (Difficulty.Easy == difficulty)
            return;
        --difficulty;
        ChangeText();
    }

    public void Next()
    {
        if (Difficulty.God == difficulty)
            return;
        ++difficulty;
        ChangeText();
    }

    private void ChangeText()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                tmpText.text = "생존 초보자";
                break;
            case Difficulty.Normal:
                tmpText.text = "생존 입문자";
                break;
            case Difficulty.Difficult:
                tmpText.text = "생존 중급자";
                break;
            case Difficulty.Hard:
                tmpText.text = "생존 숙련자";
                break;
            case Difficulty.God:
                tmpText.text = "생존 전문가";
                break;
        }
    }
}

[Serializable]
public enum Difficulty
{
    None,
    Easy,
    Normal,
    Difficult,
    Hard,
    God
}
