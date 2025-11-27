using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SandbagMachine : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpScore;
    [SerializeField] private float limitTime = 60;
    [SerializeField] private float coolDown = 2;

    private bool isStart = false;
    private float startTime = 0;
    private float coolTime = 0;
    private int score;
    private int bestScore;

    void Start()
    {
        Reset();
    }

    void Update()
    {
        if (!isStart)
            return;

        if (startTime + limitTime < Time.time)
        {
            Debug.Log("게임 종료");
            if (score > bestScore)
                bestScore = score;
            Reset();
            isStart = false;
            score = 0;
        }
    }

    public void Punching()
    {
        if (coolTime + coolDown > Time.time)
            return;
        if (!isStart)
        {
            isStart = true;
            startTime = Time.time;
            tmpScore.text = "게임 시작";
        }
        else
        {
            ++score;
            tmpScore.text = $"스코어\n{score}";
        }
    }

    void Reset()
    {
        tmpScore.text = $"최고점수\n{bestScore}";
        coolTime = Time.time;
    }
}
