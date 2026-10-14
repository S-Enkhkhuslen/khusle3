using UnityEngine;

public class EnemySensor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform sensorPoint;

    [Header("Sensor Settings")]
    [SerializeField] private float viewDistance = 25f;

    [SerializeField, Range(0f, 360f)]
    private float viewAngle = 120f;

    [SerializeField] private float playerTargetHeight = 1f;

    [SerializeField] private LayerMask sensorMask;

    [Header("Detection Memory")]
    [SerializeField] private float loseSightDelay = 1f;


    public bool PlayerDetected { get; private set; }

    public Transform PlayerTransform { get; private set; }


    private float lastSeenTime = -999f;


    private void Start()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            PlayerTransform = player.transform;
        }
    }


    private void Update()
    {
        DetectPlayer();
    }


    private void DetectPlayer()
    {
        if (PlayerTransform == null || sensorPoint == null)
        {
            PlayerDetected = false;
            return;
        }


        Vector3 targetPosition =
            PlayerTransform.position
            + Vector3.up * playerTargetHeight;


        Vector3 directionToPlayer =
            targetPosition - sensorPoint.position;


        float distanceToPlayer =
            directionToPlayer.magnitude;


        // Хэт хол байгаа эсэх
        if (distanceToPlayer <= viewDistance)
        {
            directionToPlayer.Normalize();


            // Харах өнцөг
            float angle =
                Vector3.Angle(
                    transform.forward,
                    directionToPlayer
                );


            if (angle <= viewAngle / 2f)
            {
                RaycastHit hit;


                // Sensor -> Player чиглэлд Raycast
                if (Physics.Raycast(
                    sensorPoint.position,
                    directionToPlayer,
                    out hit,
                    distanceToPlayer + 0.5f,
                    sensorMask,
                    QueryTriggerInteraction.Ignore))
                {
                    // Ray хамгийн түрүүн Player-д хүрсэн үү?
                    if (hit.transform.root.CompareTag("Player"))
                    {
                        lastSeenTime = Time.time;

                        Debug.DrawRay(
                            sensorPoint.position,
                            directionToPlayer * distanceToPlayer,
                            Color.green
                        );
                    }
                    else
                    {
                        Debug.DrawRay(
                            sensorPoint.position,
                            directionToPlayer * distanceToPlayer,
                            Color.red
                        );
                    }
                }
            }
        }


        PlayerDetected =
            Time.time - lastSeenTime <= loseSightDelay;
    }


    private void OnDrawGizmosSelected()
    {
        if (sensorPoint == null)
            return;


        Gizmos.DrawWireSphere(
            sensorPoint.position,
            viewDistance
        );


        float halfAngle =
            viewAngle / 2f;


        Vector3 leftDirection =
            Quaternion.Euler(0f, -halfAngle, 0f)
            * transform.forward;


        Vector3 rightDirection =
            Quaternion.Euler(0f, halfAngle, 0f)
            * transform.forward;


        Gizmos.DrawRay(
            sensorPoint.position,
            leftDirection * viewDistance
        );


        Gizmos.DrawRay(
            sensorPoint.position,
            rightDirection * viewDistance
        );
    }
}