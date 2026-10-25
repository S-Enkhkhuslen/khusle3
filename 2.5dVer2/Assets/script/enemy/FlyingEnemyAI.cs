
using UnityEngine;

public class FlyingEnemyAI : MonoBehaviour
{
    // =====================================================
    // DISTANCE SETTINGS
    // =====================================================

    [Header("Distance Settings")]

    // Энэ зайнаас ойр бол ухарна
    [SerializeField] private float minDistance = 8f;

    // Энэ зайнаас хол бол Player руу ойртоно
    [SerializeField] private float maxDistance = 14f;

    // Player-аас хэдэн метр дээр нисэх вэ?
    [SerializeField] private float flyingHeight = 6f;


    // =====================================================
    // MOVEMENT
    // =====================================================

    [Header("Movement")]

    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private float retreatSpeed = 6f;


    // =====================================================
    // ROTATION
    // =====================================================

    [Header("Rotation")]

    [SerializeField] private float rotateSpeed = 5f;


    // =====================================================
    // SHOOTING
    // =====================================================

    [Header("Shooting")]

    [SerializeField] private Transform firePoint;

    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] private float shootCooldown = 1f;

    [SerializeField] private float projectileSpeed = 20f;

    [SerializeField] private float damage = 10f;

    [SerializeField] private float shootTargetHeight = 1f;


    // =====================================================
    // ANIMATION SETTINGS
    // =====================================================

    [Header("Animation")]

    // ANIMATION: Flying Enemy-ийн Animator
    [SerializeField] private Animator animator;

    // ANIMATION: Хөдөлж байгааг тооцох хамгийн бага хурд
    [SerializeField] private float movingThreshold = 0.1f;

    // ANIMATION: Enemy-ийн Health болон Death
    private Enemy enemy;

    // ANIMATION: Өмнөх frame-ийн байрлал
    private Vector3 previousPosition;


    // =====================================================
    // PRIVATE VARIABLES
    // =====================================================

    private EnemySensor sensor;

    private float nextShootTime;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        sensor =
            GetComponent<EnemySensor>();

        // ANIMATION: Enemy component-ийг авна
        enemy = GetComponent<Enemy>();

        // ANIMATION: Animator холбоогүй бол
        // Child model-оос автоматаар хайна
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // ANIMATION: Нисэх хөдөлгөөнийг AI удирдана
        // Тиймээс Root Motion хэрэггүй
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // ANIMATION: Эхний байрлалыг хадгална
        previousPosition = transform.position;
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // ANIMATION: Enemy үхсэн бол
        // нисэх болон буудах систем ажиллахгүй
        if (enemy != null && enemy.IsDead)
        {
            SetMovingAnimation(false);
            return;
        }

        // Sensor байхгүй бол ажиллахгүй
        if (sensor == null)
        {
            SetMovingAnimation(false);
            return;
        }

        // Player detect хийгээгүй бол юу ч хийхгүй
        if (!sensor.PlayerDetected)
        {
            // ANIMATION: Idle ажиллуулна
            SetMovingAnimation(false);
            previousPosition = transform.position;
            return;
        }

        Transform player =
            sensor.PlayerTransform;

        if (player == null)
        {
            SetMovingAnimation(false);
            previousPosition = transform.position;
            return;
        }

        // ANIMATION: Хөдөлгөөний өмнөх байрлал
        previousPosition = transform.position;

        // Player хүртэлх 3D зай
        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        // Player руу харна
        FacePlayer(player);


        // =========================================
        // TOO CLOSE
        // =========================================

        if (distance < minDistance)
        {
            RetreatFromPlayer(player);
        }

        // =========================================
        // TOO FAR
        // =========================================

        else if (distance > maxDistance)
        {
            MoveTowardsPlayer(player);
        }

        // =========================================
        // GOOD DISTANCE
        // =========================================

        else
        {
            Shoot(player);
        }

        // ANIMATION: Нисэж хөдөлж байгаа эсэхийг шалгана
        UpdateMovementAnimation();
    }


    // =====================================================
    // MOVE TOWARDS PLAYER
    // =====================================================

    private void MoveTowardsPlayer(
        Transform player)
    {
        // Player-ийн дээр байх target position
        Vector3 targetPosition =
            player.position
            + Vector3.up * flyingHeight;

        // Enemy-г targetPosition руу хөдөлгөнө
        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
    }


    // =====================================================
    // RETREAT
    // =====================================================

    private void RetreatFromPlayer(
        Transform player)
    {
        // Player -> Enemy чиглэл
        Vector3 awayDirection =
            transform.position
            - player.position;

        // Ухрахдаа horizontal чиглэлээр холдоно
        awayDirection.y = 0f;

        // Player болон Enemy яг давхцсан бол
        // direction 0 болохоос хамгаална
        if (awayDirection == Vector3.zero)
        {
            awayDirection =
                -transform.forward;
        }

        // Direction-ийн уртыг 1 болгоно
        awayDirection.Normalize();

        // Enemy одоогийн position-оос
        // Player-ээс холдох direction руу target гаргана
        Vector3 retreatPosition =
            transform.position
            + awayDirection * 5f;

        // Player-ийн өндрийг дагана
        retreatPosition.y =
            player.position.y
            + flyingHeight;

        // Retreat position руу хөдөлнө
        transform.position =
            Vector3.MoveTowards(
                transform.position,
                retreatPosition,
                retreatSpeed * Time.deltaTime
            );
    }


    // =====================================================
    // FACE PLAYER
    // =====================================================

    private void FacePlayer(
        Transform player)
    {
        Vector3 direction =
            player.position
            - transform.position;

        if (direction == Vector3.zero)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
    }


    // =====================================================
    // SHOOT
    // =====================================================

    private void Shoot(
        Transform player)
    {
        // Cooldown дуусаагүй
        if (Time.time < nextShootTime)
            return;

        // FirePoint эсвэл Projectile байхгүй
        if (firePoint == null ||
            projectilePrefab == null)
            return;

        // Дараагийн shoot time
        nextShootTime =
            Time.time + shootCooldown;

        // ANIMATION: Буудах animation ажиллуулна
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Player-ийн бие орчим онилно
        Vector3 targetPosition =
            player.position
            + Vector3.up * shootTargetHeight;

        // FirePoint -> Player direction
        Vector3 shootDirection =
            targetPosition
            - firePoint.position;

        shootDirection.Normalize();

        // Projectile үүсгэнэ
        GameObject projectile =
            Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.LookRotation(
                    shootDirection
                )
            );

        // Projectile script авна
        EnemyProjectile projectileScript =
            projectile.GetComponent<EnemyProjectile>();

        if (projectileScript != null)
        {
            projectileScript.Launch(
                shootDirection,
                projectileSpeed,
                damage,
                transform
            );
        }

        Debug.Log("FLYING ENEMY SHOOT");
    }


    // =====================================================
    // ANIMATION - MOVEMENT
    // =====================================================

    private void UpdateMovementAnimation()
    {
        if (animator == null)
            return;

        // Өмнөх байрлалаас хэр хол хөдөлснийг олно
        float movedDistance =
            Vector3.Distance(
                previousPosition,
                transform.position
            );

        // Нэг секундэд хөдөлсөн зай буюу хурд
        float currentSpeed =
            movedDistance / Mathf.Max(
                Time.deltaTime,
                0.0001f
            );

        // Хурд хангалттай байвал хөдөлж байна
        bool isMoving =
            currentSpeed > movingThreshold;

        SetMovingAnimation(isMoving);
    }


    private void SetMovingAnimation(bool isMoving)
    {
        if (animator == null)
            return;

        // true  = Run (Flying movement)
        // false = Idle (Hover)

        animator.SetBool(
            "IsMoving",
            isMoving
        );
    }
}
