using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(75)]
public class FlyingMovement : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField, Min(0.01f)] private float skin = 0.03f;
    private CapsuleCollider body;

    private void Awake()
    {
        body = GetComponent<CapsuleCollider>();
    }

    public void MoveTowards(Vector3 target, float speed)
    {
        ResolveOverlaps();
        Vector3 remaining = Vector3.ClampMagnitude(target - transform.position,
            Mathf.Max(0f, speed) * Time.deltaTime);

        // Sweep the whole body, then slide along the surface instead of crossing it.
        for (int i = 0; i < 3 && remaining.sqrMagnitude > 0.000001f; i++)
        {
            GetCapsule(out Vector3 a, out Vector3 b, out float radius);
            float distance = remaining.magnitude;
            Vector3 direction = remaining / distance;
            RaycastHit nearest = default;
            bool blocked = false;
            foreach (RaycastHit hit in Physics.CapsuleCastAll(a, b, radius, direction,
                distance + skin, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (!blocked || hit.distance < nearest.distance)
                {
                    nearest = hit;
                    blocked = true;
                }
            }

            if (!blocked)
            {
                transform.position += remaining;
                break;
            }

            Vector3 travel = direction * Mathf.Clamp(nearest.distance - skin, 0f, distance);
            transform.position += travel;
            remaining = Vector3.ProjectOnPlane(remaining - travel, nearest.normal);
        }
    }

    private void LateUpdate()
    {
        // Facing the player can rotate the capsule into a nearby wall.
        ResolveOverlaps();
    }

    private void ResolveOverlaps()
    {
        if (body == null || !body.enabled) return;
        for (int pass = 0; pass < 3; pass++)
        {
            GetCapsule(out Vector3 a, out Vector3 b, out float radius);
            bool corrected = false;
            foreach (Collider obstacle in Physics.OverlapCapsule(a, b, radius,
                obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (obstacle.transform.IsChildOf(transform)) continue;
                if (!Physics.ComputePenetration(body, transform.position, transform.rotation,
                    obstacle, obstacle.transform.position, obstacle.transform.rotation,
                    out Vector3 direction, out float distance)) continue;
                transform.position += direction * (distance + skin);
                corrected = true;
            }
            if (!corrected) break;
        }
    }

    private void GetCapsule(out Vector3 a, out Vector3 b, out float radius)
    {
        Vector3 scale = transform.lossyScale;
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Vector3 axis = body.direction == 0 ? transform.right
            : body.direction == 2 ? transform.forward : transform.up;
        float heightScale = body.direction == 0 ? scale.x : body.direction == 2 ? scale.z : scale.y;
        float radiusScale = body.direction == 0 ? Mathf.Max(scale.y, scale.z)
            : body.direction == 2 ? Mathf.Max(scale.x, scale.y) : Mathf.Max(scale.x, scale.z);
        radius = Mathf.Max(0.01f, body.radius * radiusScale);
        float halfLength = Mathf.Max(0f, body.height * heightScale * 0.5f - radius);
        Vector3 center = transform.TransformPoint(body.center);
        a = center + axis * halfLength;
        b = center - axis * halfLength;
    }
}
