using UnityEngine;
using TMPro;

public class PlayerNicknameUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nicknameText;

    [SerializeField] private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
        else
        {
            mainCamera = Camera.main;
        }
    }

    public void SetNickname(string nickname)
    {
        if (nicknameText != null)
        {
            nicknameText.text = nickname;
        }
        else
        {
            Debug.LogError("NicknameText reference is not set in PlayerNicknameUI.", this);
        }
    }
}
