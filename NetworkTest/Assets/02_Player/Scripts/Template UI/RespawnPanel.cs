using TMPro;
using UnityEngine;

public class RespawnPanel : MonoBehaviour
{
    public GameObject respawnPanelObject;
    [SerializeField] private TMP_Text respawnText;

    public void SetRespawnTimeText(float value)
    {
        if (value <= 0)
        {
            respawnText.text = "Respawn 0.00";
        }
        else
        {
            respawnText.text = "Respawn " + value.ToString();
        }
    }
}
