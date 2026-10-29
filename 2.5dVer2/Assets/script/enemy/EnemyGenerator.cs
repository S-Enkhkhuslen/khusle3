using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class EnemyGenerator : MonoBehaviour
{
    [Header("Spawn Enemy's")]
    public GameObject MeleePrefab;
    public GameObject RangePrefab;
    public GameObject FlyingPrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform Location;
    [SerializeField, Min(0.1f)] private float navMeshSearchRadius = 5f;
    [SerializeField] private LayerMask groundMask = 1 << 3;

    [Header("Interaction")]
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float interactionRadius = 3f;
    [SerializeField, Min(0.1f)] private float interactionHeight = 4f;

    [Header("Random Spawn")]
    [SerializeField, Min(1)] private int minEnemyCount = 10;
    [SerializeField, Min(1)] private int maxEnemyCount = 20;
    [SerializeField, Min(1f)] private float spawnRadius = 20f;
    [SerializeField, Min(1f)] private float spawnSpacing = 3f;
    [SerializeField, Min(1f)] private float flyingSpawnHeight = 2f;

    private Vector3 interactionPosition;
    private bool isPlayerInRange;
    private float nextGroundRefresh;
    private Vector3 markerCenter;
    private float markerRadius = -1f;
    private LineRenderer rangeMarker;
    private Material markerMaterial;
    private GUIStyle labelStyle;
    private Camera promptCamera;
    private bool used;

    private void Start()
    {
        RefreshGroundPosition();
        CreateRangeMarker();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextGroundRefresh)
            RefreshGroundPosition();

        FindPlayer();
        UpdateInteractionState();
        UpdateRangeMarker();

        if (isPlayerInRange && Time.timeScale > 0f && Keyboard.current != null
            && Keyboard.current.eKey.wasPressedThisFrame)
        {
            SpawnEnemies();
        }
    }

    private void FindPlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void UpdateInteractionState()
    {
        isPlayerInRange = false;
        if (player == null || !player.gameObject.activeInHierarchy)
            return;

        Vector3 difference = player.position - interactionPosition;
        float heightDifference = Mathf.Abs(difference.y);
        difference.y = 0f;
        float radius = Mathf.Max(0.1f, interactionRadius);
        isPlayerInRange = difference.sqrMagnitude <= radius * radius
            && heightDifference <= Mathf.Max(0.1f, interactionHeight);
    }

    private void RefreshGroundPosition()
    {
        // The generator's pivot may be above the terrain. Reach is measured
        // horizontally from its ground position, with a separate height limit.
        interactionPosition = GroundPosition(transform.position);
        nextGroundRefresh = Time.unscaledTime + 0.5f;
    }

    private Vector3 GroundPosition(Vector3 position)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain.terrainData == null)
                continue;

            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (position.x >= origin.x && position.x <= origin.x + size.x
                && position.z >= origin.z && position.z <= origin.z + size.z)
            {
                position.y = origin.y + terrain.SampleHeight(position);
                return position;
            }
        }

        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down,
            out RaycastHit hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point;

        return position;
    }

    public GameObject SpawnMelee()
    {
        return SpawnEnemy(MeleePrefab, 0f, "Melee", null);
    }

    public GameObject SpawnRange()
    {
        return SpawnEnemy(RangePrefab, 0f, "Ranged", null);
    }

    public GameObject SpawnFlying()
    {
        return SpawnEnemy(FlyingPrefab, flyingSpawnHeight, "Flying", null);
    }

    public void SpawnEnemies()
    {
        if (used) return;
        var prefabs = new List<GameObject>();
        if (MeleePrefab != null) prefabs.Add(MeleePrefab);
        if (RangePrefab != null) prefabs.Add(RangePrefab);
        if (FlyingPrefab != null) prefabs.Add(FlyingPrefab);
        if (prefabs.Count == 0)
        {
            ReportSpawnIssue("No enemy prefabs are assigned.");
            return;
        }

        int minimum = Mathf.Max(1, minEnemyCount);
        int count = Random.Range(minimum, Mathf.Max(minimum, maxEnemyCount) + 1);
        var positions = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            float height = prefab == FlyingPrefab ? flyingSpawnHeight : 0f;
            SpawnEnemy(prefab, height, prefab.name, positions);
        }

        if (positions.Count < count)
            ReportSpawnIssue("Spawned " + positions.Count + "/" + count
                + " enemies. Not enough free NavMesh space; increase Spawn Radius.");

        if (positions.Count > 0)
        {
            used = true;
            isPlayerInRange = false;
            enabled = false;
            CombatEffects.Spawn(GroundPosition(transform.position) + Vector3.up);
            Destroy(gameObject);
        }
    }

    private GameObject SpawnEnemy(GameObject prefab, float height, string enemyType,
        List<Vector3> positions)
    {
        if (prefab == null)
        {
            ReportSpawnIssue(enemyType + " prefab is not assigned.");
            return null;
        }

        Transform spawnPoint = Location != null ? Location : transform;
        Vector3 center = GroundPosition(spawnPoint.position);
        NavMeshAgent prefabAgent = prefab.GetComponent<NavMeshAgent>();
        var filter = new NavMeshQueryFilter
        {
            agentTypeID = prefabAgent != null ? prefabAgent.agentTypeID : 0,
            areaMask = prefabAgent != null ? prefabAgent.areaMask : NavMesh.AllAreas
        };

        float radius = Mathf.Max(1f, spawnRadius);
        float spacing = Mathf.Max(1f, spawnSpacing);
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 source = GroundPosition(center + new Vector3(offset.x, 0f, offset.y));
            if (!NavMesh.SamplePosition(source, out NavMeshHit hit,
                Mathf.Max(0.1f, navMeshSearchRadius), filter))
                continue;

            // Sampling can move the point, so check the final position as well.
            Vector2 fromCenter = new Vector2(hit.position.x - center.x, hit.position.z - center.z);
            if (fromCenter.sqrMagnitude > radius * radius
                || fromCenter.sqrMagnitude < spacing * spacing)
                continue;

            bool occupied = false;
            if (positions != null)
            {
                foreach (Vector3 previous in positions)
                {
                    Vector2 difference = new Vector2(hit.position.x - previous.x,
                        hit.position.z - previous.z);
                    if (difference.sqrMagnitude < spacing * spacing)
                    {
                        occupied = true;
                        break;
                    }
                }
            }
            if (occupied) continue;

            Vector3 position = hit.position + Vector3.up * height;
            // Avoid enemies from earlier presses and the player.
            foreach (Collider nearby in Physics.OverlapSphere(position + Vector3.up,
                spacing, ~0, QueryTriggerInteraction.Collide))
            {
                if (nearby.GetComponentInParent<Enemy>() != null
                    || nearby.GetComponentInParent<PlayerHealth>() != null)
                {
                    occupied = true;
                    break;
                }
            }
            if (occupied) continue;

            GameObject enemy = Instantiate(prefab, position,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            CombatEffects.Spawn(position + Vector3.up);
            if (positions != null) positions.Add(hit.position);
            return enemy;
        }

        if (positions == null)
            ReportSpawnIssue("No free NavMesh space for " + enemyType + " inside Spawn Radius.");
        return null;
    }

    private void ReportSpawnIssue(string message)
    {
        Debug.LogWarning("EnemyGenerator: " + message, this);
    }

    private void CreateRangeMarker()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return;

        GameObject marker = new GameObject("Generator Interaction Range");
        marker.transform.SetParent(transform, false);
        rangeMarker = marker.AddComponent<LineRenderer>();
        rangeMarker.useWorldSpace = true;
        rangeMarker.loop = true;
        rangeMarker.positionCount = 64;
        rangeMarker.widthMultiplier = 0.12f;
        rangeMarker.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rangeMarker.receiveShadows = false;
        markerMaterial = new Material(shader);
        rangeMarker.sharedMaterial = markerMaterial;
        UpdateRangeMarker();
    }

    private void UpdateRangeMarker()
    {
        if (rangeMarker == null)
            return;

        float radius = Mathf.Max(0.1f, interactionRadius);
        if (markerCenter != interactionPosition || markerRadius != radius)
        {
            for (int i = 0; i < rangeMarker.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / rangeMarker.positionCount;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                rangeMarker.SetPosition(i, GroundPosition(interactionPosition + offset) + Vector3.up * 0.15f);
            }
            markerCenter = interactionPosition;
            markerRadius = radius;
        }

        Color color = isPlayerInRange ? Color.green : Color.cyan;
        if (markerMaterial.HasProperty("_BaseColor")) markerMaterial.SetColor("_BaseColor", color);
        if (markerMaterial.HasProperty("_Color")) markerMaterial.SetColor("_Color", color);
    }

    private Camera GetPromptCamera()
    {
        if (promptCamera != null && promptCamera.isActiveAndEnabled)
            return promptCamera;

        promptCamera = StatusBarUI.FindGameCamera();
        return promptCamera;
    }

    private void OnGUI()
    {
        if (!isPlayerInRange)
            return;

        Camera view = GetPromptCamera();
        if (view == null)
            return;

        // Keep the prompt next to the generator instead of at the screen center.
        Vector3 anchor = interactionPosition + Vector3.up * 1.6f
            + view.transform.right * 1.2f;
        Vector3 screenPoint = view.WorldToScreenPoint(anchor);
        if (screenPoint.z <= 0f || screenPoint.x < 0f || screenPoint.x > Screen.width
            || screenPoint.y < 0f || screenPoint.y > Screen.height)
            return;

        if (labelStyle == null)
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = false
            };

        float uiScale = Mathf.Max(0.75f, Screen.height / 900f);
        labelStyle.fontSize = (int)(22f * uiScale);
        float width = 240f * uiScale;
        float height = 38f * uiScale;
        Rect popup = new Rect(screenPoint.x - width * 0.5f,
            Screen.height - screenPoint.y - height * 0.5f, width, height);
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = -1000;
        GUI.color = new Color(0.02f, 0.025f, 0.035f, 0.8f);
        GUI.DrawTexture(popup, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(popup, "E Spawn enemies", labelStyle);
        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    private void OnEnable()
    {
        if (rangeMarker != null) rangeMarker.enabled = true;
    }

    private void OnDisable()
    {
        if (rangeMarker != null) rangeMarker.enabled = false;
    }

    private void OnDestroy()
    {
        if (markerMaterial != null) Destroy(markerMaterial);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? interactionPosition : transform.position,
            Mathf.Max(0.1f, interactionRadius));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Location != null ? Location.position : transform.position,
            Mathf.Max(1f, spawnRadius));
    }
}
