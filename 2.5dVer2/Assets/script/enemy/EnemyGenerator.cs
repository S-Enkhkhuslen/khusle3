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

    [Header("Level Finish")]
    [SerializeField] private bool unlockEscapeRocket;

    private Vector3 interactionPosition;
    private bool isPlayerInRange;
    private float nextGroundRefresh;
    private GeneratorDisplay display;
    private bool used;

    private void Start()
    {
        RefreshGroundPosition();
        display = new GeneratorDisplay(transform);
        UpdateRangeMarker();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextGroundRefresh)
            RefreshGroundPosition();

        FindPlayer();
        UpdateInteractionState();
        UpdateRangeMarker();
        display?.UpdatePrompt(interactionPosition, isPlayerInRange);

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
        return GroundUtility.Sample(position, groundMask);
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
        var spawnedEnemies = new List<Enemy>();
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            float height = prefab == FlyingPrefab ? flyingSpawnHeight : 0f;
            GameObject spawned = SpawnEnemy(prefab, height, prefab.name, positions);
            if (spawned != null && spawned.TryGetComponent(out Enemy enemy)) spawnedEnemies.Add(enemy);
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
            GameAudio.RegisterBattle(spawnedEnemies);
            GameAudio.GeneratorActivated(GroundPosition(transform.position));
            if (unlockEscapeRocket)
            {
                FindPlayer();
                EscapeRocket.Spawn(GroundPosition(transform.position), player);
            }
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

    private void UpdateRangeMarker()
    {
        display?.UpdateRange(interactionPosition, Mathf.Max(0.1f, interactionRadius), isPlayerInRange, groundMask);
    }
    private void OnEnable() { display?.SetVisible(true); }
    private void OnDisable() { display?.SetVisible(false); }
    private void OnDestroy() { display?.Dispose(); }

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
