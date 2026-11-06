using UnityEngine;

public class TankGenerator : MonoBehaviour
{
    [SerializeField] private GameObject Tank;
    [Min(0)] public int tank = 8;
    [Min(0f)] public float spawnRadius = 30f;
    [Min(0f)] public float height = 0.25f;
    [SerializeField] private LayerMask groundMask = 1 << 3;

    private void Start()
    {
        if (Tank == null) return;
        for (int i = 0; i < tank; i++)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * spawnRadius;
                Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);
                if (!TryGround(candidate, out Vector3 groundPoint)) continue;
                GameObject pickup = Instantiate(Tank, groundPoint + Vector3.up * height, Quaternion.identity);
                // The pickup rotates, so use its bounding sphere to keep its bottom clear of terrain.
                Collider collider = pickup.GetComponentInChildren<Collider>();
                if (collider != null)
                {
                    Physics.SyncTransforms();
                    float bottom = collider.bounds.center.y - collider.bounds.extents.magnitude;
                    pickup.transform.position += Vector3.up * Mathf.Max(0f, groundPoint.y + height - bottom);
                }
                break;
            }
        }
    }

    private bool TryGround(Vector3 position, out Vector3 point)
    {
        return GroundUtility.TrySample(position, groundMask, out point, 1000f);
    }
}
