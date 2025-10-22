using UnityEngine;
using UnityEngine.AI;

public class MonsterMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    public Transform target;
    public float stoppingDistance = 1.5f; // 몬스터가 멈출 거리 설정

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
        }
    }

    void Update()
    {
        if (target != null && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(target.position);
        }
    }
}