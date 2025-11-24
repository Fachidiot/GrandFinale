using TMPro;
using UnityEngine;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;

    public void Setup(PlayerInfo player, bool isHost)
    {
        if (playerNameText.text != player.nickname)
            playerNameText.text = isHost ? $"{player.nickname} (Host)" : $"{player.nickname}";
    }
}
