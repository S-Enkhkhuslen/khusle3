using UnityEngine;
using UnityEngine.AI;
public class MeleeEnemyAI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float attackDistance = 2f;
    [SerializeField] private float rotateSpeed = 5f;
    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackHitDelay = 0.35f;
    [SerializeField, Min(0f)] private float meleeVerticalRange = 1.5f;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float movingThreshold = 0.1f;
    private EnemySensor sensor;
    private EnemyPatrol patrol;
    private NavMeshAgent agent;
    private Enemy enemy;
    private float nextAttackTime;
    private bool attackPending;
    private float attackHitTime;
    private Transform attackTarget;
    private void Awake()
    {
        sensor = GetComponent<EnemySensor>();
        patrol = GetComponent<EnemyPatrol>();
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (agent != null)
        {
            agent.updateRotation = false;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void OnEnable()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();
        if (enemy != null)
            enemy.OnEnemyDied += HandleDeath;
    }

    private void OnDisable()
    {
        attackPending = false;
        attackTarget = null;
        if (enemy != null)
            enemy.OnEnemyDied -= HandleDeath;
    }

    private void Update()
    {
        if (enemy != null && enemy.IsDead)
            return;
        UpdateAttackDamage();
        if (sensor == null || agent == null)
            return;
        if (!sensor.PlayerDetected)
        {
            if (patrol != null && patrol.isActiveAndEnabled)
                return;
            StopMoving();
            SetMoving(false);
            return;
        }

        Transform player = sensor.PlayerTransform;
        if (player == null)
        {
            StopMoving();
            SetMoving(false);
            return;
        }

        FacePlayer(player);
        float distance = GetHorizontalDistance(player);
        if (distance > attackDistance)
        {
            ChasePlayer(player);
        }

        else
        {
            StopMoving();
            SetMoving(false);
            AttackPlayer(player);
        }

        UpdateMovementAnimation();
    }

    private void ChasePlayer(Transform player)
    {
        if (agent == null || !agent.isOnNavMesh)
            return;
        Vector3 targetPosition = new Vector3( player.position.x, transform.position.y, player.position.z );
        NavMeshHit hit;
        if (NavMesh.SamplePosition( targetPosition, out hit, 5f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination( hit.position );
        }
    }

    private void StopMoving()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;
        agent.isStopped = true;
    }

    private void FacePlayer(Transform player)
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction == Vector3.zero)
            return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp( transform.rotation, targetRotation, rotateSpeed * Time.deltaTime );
    }

    private void AttackPlayer(Transform player)
    {
        if (!CombatPhysics.CanMeleeHit(transform, player, attackDistance, meleeVerticalRange))
            return;
        if (Time.time < nextAttackTime)
            return;
        nextAttackTime = Time.time + attackCooldown;
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Damage-ийг animation-ийн цохилтын мөчид өгнө.
        attackTarget = player;
        attackHitTime = Time.time + attackHitDelay;
        attackPending = true;
    }

    private void UpdateAttackDamage()
    {
        if (!attackPending)
            return;
        if (Time.time < attackHitTime)
            return;
        attackPending = false;
        if (attackTarget == null)
            return;
        if (!CombatPhysics.CanMeleeHit(transform, attackTarget, attackDistance, meleeVerticalRange))
        {
            attackTarget = null;
            return;
        }

        PlayerHealth playerHealth = attackTarget.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage( attackDamage );
        }

        attackTarget = null;
    }

    private void UpdateMovementAnimation()
    {
        if (animator == null || agent == null || !agent.isOnNavMesh)
            return;
        bool isMoving = !agent.isStopped && agent.velocity.magnitude > movingThreshold;
        SetMoving(isMoving);
    }

    private void SetMoving(bool value)
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", value);
        }
    }

    private void HandleDeath()
    {
        StopMoving();
        SetMoving(false);
        attackPending = false;
        attackTarget = null;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        enabled = false;
    }

    private float GetHorizontalDistance( Transform player)
    {
        Vector3 difference = player.position - transform.position;
        difference.y = 0f;
        return difference.magnitude;
    }
}
