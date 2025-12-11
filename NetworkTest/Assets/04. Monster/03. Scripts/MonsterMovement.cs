using UnityEngine;
using UnityEngine.AI;

public class MonsterMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    public Transform target;
    public float stoppingDistance = 1.5f; // 몬스터가 멈출 거리 설정

    private bool isHost;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // This component should only be active on the host.
        isHost = NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.Host;
        if (!isHost)
        {
            // Disable the NavMeshAgent on clients to prevent movement conflicts.
            // The NetworkMonsterTransformSync script will handle positioning.
            if (agent != null)
            {
                agent.enabled = false;
            }
            return;
        }
        
        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
        }
    }

    void Update()
    {
        // Only the host calculates the monster's path.
        if (isHost && target != null && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(target.position);
        }
    }
}