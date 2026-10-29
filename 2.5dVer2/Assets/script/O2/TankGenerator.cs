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
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain.terrainData == null) continue;
            Vector3 local = position - terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z) continue;
            point = new Vector3(position.x, terrain.transform.position.y + terrain.SampleHeight(position), position.z);
            return true;
        }
        if (Physics.Raycast(position + Vector3.up * 1000f, Vector3.down, out RaycastHit hit,
            2000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }
        point = default;
        return false;
    }
}
