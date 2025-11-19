using UnityEngine;

/// <summary>
/// 이 스크립트가 부착된 오브젝트를 위아래로 부드럽게 흔듭니다.
/// </summary>
public class HoverBob : MonoBehaviour
{
    [Tooltip("흔들리는 속도")]
    public float bobSpeed = 2f;
    [Tooltip("흔들리는 최대 높이")]
    public float bobAmount = 0.1f;

    private float startY; // 오브젝트의 원래 Y 위치

    void Start()
    {
        // 이 오브젝트의 '로컬' Y 위치를 기억
        startY = transform.localPosition.y;
    }

    void Update()
    {
        // Sin 함수를 이용해 -1 ~ +1 사이의 값을 만듦
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;

        // 원래 Y 위치에 흔들리는 값을 더함
        transform.localPosition = new Vector3(
            transform.localPosition.x,
            startY + bobOffset,
            transform.localPosition.z
        );
    }
}