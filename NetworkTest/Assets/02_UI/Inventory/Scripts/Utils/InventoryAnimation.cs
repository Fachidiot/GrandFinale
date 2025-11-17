using UnityEngine;
using DG.Tweening;

public class InventoryAnimation : MonoBehaviour
{
    // 유니티 인스펙터에서 설정할 변수들
    [SerializeField] private RectTransform _inventoryPanel;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _openDuration = 0.5f;
    [SerializeField] private float _startScale = 0.8f; // 시작 크기 (원래 크기 1.0f보다 작게)

    public void OpenInventory()
    {
        // 1. 초기 상태 설정 (닫혀있을 때)
        _canvasGroup.alpha = 0f;
        _inventoryPanel.localScale = Vector3.one * _startScale;

        // 2. 애니메이션 적용
        _canvasGroup.DOFade(1f, _openDuration);
        _inventoryPanel.DOScale(1f, _openDuration)
                       .SetEase(Ease.OutBack); // 튕기는 듯한 효과
    }

    public void CloseInventory()
    {
        // 닫을 때는 부드럽게 작아지면서 사라집니다.
        _canvasGroup.DOFade(0f, _openDuration);
        _inventoryPanel.DOScale(Vector3.one * _startScale, _openDuration)
                       .SetEase(Ease.InBack); // 들어가는 듯한 효과
    }
}