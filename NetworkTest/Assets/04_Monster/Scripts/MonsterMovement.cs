using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

public class MonsterMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform currentTarget;
    public float stoppingDistance = 1.5f; // The distance at which the monster will stop

    private const float TargetUpdateRate = 1.0f; // How often to check for the nearest player (in seconds)

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
        }
    }

    void Start()
    {
        // AI logic should only run on the host
        if (NetworkManager.Instance.Mode != NetworkMode.Host)
        {
            this.enabled = false;
            return;
        }

        StartCoroutine(UpdateTargetCoroutine());
    }

    void Update()
    {
        if (currentTarget != null && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(currentTarget.position);
        }
    }

    private IEnumerator UpdateTargetCoroutine()
    {
        while (true)
        {
            FindNearestPlayer();
            yield return new WaitForSeconds(TargetUpdateRate);
        }
    }

    private void FindNearestPlayer()
    {
        if (NetworkPlayerManager.Instance == null || NetworkPlayerManager.Instance.Players.Count == 0)
        {
            currentTarget = null;
            return;
        }

        float closestDistance = float.MaxValue;
        Transform nearestPlayer = null;

        foreach (var player in NetworkPlayerManager.Instance.Players.Values)
        {
            if (player == null) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestPlayer = player.transform;
            }
        }

        currentTarget = nearestPlayer;
    }
}