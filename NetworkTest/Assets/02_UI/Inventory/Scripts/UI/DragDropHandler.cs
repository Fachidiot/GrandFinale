using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 드래그 & 드롭 처리 전담 컨트롤러
/// - 아이템 드래그 시각화
/// - 패널 드래그 이동
/// - 드래그 아이콘 관리
/// </summary>
public class DragDropHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    #region Serialized Fields

    [Header("Drag Icons")]
    [SerializeField] private Image smallDragIcon;
    [SerializeField] private Image fullDragIcon;

    [Header("Panel References")]
    [SerializeField] private GameObject smallInventoryPanel;
    [SerializeField] private GameObject fullInventoryPanel;
    [SerializeField] private RectTransform smallPanelRect;
    [SerializeField] private RectTransform smallHeaderBarRect;

    #endregion

    #region Private Fields

    private Vector2 dragOffset;
    private Image currentActiveDragIcon;

    #endregion

    #region IBeginDragHandler Implementation

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Small Inventory 헤더바 드래그만 허용
        if (smallPanelRect != null && smallHeaderBarRect != null)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                smallHeaderBarRect, eventData.position, eventData.pressEventCamera))
            {
                CalculateDragOffset(eventData);
            }
        }
    }

    #endregion

    #region IDragHandler Implementation

    public void OnDrag(PointerEventData eventData)
    {
        if (smallPanelRect != null)
        {
            UpdatePanelPosition(eventData);
        }
    }

    #endregion

    #region Public API - Item Drag

    /// <summary>
    /// 아이템 드래그 시작
    /// </summary>
    public void StartItemDrag(Sprite iconSprite)
    {
        Image dragIcon = GetActiveDragIcon();
        if (dragIcon == null) return;

        dragIcon.sprite = iconSprite;
        dragIcon.gameObject.SetActive(true);
        currentActiveDragIcon = dragIcon;
    }

    /// <summary>
    /// 드래그 아이콘 위치 업데이트
    /// </summary>
    public void UpdateDragIconPosition(Vector2 screenPosition)
    {
        Image dragIcon = GetActiveDragIcon();
        if (dragIcon != null)
        {
            dragIcon.rectTransform.position = screenPosition;
        }
    }

    /// <summary>
    /// 아이템 드래그 종료
    /// </summary>
    public void EndItemDrag()
    {
        if (smallDragIcon != null)
        {
            smallDragIcon.gameObject.SetActive(false);
        }

        if (fullDragIcon != null)
        {
            fullDragIcon.gameObject.SetActive(false);
        }

        currentActiveDragIcon = null;
    }

    #endregion

    #region Private Methods - Panel Drag

    private void CalculateDragOffset(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            smallPanelRect,
            eventData.position,
            eventData.pressEventCamera,
            out dragOffset
        );
    }

    private void UpdatePanelPosition(PointerEventData eventData)
    {
        RectTransform parentRect = smallPanelRect.parent as RectTransform;
        if (parentRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition))
        {
            smallPanelRect.anchoredPosition = localPointerPosition - dragOffset;
        }
    }

    #endregion

    #region Private Methods - Drag Icon

    private Image GetActiveDragIcon()
    {
        // 이미 활성화된 아이콘이 있으면 재사용
        if (currentActiveDragIcon != null)
        {
            return currentActiveDragIcon;
        }

        // 현재 활성화된 인벤토리에 따라 선택
        if (IsInventoryActive(fullInventoryPanel))
        {
            return fullDragIcon;
        }
        else if (IsInventoryActive(smallInventoryPanel))
        {
            return smallDragIcon;
        }

        return null;
    }

    private bool IsInventoryActive(GameObject panel)
    {
        return panel != null && panel.activeSelf;
    }

    #endregion
}