using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

public class SynthesisRoulette : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    #region Serialized Fields
    [Header("Settings")]
    public float radius = 250f;
    public float rotationSpeed = 0.5f;
    public float friction = 0.95f;
    #endregion

    #region Private Fields
    private List<RectTransform> items = new List<RectTransform>();
    private float currentRotationVelocity = 0f;
    private bool isDragging = false;
    #endregion

    #region Unity Lifecycle
    void Update()
    {
        if (!isDragging && Mathf.Abs(currentRotationVelocity) > 0.01f)
        {
            transform.Rotate(Vector3.back, currentRotationVelocity * Time.deltaTime);
            currentRotationVelocity *= friction;
        }

        foreach (var item in items)
        {
            if (item != null) item.rotation = Quaternion.identity;
        }
    }
    #endregion

    #region Layout & Animation
    public void RefreshLayout(List<SynthesisNode_UI> nodes)
    {
        items.Clear();
        if (nodes.Count == 0) return;

        float angleStep = 360f / nodes.Count;
        float startAngle = 0f;

        for (int i = 0; i < nodes.Count; i++)
        {
            RectTransform itemRect = nodes[i].GetComponent<RectTransform>();
            items.Add(itemRect);

            itemRect.SetParent(transform);
            itemRect.localScale = Vector3.one;

            float angle = startAngle - (i * angleStep);
            float rad = (angle + 90) * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;

            itemRect.anchoredPosition = new Vector2(x, y);
        }
    }

    public void PlayConvergenceAnimation(System.Action onComplete)
    {
        if (items == null || items.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        // 시퀀스 생성
        Sequence seq = DOTween.Sequence();

        // 1. 룰렛 회전
        seq.Join(transform.DORotate(new Vector3(0, 0, -720), 1.0f, RotateMode.FastBeyond360)
            .SetEase(Ease.InCubic));

        // 2. 아이템 중앙 이동 및 축소
        bool hasValidItem = false;
        foreach (var item in items)
        {
            if (item != null)
            {
                hasValidItem = true;
                seq.Join(item.DOAnchorPos(Vector2.zero, 1.0f).SetEase(Ease.InBack));
                seq.Join(item.DOScale(0f, 1.0f).SetEase(Ease.InBack));
            }
        }

        if (!hasValidItem)
        {
            onComplete?.Invoke();
            return;
        }

        // 3. 완료 콜백 연결
        seq.OnComplete(() => {
            onComplete?.Invoke();
            transform.rotation = Quaternion.identity; // 회전 초기화
        });

        // 실행
        seq.Play();
    }
    #endregion

    #region Drag Handlers
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        currentRotationVelocity = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float delta = eventData.delta.x + eventData.delta.y;
        transform.Rotate(Vector3.back, -delta * rotationSpeed);
        currentRotationVelocity = -delta * rotationSpeed * 50f;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
    #endregion
}