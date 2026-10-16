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


    private EnemySensor sensor;
    private NavMeshAgent agent;

    // Дараагийн удаа хэдэн секундэд attack хийж болохыг хадгална
    private float nextAttackTime;


    private void Awake()
    {
        sensor = GetComponent<EnemySensor>();

        agent = GetComponent<NavMeshAgent>();


        // Enemy-ийн rotation-ийг бид өөрсдөө удирдана
        if (agent != null)
        {
            agent.updateRotation = false;
        }
    }


    private void Update()
    {
        // Sensor эсвэл Agent байхгүй бол ажиллахгүй
        if (sensor == null || agent == null)
            return;


        // Player detect хийгээгүй бол зогсоно
        if (!sensor.PlayerDetected)
        {
            StopMoving();
            return;
        }


        // Sensor-оос Player-ийн Transform авна
        Transform player =
            sensor.PlayerTransform;


        if (player == null)
            return;


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

            AttackPlayer(player);
        }
    }


    // =====================================================
    // CHASE
    // =====================================================

    private void ChasePlayer(Transform player)
    {
        if (!agent.isOnNavMesh)
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
        if (!agent.isOnNavMesh)
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
        // Cooldown дуусаагүй бол attack хийхгүй
        if (Time.time < nextAttackTime)
            return;


        // Дараагийн attack хийх боломжтой хугацааг тооцно
        nextAttackTime =
            Time.time + attackCooldown;


        Debug.Log("MELEE ATTACK");


        // Player дээрх Health script-ийг хайна
        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();


        // Health script байгаа бол damage өгнө
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                attackDamage
            );
        }
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