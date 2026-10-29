using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(50)]
public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float automaticPatrolDistance = 8f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtPoint = 1f;

    private NavMeshAgent agent;
    private EnemySensor sensor;
    private Enemy enemy;
    private Animator animator;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool goingToB = true;
    private float waitUntil;
    private float combatSpeed;
    private float combatStoppingDistance;

    public bool IsPatrolling { get; private set; }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<EnemySensor>();
        enemy = GetComponent<Enemy>();
        animator = GetComponentInChildren<Animator>();

        startPosition = pointA != null ? pointA.position : transform.position;
        endPosition = pointB != null ? pointB.position
            : startPosition + transform.forward * automaticPatrolDistance;

        if (agent != null)
        {
            combatSpeed = agent.speed;
            combatStoppingDistance = agent.stoppingDistance;
        }
        else if (pointB == null && Physics.Raycast(endPosition + Vector3.up * 100f,
            Vector3.down, out RaycastHit ground, 200f, 1 << 3))
        {
            endPosition.y = Mathf.Max(endPosition.y, ground.point.y + 2f);
        }
    }

    private void Update()
    {
        if (enemy != null && enemy.IsDead)
        {
            EndPatrol(true);
            return;
        }
        if (sensor != null && sensor.PlayerDetected)
        {
            EndPatrol(false);
            return;
        }

        if (!IsPatrolling)
        {
            IsPatrolling = true;
            waitUntil = 0f;
            if (agent != null)
            {
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0.35f;
            }
        }
        if (Time.time < waitUntil)
            return;

        Vector3 target = goingToB ? endPosition : startPosition;
        if (agent != null)
        {
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
                return;

            if (Physics.Raycast(target + Vector3.up * 100f, Vector3.down,
                out RaycastHit ground, 200f, 1 << 3))
                target.y = ground.point.y;

            var filter = new NavMeshQueryFilter
            {
                agentTypeID = agent.agentTypeID,
                areaMask = agent.areaMask
            };
            if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 5f, filter))
            {
                agent.isStopped = true;
                SetMoving(false);
                return;
            }
            target = hit.position;
        }

        // Reach B, wait, then go back to A (and repeat).
        if (Vector3.Distance(transform.position, target) <= 0.5f)
        {
            if (agent != null) agent.isStopped = true;
            goingToB = !goingToB;
            waitUntil = Time.time + waitAtPoint;
            SetMoving(false);
            return;
        }

        Vector3 direction = target - transform.position;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(target);
            direction = agent.steeringTarget - transform.position;
            SetMoving(agent.velocity.sqrMagnitude > 0.01f);
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, target,
                patrolSpeed * Time.deltaTime);
            SetMoving(Time.deltaTime > 0f);
        }

        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction), 6f * Time.deltaTime);
    }

    private void SetMoving(bool moving)
    {
        if (animator != null) animator.SetBool("IsMoving", moving);
    }

    private void EndPatrol(bool stop)
    {
        if (!IsPatrolling) return;
        if (agent != null)
        {
            agent.speed = combatSpeed;
            agent.stoppingDistance = combatStoppingDistance;
            if (stop && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }
        IsPatrolling = false;
        if (stop) SetMoving(false);
    }

    private void OnDisable()
    {
        EndPatrol(true);
    }
}
