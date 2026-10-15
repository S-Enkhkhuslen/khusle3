using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class MeleeEnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySensor sensor;
    [SerializeField] private NavMeshAgent agent;

    [Header("Movement")]
    [SerializeField] private float attackDistance = 2f;


    private void Update()
    {
        // Sensor эсвэл Agent холбоогүй бол ажиллахгүй
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


        // Player хүртэлх зайг олно
        float distance =
            GetHorizontalDistance(player);


        // Player attack хийх зайнаас хол байгаа бол
        if (distance > attackDistance)
        {
            Debug.Log("Chase");
            ChasePlayer(player);
        }
        else
        {
            Debug.Log("Stop");
            // Attack хийх зайд ирсэн тул зогсоно
            StopMoving();
        }
    }


    private void ChasePlayer(Transform player)
    {
        // Enemy NavMesh дээр байхгүй бол
        // SetDestination хийж болохгүй
        if (!agent.isOnNavMesh)
            return;


        agent.isStopped = false;

        agent.SetDestination(
            player.position
        );
    }


    private void StopMoving()
    {
        if (!agent.isOnNavMesh)
            return;


        agent.isStopped = true;
    }


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