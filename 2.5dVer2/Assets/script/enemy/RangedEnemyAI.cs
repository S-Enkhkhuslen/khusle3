using UnityEngine;
using UnityEngine.AI;
public class RangedEnemyAI : MonoBehaviour
{
    [Header("Ranged Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootCooldown = 1f;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float rangedDamage = 10f;
    [SerializeField] private float shootTargetHeight = 1f;
    private float nextShootTime;
    [SerializeField] private float shootMaxDistance = 15f;
    [SerializeField] private float meleeEnterDistance = 4f;
    [SerializeField] private float meleeExitDistance = 6f;
    [SerializeField] private float meleeAttackDistance = 2f;
    [Header("Melee Attack")]
    [SerializeField] private float meleeDamage = 20f;
    [SerializeField] private float meleeCooldown = 1.5f;
    private float nextMeleeTime;
    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 5f;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float movingThreshold = 0.1f;
    [SerializeField] private float meleeHitDelay = 0.35f;
    [SerializeField, Min(0f)] private float meleeVerticalRange = 1.5f;
    private bool meleeAttackPending = false;
    private float meleeHitTime;
    private Transform meleeTarget;
    private Enemy enemy;
    private EnemySensor sensor;
    private EnemyPatrol patrol;
    private NavMeshAgent agent;
    private bool isMeleeMode = false;
    private void OnDisable()
    {
        meleeAttackPending = false;
        meleeTarget = null;
        isMeleeMode = false;
    }
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

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (agent != null)
        {
            agent.updateRotation = false;
        }
    }

    private void Update()
    {
        if (enemy != null && enemy.IsDead)
        {
            StopMoving();
            SetMovingAnimation(false);
            meleeAttackPending = false;
            meleeTarget = null;
            return;
        }

        UpdateMeleeDamage();
        if (sensor == null || agent == null)
        {
            SetMovingAnimation(false);
            return;
        }

        if (!sensor.PlayerDetected)
        {
            if (patrol != null && patrol.isActiveAndEnabled)
                return;
            StopMoving();
            SetMovingAnimation(false);
            return;
        }

        Transform player = sensor.PlayerTransform;
        if (player == null)
        {
            StopMoving();
            SetMovingAnimation(false);
            return;
        }

        float distance = GetHorizontalDistance(player);
        CharacterController playerController = player.GetComponent<CharacterController>();
        bool canUseMelee = Mathf.Abs(player.position.y - transform.position.y) <= meleeVerticalRange
            && (playerController == null || playerController.isGrounded);
        FacePlayer(player);
        if (!isMeleeMode && canUseMelee && distance <= meleeEnterDistance)
        {
            isMeleeMode = true;
        }

        if (isMeleeMode && (distance >= meleeExitDistance || !canUseMelee))
        {
            isMeleeMode = false;
        }

        if (!canUseMelee)
        {
            meleeAttackPending = false;
            meleeTarget = null;
        }

        if (isMeleeMode)
        {
            if (distance > meleeAttackDistance || !sensor.CanSeePlayer)
            {
                ChasePlayer(player);
            }
            else
            {
                StopMoving();
                MeleeAttack(player);
            }

            UpdateMovementAnimation();
            return;
        }

        if (distance > shootMaxDistance || !sensor.CanSeePlayer)
        {
            ChasePlayer(player);
        }
        else
        {
            StopMoving();
            RangedAttack(player);
        }

        UpdateMovementAnimation();
    }

    private void ChasePlayer(Transform player)
    {
        EnemyNavigation.Chase(agent, player.position);
    }

    private void StopMoving()
    {
        EnemyNavigation.Stop(agent);
    }

    private void FacePlayer(Transform player)
    {
        EnemyNavigation.Face(transform, player, rotateSpeed);
    }

    private void RangedAttack(Transform player)
    {
        if (Time.time < nextShootTime)
            return;
        if (firePoint == null || projectilePrefab == null)
            return;
        nextShootTime = Time.time + shootCooldown;
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        Vector3 targetPosition = player.position
            + Vector3.up * shootTargetHeight;
        Vector3 shootDirection = targetPosition - firePoint.position;
        shootDirection.Normalize();
        GameObject projectile = Instantiate( projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection) );
        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Launch( shootDirection, projectileSpeed, rangedDamage, transform );
        }

    }

    private void MeleeAttack(Transform player)
    {
        if (!CombatPhysics.CanMeleeHit(transform, player, meleeAttackDistance, meleeVerticalRange))
            return;
        if (Time.time < nextMeleeTime)
            return;
        nextMeleeTime = Time.time + meleeCooldown;
        GameAudio.MeleeSwing(transform.position);
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Damage-ийг animation-ийн цохилтын мөчид өгнө.
        meleeTarget = player;
        meleeHitTime = Time.time + meleeHitDelay;
        meleeAttackPending = true;
    }

    private void UpdateMeleeDamage()
    {
        if (!meleeAttackPending || Time.time < meleeHitTime) return;
        meleeAttackPending = false;
        if (CombatPhysics.CanMeleeHit(transform, meleeTarget, meleeAttackDistance, meleeVerticalRange))
            meleeTarget.GetComponent<PlayerHealth>()?.TakeDamage(meleeDamage);
        meleeTarget = null;
    }

    private void UpdateMovementAnimation()
    {
        if (animator == null || agent == null)
            return;
        if (!agent.isOnNavMesh)
        {
            SetMovingAnimation(false);
            return;
        }

        bool isMoving = !agent.isStopped && agent.velocity.magnitude > movingThreshold;
        if (meleeAttackPending)
        {
            isMoving = false;
        }

        SetMovingAnimation(isMoving);
    }

    private void SetMovingAnimation(bool isMoving)
    {
        if (animator == null)
            return;
        animator.SetBool("IsMoving", isMoving);
    }

    private float GetHorizontalDistance(Transform player)
    {
        return EnemyNavigation.Distance(transform, player);
    }
}
