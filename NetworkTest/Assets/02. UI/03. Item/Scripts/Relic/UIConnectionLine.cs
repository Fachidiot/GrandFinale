using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIConnectionLine : MaskableGraphic
{
    [Header("Curve Settings")]
    public Transform startTarget;
    public Transform endTarget;

    // parentCanvas 변수는 이제 필요 없음 (자동 계산)

    public int segments = 20;
    public float sagAmount = 50f;
    public float thickness = 5f;

    void Update()
    {
        // 타겟이 움직이면 매 프레임 다시 그리기 요청
        if (startTarget != null && endTarget != null)
        {
            SetVerticesDirty();
        }
    }

    public void SetTargets(Transform start, Transform end, Transform parent)
    {
        startTarget = start;
        endTarget = end;
        // parent는 더 이상 안 씀
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (startTarget == null || endTarget == null) return;

        // [핵심 수정] 복잡한 스크린 좌표 변환 다 버림
        // 그냥 내 위치 기준으로 상대방이 어디 있는지 계산 (가장 정확함)
        Vector2 startPos = transform.InverseTransformPoint(startTarget.position);
        Vector2 endPos = transform.InverseTransformPoint(endTarget.position);

        // --- 베지에 곡선 계산 ---
        Vector2 mid = (startPos + endPos) / 2f;
        float distance = Vector2.Distance(startPos, endPos);

        // 거리가 멀수록 축 처짐
        Vector2 sag = new Vector2(0, -(sagAmount + (distance * 0.05f)));
        Vector2 controlPoint = mid + sag;

        List<Vector2> points = new List<Vector2>();
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            points.Add(CalculateQuadraticBezierPoint(t, startPos, controlPoint, endPos));
        }

        // --- 선 두께 만들기 ---
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];

            Vector2 dir = (p2 - p1).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * thickness * 0.5f;

            UIVertex v1 = CreateUIVertex(p1 - normal);
            UIVertex v2 = CreateUIVertex(p1 + normal);
            UIVertex v3 = CreateUIVertex(p2 + normal);
            UIVertex v4 = CreateUIVertex(p2 - normal);

            vh.AddUIVertexQuad(new UIVertex[] { v1, v2, v3, v4 });
        }
    }

    private UIVertex CreateUIVertex(Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color; // 인스펙터에서 설정한 색상 사용
        return vertex;
    }

    private Vector2 CalculateQuadraticBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }
}