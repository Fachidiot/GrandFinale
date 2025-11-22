using UnityEngine;

public class SimpleTalkModule : MonoBehaviour
{
    [SerializeField] private BarkLinesSO barkData;
    [SerializeField] private NPCBubbleUI bubbleUI;

    private void Awake()
    {
        if (!bubbleUI) bubbleUI = GetComponentInChildren<NPCBubbleUI>(true);
    }

    public void PlayRandomTalk()
    {
        if (bubbleUI && barkData != null && barkData.lines.Length > 0)
        {
            string line = barkData.lines[Random.Range(0, barkData.lines.Length)];
            bubbleUI.ShowMessage(line);
        }
    }
}