// SpiderMovement.cs
using UnityEngine;

[RequireComponent(typeof(Spider))]
public class SpiderMovement : MonoBehaviour, IMonsterMovement
{
    private Spider spider;

    private void Awake()
    {
        spider = GetComponent<Spider>();
    }

    // AI에게 받은 목적지를 거미가 필요한 방향으로 번역
    public void Move(Vector3 destination, float speed)
    {
        // 목적지까지의 월드 공간 방향을 계산합니다.
        Vector3 worldDirection = (destination - transform.position).normalized;

        // 월드 방향을 거미의 로컬 방향으로 변환하여 전달합니다.
        Vector3 localDirection = transform.InverseTransformDirection(worldDirection);
        spider.walk(localDirection, speed);
    }

    public void TurnTowards(Vector3 worldTargetPosition, float turnSpeed)
    {
        Vector3 worldDirection = (worldTargetPosition - transform.position).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(worldDirection);
        spider.turn(localDirection);
    }

    public void Stop()
    {
        spider.walk(Vector3.zero, 0);
    }
}