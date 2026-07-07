using UnityEngine;

public class WorldWrap : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private Terrain terrain;

    [Header("Player Controller Script")]
    [SerializeField] private Controller playerController;

    [Header("Settings")]
    [SerializeField] private float insideOffset = 5f;
    [SerializeField] private float heightOffset = 1f;

    private Vector3 terrainPosition;
    private Vector3 terrainSize;

    private void Start()
    {
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (terrain == null)
        {
            Debug.LogError("WorldWrap: Terrain олдсонгүй!");
            return;
        }

        if (playerController == null)
        {
            playerController = GetComponent<Controller>();
        }

        terrainPosition = terrain.transform.position;
        terrainSize = terrain.terrainData.size;

        Debug.Log($"Terrain X: {terrainPosition.x} ~ {terrainPosition.x + terrainSize.x}");
        Debug.Log($"Terrain Z: {terrainPosition.z} ~ {terrainPosition.z + terrainSize.z}");
    }

    private void LateUpdate()
    {
        if (terrain == null)
            return;

        Vector3 pos = transform.position;
        bool shouldWrap = false;

        float minX = terrainPosition.x;
        float maxX = terrainPosition.x + terrainSize.x;

        float minZ = terrainPosition.z;
        float maxZ = terrainPosition.z + terrainSize.z;

        if (pos.x < minX)
        {
            pos.x = maxX - insideOffset;
            shouldWrap = true;
        }
        else if (pos.x > maxX)
        {
            pos.x = minX + insideOffset;
            shouldWrap = true;
        }

        if (pos.z < minZ)
        {
            pos.z = maxZ - insideOffset;
            shouldWrap = true;
        }
        else if (pos.z > maxZ)
        {
            pos.z = minZ + insideOffset;
            shouldWrap = true;
        }

        if (shouldWrap)
        {
            Teleport(pos);
        }
    }

    private void Teleport(Vector3 newPosition)
    {
        float terrainHeight = terrain.SampleHeight(newPosition);
        newPosition.y = terrain.transform.position.y + terrainHeight + heightOffset;

        // Teleport хийх үед Controller script-ийг түр унтраана.
        // Ингэхгүй бол controller дараагийн frame дээр хуучин хөдөлгөөнөө үргэлжлүүлээд эвгүйтэж магадгүй.
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        transform.position = newPosition;

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Debug.Log($"Wrapped to: {newPosition}");
    }
}