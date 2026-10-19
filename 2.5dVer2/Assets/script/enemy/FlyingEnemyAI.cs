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
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // Sensor байхгүй бол ажиллахгүй
        if (sensor == null)
            return;


        // Player detect хийгээгүй бол юу ч хийхгүй
        if (!sensor.PlayerDetected)
            return;


        Transform player =
            sensor.PlayerTransform;


        if (player == null)
            return;


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
                damage
            );
        }


        Debug.Log("FLYING ENEMY SHOOT");
    }
}