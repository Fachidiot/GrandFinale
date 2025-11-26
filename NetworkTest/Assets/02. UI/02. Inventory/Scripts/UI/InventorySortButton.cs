using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InventorySortButton : MonoBehaviour
{
    private Button btn;

    [Header("Animation Settings")]
    [SerializeField] private float punchScale = 0.15f; // 클릭 시 크기 변화

    void Awake()
    {
        btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnSortClicked);
        }
    }

    private void OnSortClicked()
    {
        // 1. 매니저 정렬 호출
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SortItems();
        }

        // 2. 사운드 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.uiClickClip);
        }

        // 띠용 효과
        transform.DOPunchScale(Vector3.one * punchScale, 0.3f, 10, 1);

    }
}