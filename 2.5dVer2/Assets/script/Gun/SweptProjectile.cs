using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class SweptProjectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] protected float lifeTime = 10f;
    private Rigidbody body;
    private Collider projectileCollider;
    private Vector3 velocity;
    private float radius;
    private bool launched;
    private bool impacted;
    protected Transform Owner { get; private set; }

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody>();
        projectileCollider = GetComponent<Collider>();
        body.useGravity = false;
        body.isKinematic = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        if (projectileCollider != null) projectileCollider.isTrigger = true;
    }

    protected void Initialize(Vector3 direction, float speed, Transform owner)
    {
        if (projectileCollider == null || direction.sqrMagnitude < 0.0001f)
        {
            Destroy(gameObject);
            return;
        }
        Owner = owner;
        velocity = direction.normalized * Mathf.Max(0f, speed);
        SphereCollider sphere = projectileCollider as SphereCollider;
        Vector3 scale = transform.lossyScale;
        radius = sphere != null
            ? sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))
            : Mathf.Max(0.025f, projectileCollider.bounds.extents.magnitude);
        radius = Mathf.Max(0.01f, radius);
        launched = true;
        Destroy(gameObject, Mathf.Max(0.1f, lifeTime));
    }

    private void FixedUpdate()
    {
        if (!launched || impacted) return;
        Vector3 position = body.position;
        // Sphere casts do not report colliders overlapping the start of the sweep.
        foreach (Collider other in Physics.OverlapSphere(position, radius, ~0, QueryTriggerInteraction.Collide))
            if (TryHit(other)) return;

        Vector3 displacement = velocity * Time.fixedDeltaTime;
        float distance = displacement.magnitude;
        if (distance <= 0f) return;
        RaycastHit[] hits = Physics.SphereCastAll(position, radius, displacement / distance,
            distance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (ShouldIgnore(hit.collider)) continue;
            body.position = position + displacement.normalized * Mathf.Max(0f, hit.distance);
            if (TryHit(hit.collider)) return;
        }
        body.MovePosition(position + displacement);
    }

    private bool ShouldIgnore(Collider other)
    {
        return other == null || other == projectileCollider
            || other.GetComponentInParent<SweptProjectile>() != null
            || (Owner != null && other.transform.IsChildOf(Owner))
            || IgnoreTarget(other);
    }

    private bool TryHit(Collider other)
    {
        if (impacted || ShouldIgnore(other)) return false;
        impacted = true; // Destruction is deferred; suppress every other callback this frame.
        OnImpact(other);
        projectileCollider.enabled = false;
        Destroy(gameObject);
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (launched) TryHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (launched) TryHit(collision.collider);
    }

    protected abstract bool IgnoreTarget(Collider other);
    protected abstract void OnImpact(Collider other);
}
