using UnityEngine;

public class SimpleTalkModule : MonoBehaviour
{
    [SerializeField] private BarkLinesSO barkData;
    [SerializeField] private NPCBubbleUI bubbleUI;
    [SerializeField] private bool isRandom = true;

    private void Awake()
    {
        if (!bubbleUI) bubbleUI = GetComponentInChildren<NPCBubbleUI>(true);
    }

    private int talkindex = 0;
    public void PlayRandomTalk()
    {
        if (isRandom)
        {
            if (bubbleUI && barkData != null && barkData.lines.Length > 0)
            {
                string line = barkData.lines[Random.Range(0, barkData.lines.Length)];
                bubbleUI.ShowMessage(line);
            }
        }
        else
        {
            if (bubbleUI && barkData != null && barkData.lines.Length > 0)
            {
                if (talkindex >= barkData.lines.Length)
                    talkindex = 0;
                string line = barkData.lines[talkindex];
                bubbleUI.ShowMessage(line);
                talkindex++;
            }
        }
    }
}