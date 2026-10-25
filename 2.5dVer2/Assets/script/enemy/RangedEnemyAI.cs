
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

    // Үүнээс хол байвал Player руу ойртоно
    [SerializeField] private float shootMaxDistance = 15f;

    // Үүнээс ойртоход Melee mode руу орно
    [SerializeField] private float meleeEnterDistance = 4f;

    // Melee mode-д орсны дараа
    // Player энэ зайнаас холдвол Ranged mode руу буцна
    [SerializeField] private float meleeExitDistance = 6f;

    // Melee attack хийх бодит зай
    [SerializeField] private float meleeAttackDistance = 2f;

    [Header("Melee Attack")]

    [SerializeField] private float meleeDamage = 20f;

    [SerializeField] private float meleeCooldown = 1.5f;

    private float nextMeleeTime;

    [Header("Rotation")]

    [SerializeField] private float rotateSpeed = 5f;


    // =====================================================
    // ANIMATION SETTINGS
    // =====================================================

    [Header("Animation")]

    // ANIMATION: Ranged Enemy-ийн Animator
    [SerializeField] private Animator animator;

    // ANIMATION: Хөдөлж байгааг мэдэх хамгийн бага хурд
    [SerializeField] private float movingThreshold = 0.1f;

    // ANIMATION: Attack хөдөлгөөний эхнээс
    // хэдэн секундийн дараа Melee damage өгөх вэ?
    [SerializeField] private float meleeHitDelay = 0.35f;
    [SerializeField, Min(0f)] private float meleeVerticalRange = 1.5f;

    // ANIMATION: Melee attack damage хүлээгдэж байгаа эсэх
    private bool meleeAttackPending = false;

    // ANIMATION: Damage өгөх хугацаа
    private float meleeHitTime;

    // ANIMATION: Дайрч байгаа Player
    private Transform meleeTarget;

    // ANIMATION: Enemy-ийн Health болон Death төлөв
    private Enemy enemy;


    private EnemySensor sensor;
    private NavMeshAgent agent;


    // Одоо Melee mode дээр байгаа эсэх
    private bool isMeleeMode = false;


    private void Awake()
    {
        sensor = GetComponent<EnemySensor>();

        agent = GetComponent<NavMeshAgent>();

        // ANIMATION: Enemy script-ийг авна
        enemy = GetComponent<Enemy>();

        // ANIMATION: Animator Inspector дээр
        // холбогдоогүй бол Child model-оос хайна
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // ANIMATION: NavMeshAgent хөдөлгөөнийг удирдах учраас
        // Root Motion-ийг унтраана
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // Rotation-ийг өөрсдөө удирдана
        if (agent != null)
        {
            agent.updateRotation = false;
        }
    }


    private void Update()
    {
        // ANIMATION: Enemy үхсэн бол
        // цааш AI ажиллуулахгүй
        if (enemy != null && enemy.IsDead)
        {
            StopMoving();

            SetMovingAnimation(false);

            meleeAttackPending = false;
            meleeTarget = null;

            return;
        }

        // ANIMATION: Хүлээгдэж байгаа Melee damage шалгана
        UpdateMeleeDamage();

        // Component байхгүй бол ажиллахгүй
        if (sensor == null || agent == null)
        {
            SetMovingAnimation(false);
            return;
        }

        // Player detect хийгээгүй
        if (!sensor.PlayerDetected)
        {
            StopMoving();

            // ANIMATION: Idle ажиллуулна
            SetMovingAnimation(false);

            return;
        }


        Transform player =
            sensor.PlayerTransform;


        if (player == null)
        {
            StopMoving();
            SetMovingAnimation(false);
            return;
        }


        // Player хүртэлх horizontal зай
        float distance =
            GetHorizontalDistance(player);


        // Player руу харна
        FacePlayer(player);


        // =========================================
        // RANGED -> MELEE
        // =========================================

        if (!isMeleeMode &&
            distance <= meleeEnterDistance)
        {
            isMeleeMode = true;

            Debug.Log("MELEE MODE");
        }


        // =========================================
        // MELEE -> RANGED
        // =========================================

        if (isMeleeMode &&
            distance >= meleeExitDistance)
        {
            isMeleeMode = false;

            Debug.Log("RANGED MODE");
        }


        // =========================================
        // MELEE MODE
        // =========================================

        if (isMeleeMode)
        {
            // Player melee attack distance-аас хол
            if (distance > meleeAttackDistance)
            {
                ChasePlayer(player);
            }
            else
            {
                StopMoving();

                MeleeAttack(player);
            }

            // ANIMATION: Хөдөлгөөний төлөв шинэчилнэ
            UpdateMovementAnimation();

            // Melee mode ажиллачихсан учраас
            // доорх Ranged код руу оруулахгүй
            return;
        }


        // =========================================
        // RANGED MODE
        // =========================================

        // Player буудах зайнаас хэт хол
        if (distance > shootMaxDistance)
        {
            ChasePlayer(player);
        }
        else
        {
            // Зөв буудах зайд орсон
            StopMoving();

            RangedAttack(player);
        }

        // ANIMATION: Run эсвэл Idle ажиллуулна
        UpdateMovementAnimation();
    }


    // =====================================================
    // CHASE PLAYER
    // =====================================================

    private void ChasePlayer(Transform player)
    {
        if (agent == null || !agent.isOnNavMesh)
            return;


        // Player-ийн X/Z байрлал
        Vector3 targetPosition =
            new Vector3(
                player.position.x,
                transform.position.y,
                player.position.z
            );


        NavMeshHit hit;


        // Ойролцоох NavMesh position олно
        if (NavMesh.SamplePosition(
            targetPosition,
            out hit,
            5f,
            NavMesh.AllAreas))
        {
            agent.isStopped = false;

            agent.SetDestination(
                hit.position
            );
        }
    }


    // =====================================================
    // STOP
    // =====================================================

    private void StopMoving()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;


        agent.isStopped = true;
    }


    // =====================================================
    // FACE PLAYER
    // =====================================================

    private void FacePlayer(Transform player)
    {
        Vector3 direction =
            player.position
            - transform.position;


        // Ground enemy тул дээш доош хазайхгүй
        direction.y = 0f;


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
    // RANGED ATTACK
    // =====================================================

    private void RangedAttack(Transform player)
    {
        // Cooldown дуусаагүй бол буудахгүй
        if (Time.time < nextShootTime)
            return;


        // FirePoint эсвэл Projectile байхгүй бол
        // буудаж болохгүй
        if (firePoint == null || projectilePrefab == null)
            return;


        // Дараагийн буудах хугацааг тохируулна
        nextShootTime =
            Time.time + shootCooldown;


        // ANIMATION: Буудах animation ажиллуулна
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }


        // Player-ийн бие орчимд онилно
        Vector3 targetPosition =
            player.position
            + Vector3.up * shootTargetHeight;


        // FirePoint -> Player чиглэл
        Vector3 shootDirection =
            targetPosition - firePoint.position;


        // Чиглэлийн уртыг 1 болгоно
        shootDirection.Normalize();


        // Projectile үүсгэнэ
        GameObject projectile =
            Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.LookRotation(shootDirection)
            );


        // Projectile дээрх script-ийг авна
        EnemyProjectile projectileScript =
            projectile.GetComponent<EnemyProjectile>();


        // Script байгаа бол сумыг хөдөлгөнө
        if (projectileScript != null)
        {
            projectileScript.Launch(
                shootDirection,
                projectileSpeed,
                rangedDamage,
                transform
            );
        }


        Debug.Log("RANGED SHOOT");
    }


    // =====================================================
    // MELEE ATTACK
    // =====================================================
    private void MeleeAttack(Transform player)
    {
        if (!CombatPhysics.CanMeleeHit(transform, player, meleeAttackDistance, meleeVerticalRange))
            return;

        // Дараагийн attack хийх хугацаа болоогүй бол
        // одоохондоо attack хийхгүй
        if (Time.time < nextMeleeTime)
            return;


        // Дараагийн attack хийх боломжтой хугацааг тохируулна
        nextMeleeTime =
            Time.time + meleeCooldown;


        // ANIMATION: Melee үед мөн Attack ажиллуулна
        // Одоогоор Ranged болон Melee mode
        // нэг Attack animation ашиглаж байгаа
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }


        // Player дээр байгаа Health script-ийг авна
        // ANIMATION: Damage-ийг шууд өгөхгүй
        // Attack хөдөлгөөний дараа өгнө
        meleeTarget = player;

        meleeHitTime =
            Time.time + meleeHitDelay;

        meleeAttackPending = true;


        Debug.Log("MELEE ATTACK");
    }


    // =====================================================
    // ANIMATION - MELEE DAMAGE
    // =====================================================

    private void UpdateMeleeDamage()
    {
        // Melee Attack хүлээгдээгүй бол
        if (!meleeAttackPending)
            return;

        // Цохилт хийх хугацаа болоогүй бол
        if (Time.time < meleeHitTime)
            return;

        meleeAttackPending = false;

        // Player байхгүй болсон бол
        if (meleeTarget == null)
            return;

        // Player хэт холдсон эсэхийг шалгана
        float distance =
            GetHorizontalDistance(meleeTarget);

        if (!CombatPhysics.CanMeleeHit(transform, meleeTarget, meleeAttackDistance, meleeVerticalRange))
        {
            meleeTarget = null;
            return;
        }

        // Player дээр байгаа Health script-ийг авна
        PlayerHealth playerHealth =
            meleeTarget.GetComponent<PlayerHealth>();


        // PlayerHealth олдсон бол damage өгнө
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                meleeDamage
            );
        }

        meleeTarget = null;
    }


    // =====================================================
    // ANIMATION - MOVEMENT
    // =====================================================

    private void UpdateMovementAnimation()
    {
        if (animator == null || agent == null)
            return;

        if (agent == null || !agent.isOnNavMesh)
        {
            SetMovingAnimation(false);
            return;
        }

        // NavMeshAgent үнэхээр хөдөлж байгаа эсэх
        bool isMoving =
            !agent.isStopped &&
            agent.velocity.magnitude > movingThreshold;

        // ANIMATION: Melee цохилт хийх үед
        // Run animation-ийг түр зогсооно
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

        // true = Run
        // false = Idle
        animator.SetBool("IsMoving", isMoving);
    }


    // =====================================================
    // DISTANCE
    // =====================================================

    private float GetHorizontalDistance(
        Transform player)
    {
        Vector3 difference =
            player.position
            - transform.position;


        difference.y = 0f;


        return difference.magnitude;
    }
}
