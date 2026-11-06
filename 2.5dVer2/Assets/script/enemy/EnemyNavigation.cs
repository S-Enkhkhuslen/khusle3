using UnityEngine;
using UnityEngine.AI;

// Shared ground movement; each enemy keeps its own attack behaviour.
public static class EnemyNavigation
{
    public static float Distance(Transform enemy, Transform target)
    {
        Vector3 offset = target.position - enemy.position; offset.y = 0f; return offset.magnitude;
    }
    public static void Face(Transform enemy, Transform target, float speed)
    {
        Vector3 direction = target.position - enemy.position; direction.y = 0f;
        if (direction.sqrMagnitude < 0.000001f) return;
        enemy.rotation = Quaternion.Slerp(enemy.rotation, Quaternion.LookRotation(direction), speed * Time.deltaTime);
    }
    public static void Chase(NavMeshAgent agent, Vector3 target)
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        // Use the player's actual level; flattening Y loses downhill targets.
        if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 5f, filter))
        {
            // A flying target may be far above the nearest walkable ground.
            if (!GroundUtility.TrySample(target, 1 << 3, out Vector3 ground)
                || !NavMesh.SamplePosition(ground, out hit, 5f, filter)) return;
        }
        agent.isStopped = false; agent.SetDestination(hit.position);
    }
    public static void Stop(NavMeshAgent agent)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
    }
}
