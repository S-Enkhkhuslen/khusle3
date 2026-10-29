
using UnityEngine;
using UnityEngine.AI;

public class MeleeEnemyAI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float attackDistance = 2f;

    // Enemy Player руу хэр хурдан эргэх вэ?
    [SerializeField] private float rotateSpeed = 5f;

    [Header("Attack")]
    // Нэг attack-аас дараагийн attack хүртэлх хугацаа
    [SerializeField] private float attackCooldown = 1.5f;

    // Нэг цохилтоор хэдэн damage өгөх вэ?
    [SerializeField] private float attackDamage = 20f;

    // ANIMATION: Сэлэм цавчих мөч хүртэл хүлээнэ
    [SerializeField] private float attackHitDelay = 0.35f;
    [SerializeField, Min(0f)] private float meleeVerticalRange = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [SerializeField] private float movingThreshold = 0.1f;

    private EnemySensor sensor;
    private EnemyPatrol patrol;
    private NavMeshAgent agent;
    private Enemy enemy;

    // Дараагийн удаа хэдэн секундэд attack хийж болохыг хадгална
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

        // Enemy-ийн rotation-ийг бид өөрсдөө удирдана
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
        if (enemy != null)
            enemy.OnEnemyDied -= HandleDeath;
    }


    private void Update()
    {
        // Enemy үхсэн бол AI ажиллахгүй
        if (enemy != null && enemy.IsDead)
            return;

        // Attack animation-ийн damage өгөх мөч
        UpdateAttackDamage();

        // Sensor эсвэл Agent байхгүй бол ажиллахгүй
        if (sensor == null || agent == null)
            return;

        // Player detect хийгээгүй бол зогсоно
        if (!sensor.PlayerDetected)
        {
            if (patrol != null && patrol.isActiveAndEnabled)
                return;

            StopMoving();
            SetMoving(false);
            return;
        }

        // Sensor-оос Player-ийн Transform авна
        Transform player =
            sensor.PlayerTransform;

        if (player == null)
        {
            StopMoving();
            SetMoving(false);
            return;
        }

        // Player руу үргэлж харна
        FacePlayer(player);

        // Player хүртэлх газрын зайг олно
        float distance =
            GetHorizontalDistance(player);

        // Player хол байгаа бол араас нь хөөнө
        if (distance > attackDistance)
        {
            ChasePlayer(player);
        }

        // Player ойрхон болсон
        else
        {
            StopMoving();

            SetMoving(false);

            AttackPlayer(player);
        }

        UpdateMovementAnimation();
    }


    // =====================================================
    // CHASE
    // =====================================================

    private void ChasePlayer(Transform player)
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        Vector3 targetPosition =
            new Vector3(
                player.position.x,
                transform.position.y,
                player.position.z
            );

        NavMeshHit hit;

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
        // Enemy -> Player чиглэл
        Vector3 direction =
            player.position - transform.position;

        // Ground enemy болохоор
        // дээш доош хазайлгахгүй
        direction.y = 0f;

        // Player яг Enemy-тэй нэг position дээр байвал
        // rotation хийх боломжгүй
        if (direction == Vector3.zero)
            return;

        // Player руу харахад хэрэгтэй rotation
        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        // Шууд тас эргэхгүй,
        // аажмаар Player руу эргэнэ
        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
    }


    // =====================================================
    // ATTACK
    // =====================================================

    private void AttackPlayer(Transform player)
    {
        if (!CombatPhysics.CanMeleeHit(transform, player, attackDistance, meleeVerticalRange))
            return;

        // Cooldown дуусаагүй бол attack хийхгүй
        if (Time.time < nextAttackTime)
            return;

        // Дараагийн attack хийх боломжтой хугацааг тооцно
        nextAttackTime =
            Time.time + attackCooldown;

        Debug.Log("MELEE ATTACK");

        // ANIMATION: Attack эхлүүлнэ
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Player дээрх Health script-ийг хайна
        // Damage-ийг animation-ийн цохилтын мөчид өгнө
        attackTarget = player;
        attackHitTime = Time.time + attackHitDelay;
        attackPending = true;
    }


    // =====================================================
    // ANIMATION - ATTACK DAMAGE
    // =====================================================

    private void UpdateAttackDamage()
    {
        if (!attackPending)
            return;

        if (Time.time < attackHitTime)
            return;

        attackPending = false;

        if (attackTarget == null)
            return;

        // Цохилтын үед Player ойрхон байгаа эсэх
        float distance =
            GetHorizontalDistance(attackTarget);

        if (!CombatPhysics.CanMeleeHit(transform, attackTarget, attackDistance, meleeVerticalRange))
        {
            attackTarget = null;
            return;
        }

        PlayerHealth playerHealth =
            attackTarget.GetComponent<PlayerHealth>();

        // Health script байгаа бол damage өгнө
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                attackDamage
            );
        }

        attackTarget = null;
    }


    // =====================================================
    // ANIMATION - MOVEMENT
    // =====================================================

    private void UpdateMovementAnimation()
    {
        if (animator == null || agent == null || !agent.isOnNavMesh)
            return;

        bool isMoving =
            !agent.isStopped &&
            agent.velocity.magnitude > movingThreshold;

        SetMoving(isMoving);
    }


    private void SetMoving(bool value)
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", value);
        }
    }


    // =====================================================
    // ANIMATION - DEATH
    // =====================================================

    private void HandleDeath()
    {
        // Үхсэн үед AI зогсоно
        StopMoving();
        SetMoving(false);

        // Хүлээгдэж байгаа Attack damage-ийг цуцална
        attackPending = false;
        attackTarget = null;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        // Death animation-ийг Enemy.cs ажиллуулна
        enabled = false;
    }


    // =====================================================
    // DISTANCE
    // =====================================================

    private float GetHorizontalDistance(
        Transform player)
    {
        Vector3 difference =
            player.position - transform.position;

        // Дээш доош зайг тооцохгүй
        difference.y = 0f;

        return difference.magnitude;
    }
}
