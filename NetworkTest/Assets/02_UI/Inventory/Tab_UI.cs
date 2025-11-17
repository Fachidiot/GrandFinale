using UnityEngine;
using UnityEngine.UI;

public class Tab_UI : MonoBehaviour
{
    public InventoryFilterType filterType;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnTabClicked);
        }
    }

    // 탭 클릭 시 필터 적용
    private void OnTabClicked()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetFilter(filterType);
        }
    }
}