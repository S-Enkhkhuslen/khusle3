using UnityEngine;
using Unity.Cinemachine;

// Wrap before Cinemachine/weather LateUpdate so they see the destination this frame.
[DefaultExecutionOrder(-10)]
public class WorldWrap : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private Terrain terrain;
    [Header("Settings")]
    [Min(0f)] [SerializeField] private float insideOffset = 5f;

    private CharacterController characterController;
    private Jetpack jetpack;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        jetpack = GetComponent<Jetpack>();
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("WorldWrap: Terrain олдсонгүй!", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f || terrain == null || terrain.terrainData == null) return;
        Vector3 origin = terrain.transform.position, size = terrain.terrainData.size;
        // Both sides use the same inset: never walk off the collider before wrapping.
        float marginX = Mathf.Clamp(insideOffset, 0f, size.x * 0.49f);
        float marginZ = Mathf.Clamp(insideOffset, 0f, size.z * 0.49f);
        Vector3 position = transform.position;
        position.x = Wrap(position.x, origin.x + marginX, origin.x + size.x - marginX);
        position.z = Wrap(position.z, origin.z + marginZ, origin.z + size.z - marginZ);
        if (position.x != transform.position.x || position.z != transform.position.z)
            Teleport(position);
    }

    private static float Wrap(float value, float min, float max)
    {
        // Keep the distance travelled past an edge, even for a fast diagonal crossing.
        if (max <= min || (value >= min && value < max)) return value;
        return min + Mathf.Repeat(value - min, max - min);
    }

    private void Teleport(Vector3 newPosition)
    {
        Vector3 oldPosition = transform.position;
        float oldGround = terrain.transform.position.y + terrain.SampleHeight(oldPosition);
        float newGround = terrain.transform.position.y + terrain.SampleHeight(newPosition);
        float bottomOffset = characterController != null
            ? (characterController.center.y - characterController.height * 0.5f) * transform.lossyScale.y : 0f;
        float feetAboveGround = oldPosition.y + bottomOffset - oldGround;
        bool grounded = characterController != null && characterController.isGrounded
            && (jetpack == null || jetpack.VerticalVelocity <= 0f);
        float clearance = grounded ? 0.05f : Mathf.Max(0.05f, feetAboveGround);
        newPosition.y = newGround - bottomOffset + clearance;

        bool wasEnabled = characterController != null && characterController.enabled;
        if (wasEnabled) characterController.enabled = false;
        transform.position = newPosition;
        Physics.SyncTransforms();
        if (wasEnabled) characterController.enabled = true;
        if (grounded) jetpack?.ResetVerticalVelocity();

        Vector3 delta = newPosition - oldPosition;
        // Follow/LookAt can point at a child such as the astronaut's camera target.
        foreach (Transform target in GetComponentsInChildren<Transform>(true))
            CinemachineCore.OnTargetObjectWarped(target, delta);
        PlanetDustWeather.OnPlayerWarped(transform, delta);
    }
}
