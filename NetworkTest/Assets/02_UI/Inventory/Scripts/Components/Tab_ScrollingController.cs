// (선택사항) Tab_ScrollingController.cs
using UnityEngine;
using UnityEngine.UI;

public class Tab_ScrollingController : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private float scrollSpeed = 0.5f;

    // L1: 왼쪽으로 스크롤 버튼이 누를 때 호출될 함수
    public void ScrollLeft()
    {
        // 현재 스크롤 위치에서 1페이지(뷰포트 너비)만큼 왼쪽으로 이동
        float target = scrollRect.horizontalNormalizedPosition - scrollSpeed;
        target = Mathf.Clamp01(target);
        scrollRect.StopMovement(); // 움직임을 멈추고
        scrollRect.horizontalNormalizedPosition = target; // 즉시 이동 (SmoothDamp 대신)
    }

    // L2: 오른쪽으로 스크롤 버튼이 누를 때 호출될 함수
    public void ScrollRight()
    {
        // 현재 스크롤 위치에서 1페이지(뷰포트 너비)만큼 오른쪽으로 이동
        float target = scrollRect.horizontalNormalizedPosition + scrollSpeed;
        target = Mathf.Clamp01(target);
        scrollRect.StopMovement();
        scrollRect.horizontalNormalizedPosition = target;
    }
}