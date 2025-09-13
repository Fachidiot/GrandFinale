using TMPro;
using UnityEngine;

public class HealthPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;

    public void SetHealthValue(float value)
    {
        if (value <= 0)
        {
            healthText.text = "0";
        }
        else
        {
            healthText.text = value.ToString();
        }
    }
}
