using UnityEngine;

// Terrain height is shared by enemy and oxygen-pickup spawning.
public static class GroundUtility
{
    public static Vector3 Sample(Vector3 position, LayerMask mask)
    {
        return TrySample(position, mask, out Vector3 ground) ? ground : position;
    }
    public static bool TrySample(Vector3 position, LayerMask mask, out Vector3 ground, float rayHeight = 100f)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain.terrainData == null) continue;
            Vector3 local = position - terrain.transform.position, size = terrain.terrainData.size;
            if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z) continue;
            ground = new Vector3(position.x, terrain.transform.position.y + terrain.SampleHeight(position), position.z);
            return true;
        }
        if (Physics.Raycast(position + Vector3.up * rayHeight, Vector3.down, out RaycastHit hit,
            rayHeight * 2f, mask, QueryTriggerInteraction.Ignore))
        { ground = hit.point; return true; }
        ground = position; return false;
    }
}
