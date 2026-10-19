using Unity.VisualScripting;
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
    [SerializeField] private float meleeEnterDistance = 3f;

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


    private EnemySensor sensor;
    private NavMeshAgent agent;


    // Одоо Melee mode дээр байгаа эсэх
    private bool isMeleeMode = false;


    private void Awake()
    {
        sensor = GetComponent<EnemySensor>();

        agent = GetComponent<NavMeshAgent>();


        // Rotation-ийг өөрсдөө удирдана
        if (agent != null)
        {
            agent.updateRotation = false;
        }
    }


    private void Update()
    {
        // Component байхгүй бол ажиллахгүй
        if (sensor == null || agent == null)
            return;


        // Player detect хийгээгүй
        if (!sensor.PlayerDetected)
        {
            StopMoving();
            return;
        }


        Transform player =
            sensor.PlayerTransform;


        if (player == null)
            return;


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
    }


    // =====================================================
    // CHASE PLAYER
    // =====================================================

    private void ChasePlayer(Transform player)
    {
        if (!agent.isOnNavMesh)
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
        if (!agent.isOnNavMesh)
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
                rangedDamage
            );
        }


        Debug.Log("RANGED SHOOT");
    }


    // =====================================================
    // MELEE ATTACK
    // =====================================================
    private void MeleeAttack(Transform player)
    {
        // Дараагийн attack хийх хугацаа болоогүй бол
        // одоохондоо attack хийхгүй
        if (Time.time < nextMeleeTime)
            return;


        // Дараагийн attack хийх боломжтой хугацааг тохируулна
        nextMeleeTime =
            Time.time + meleeCooldown;


        // Player дээр байгаа Health script-ийг авна
        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();


        // PlayerHealth олдсон бол damage өгнө
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                meleeDamage
            );
        }


        Debug.Log("MELEE ATTACK");
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