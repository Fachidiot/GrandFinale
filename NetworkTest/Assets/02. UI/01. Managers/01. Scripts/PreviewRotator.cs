using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PreviewRotator : MonoBehaviour, IDragHandler
{
    [Header("Settings")]
    [Tooltip("회전시킬 3D 모델의 Transform을 연결하세요.")]
    [SerializeField] private CustomizeManager customizeManager;

    [Tooltip("회전 속도 (민감도)")]
    [SerializeField] private float rotationSpeed = 0.5f;

    [Tooltip("드래그 방향 반대로 회전하려면 체크")]
    [SerializeField] private bool invertDirection = true;

    // IDragHandler 인터페이스 구현
    public void OnDrag(PointerEventData eventData)
    {
        if (customizeManager == null)
            return;

        // 드래그한 가로 길이(delta.x)를 가져옴
        float x = eventData.delta.x;

        // 회전 방향 결정
        float direction = invertDirection ? -1 : 1;

        // Y축 기준으로 회전 적용 (Space.World 또는 Space.Self 선택 가능, 보통 모델 뷰어는 World 기준이 자연스러움)
        customizeManager.GetCurrentModel().transform.parent.transform.Rotate(Vector3.up, x * rotationSpeed * direction, Space.World);
    }
}
