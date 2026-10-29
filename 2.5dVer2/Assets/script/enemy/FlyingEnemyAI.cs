using UnityEngine;
public class FlyingEnemyAI : MonoBehaviour
{
    [Header("Distance Settings")]
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 14f;
    [SerializeField] private float flyingHeight = 6f;
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float retreatSpeed = 6f;
    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 5f;
    [Header("Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootCooldown = 1f;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float shootTargetHeight = 1f;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float movingThreshold = 0.1f;
    private Enemy enemy;
    private Vector3 previousPosition;
    private EnemySensor sensor;
    private EnemyPatrol patrol;
    private FlyingMovement movement;
    private float nextShootTime;
    private void Awake()
    {
        sensor = GetComponent<EnemySensor>();
        patrol = GetComponent<EnemyPatrol>();
        enemy = GetComponent<Enemy>();
        movement = GetComponent<FlyingMovement>();
        if (movement == null) movement = gameObject.AddComponent<FlyingMovement>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        previousPosition = transform.position;
    }

    private void Update()
    {
        if (enemy != null && enemy.IsDead)
        {
            SetMovingAnimation(false);
            return;
        }

        if (sensor == null)
        {
            SetMovingAnimation(false);
            return;
        }

        if (!sensor.PlayerDetected)
        {
            if (patrol != null && patrol.isActiveAndEnabled)
                return;
            SetMovingAnimation(false);
            previousPosition = transform.position;
            return;
        }

        Transform player = sensor.PlayerTransform;
        if (player == null)
        {
            SetMovingAnimation(false);
            previousPosition = transform.position;
            return;
        }

        previousPosition = transform.position;
        float distance = Vector3.Distance( transform.position, player.position );
        FacePlayer(player);
        if (distance < minDistance)
        {
            RetreatFromPlayer(player);
        }

        else if (distance > maxDistance)
        {
            MoveTowardsPlayer(player);
        }

        else
        {
            Shoot(player);
        }

        UpdateMovementAnimation();
    }

    private void MoveTowardsPlayer( Transform player)
    {
        Vector3 targetPosition = player.position
            + Vector3.up * flyingHeight;
        movement.MoveTowards(targetPosition, moveSpeed);
    }

    private void RetreatFromPlayer( Transform player)
    {
        Vector3 awayDirection = transform.position
            - player.position;
        awayDirection.y = 0f;
        if (awayDirection == Vector3.zero)
        {
            awayDirection = -transform.forward;
        }

        awayDirection.Normalize();
        Vector3 retreatPosition = transform.position
            + awayDirection * 5f;
        retreatPosition.y = player.position.y
            + flyingHeight;
        movement.MoveTowards(retreatPosition, retreatSpeed);
    }

    private void FacePlayer( Transform player)
    {
        Vector3 direction = player.position
            - transform.position;
        if (direction == Vector3.zero)
            return;
        Quaternion targetRotation = Quaternion.LookRotation( direction );
        transform.rotation = Quaternion.Slerp( transform.rotation, targetRotation, rotateSpeed * Time.deltaTime );
    }

    private void Shoot( Transform player)
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
        Vector3 shootDirection = targetPosition
            - firePoint.position;
        shootDirection.Normalize();
        GameObject projectile = Instantiate( projectilePrefab, firePoint.position, Quaternion.LookRotation( shootDirection ) );
        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Launch( shootDirection, projectileSpeed, damage, transform );
        }

    }

    private void UpdateMovementAnimation()
    {
        if (animator == null)
            return;
        float movedDistance = Vector3.Distance( previousPosition, transform.position );
        float currentSpeed = movedDistance / Mathf.Max( Time.deltaTime, 0.0001f );
        bool isMoving = currentSpeed > movingThreshold;
        SetMovingAnimation(isMoving);
    }

    private void SetMovingAnimation(bool isMoving)
    {
        if (animator == null)
            return;
        animator.SetBool( "IsMoving", isMoving );
    }
}
