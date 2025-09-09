using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;

    private int roomId;

    public void Setup(RoomInfo room)
    {
        this.roomId = room.room_id;
        roomNameText.text = room.room_name;
        playerCountText.text = $"{room.player_count} / 4"; // Assuming max players is 4

        GetComponent<Button>().onClick.AddListener(() =>
        {
            UIManager.Instance.JoinRoomById(this.roomId);
        });
    }
}
