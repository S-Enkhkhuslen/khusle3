using UnityEngine;

public class EnemySensor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform sensorPoint;
    [Header("Sensor Settings")]
    [SerializeField] private float viewDistance = 50f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 160f;
    [SerializeField] private float playerTargetHeight = 1f;
    [SerializeField] private LayerMask sensorMask;
    [Header("Detection Memory")]
    [SerializeField] private float loseSightDelay = 1f;

    public bool PlayerDetected { get; private set; }
    public Transform PlayerTransform { get; private set; }
    private float lastSeenTime = float.NegativeInfinity;
    private PlayerHealth playerHealth;
    private Enemy enemy;

    private void Start()
    {
        enemy = GetComponent<Enemy>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        PlayerTransform = player.transform;
        playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (PlayerTransform == null || sensorPoint == null
            || !PlayerTransform.gameObject.activeInHierarchy
            || (playerHealth != null && playerHealth.IsDead)
            || (enemy != null && enemy.IsDead))
        {
            PlayerDetected = false;
            return;
        }

        Vector3 target = PlayerTransform.position + Vector3.up * playerTargetHeight;
        Vector3 direction = target - sensorPoint.position;
        float distance = direction.magnitude;
        Vector3 horizontalDirection = direction;
        horizontalDirection.y = 0f;
        Vector3 horizontalForward = transform.forward;
        horizontalForward.y = 0f;
        float angle = horizontalDirection.sqrMagnitude > 0.000001f
            ? Vector3.Angle(horizontalForward, horizontalDirection) : 0f;

        // Өнцгийг хэвтээгээр, зай болон харагдах эсэхийг бүтэн 3D-ээр шалгана.
        if (distance <= viewDistance && angle <= viewAngle * 0.5f
            && Physics.Raycast(sensorPoint.position, direction.normalized, out RaycastHit hit,
                distance + 0.5f, sensorMask, QueryTriggerInteraction.Ignore)
            && (hit.transform == PlayerTransform || hit.transform.IsChildOf(PlayerTransform)))
            lastSeenTime = Time.time;

        PlayerDetected = Time.time - lastSeenTime <= loseSightDelay;
    }

    private void OnDrawGizmosSelected()
    {
        if (sensorPoint == null) return;
        Gizmos.DrawWireSphere(sensorPoint.position, viewDistance);
        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawRay(sensorPoint.position, left * viewDistance);
        Gizmos.DrawRay(sensorPoint.position, right * viewDistance);
    }
}
