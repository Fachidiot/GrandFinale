// IMonsterMovement.cs
using UnityEngine;

public interface IMonsterMovement
{
    // 방향이 아닌 최종 목적지를 명령하도록 변경
    void Move(Vector3 destination, float speed);
    void TurnTowards(Vector3 worldTargetPosition, float turnSpeed);
    void Stop();
}