using TMPro;
using UnityEngine;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI readyStatusText;

    public void Setup(PlayerInfo player, bool isHost)
    {
        if (playerNameText.text != player.nickname)
            playerNameText.text = isHost ? $"{player.nickname} (Host)" : $"{player.nickname}";

        if (isHost)
        {
            readyStatusText.text = "---";
            readyStatusText.color = Color.yellow;
        }
        else
        {
            readyStatusText.text = player.is_ready ? "Ready" : "Not Ready";
            readyStatusText.color = player.is_ready ? Color.green : Color.white;
        }
    }
}
