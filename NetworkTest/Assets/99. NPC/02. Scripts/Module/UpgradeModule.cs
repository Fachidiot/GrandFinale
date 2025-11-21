using UnityEngine;

public class UpgradeModule : MonoBehaviour
{
    [SerializeField] private GameObject upgradeCanvas;

    private bool isOpen = false;

    private void Start()
    {
        if (upgradeCanvas) upgradeCanvas.SetActive(false);
    }

    public void ToggleUI()
    {
        isOpen = !isOpen;
        if (upgradeCanvas)
        {
            upgradeCanvas.SetActive(isOpen);

            // 마우스 커서 처리
            Cursor.visible = isOpen;
            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }
        Debug.Log($"[Upgrade] UI {(isOpen ? "Opened" : "Closed")}");
    }
}