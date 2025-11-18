using UnityEngine;

[RequireComponent(typeof(Spider))]
public class SpiderMovementAdapter : MonoBehaviour, IMonsterMovement
{
    private Spider spider;

    void Awake()
    {
        spider = GetComponent<Spider>();
    }

    // 'Transform context' 파라미터가 없습니다.
    public void Move(Vector3 worldDirection, float speed)
    {
        // 자기 자신의 transform을 사용합니다.
        Vector3 localDirection = transform.InverseTransformDirection(worldDirection);
        spider.walk(localDirection, speed);
    }

    // 'Transform context' 파라미터가 없습니다.
    public void TurnTowards(Vector3 worldTargetPosition, float turnSpeed)
    {
        // 자기 자신의 transform을 사용합니다.
        Vector3 worldDirection = (worldTargetPosition - transform.position).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(worldDirection);
        spider.turn(localDirection);
    }

    public void Stop()
    {
        spider.walk(Vector3.zero, 0f);
    }
}