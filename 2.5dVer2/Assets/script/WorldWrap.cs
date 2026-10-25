using UnityEngine;

public class WorldWrap : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private Terrain terrain;


    [Header("Settings")]
    [SerializeField] private float insideOffset = 5f;

    private CharacterController characterController;
    private Jetpack jetpack;
    private Vector3 terrainPosition;
    private Vector3 terrainSize;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        jetpack = GetComponent<Jetpack>();
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (terrain == null)
        {
            Debug.LogError("WorldWrap: Terrain олдсонгүй!");
            return;
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
        float oldGround = terrain.transform.position.y + terrain.SampleHeight(transform.position);
        float newGround = terrain.transform.position.y + terrain.SampleHeight(newPosition);
        float bottomOffset = characterController != null
            ? (characterController.center.y - characterController.height * 0.5f) * transform.lossyScale.y : 0f;
        float feetAboveGround = transform.position.y + bottomOffset - oldGround;
        bool airborne = characterController != null && !characterController.isGrounded;
        float clearance = airborne ? Mathf.Max(0.05f, feetAboveGround) : 0.05f;
        newPosition.y = newGround - bottomOffset + clearance;

        bool wasEnabled = characterController != null && characterController.enabled;
        if (wasEnabled) characterController.enabled = false;
        transform.position = newPosition;
        Physics.SyncTransforms();
        if (wasEnabled) characterController.enabled = true;
        if (!airborne) jetpack?.ResetVerticalVelocity();
    }
}
