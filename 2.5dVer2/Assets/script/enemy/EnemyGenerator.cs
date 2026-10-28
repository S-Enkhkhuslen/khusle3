using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(100)]
public class EnemyGenerator : MonoBehaviour
{
    [Header("Spawn Enemy's")]
    [SerializeField] public GameObject MeleePrefab;
    [SerializeField] public GameObject RangePrefab;
    [SerializeField] public GameObject FlyingPrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform Location;
    [SerializeField, Min(0.1f)] private float navMeshSearchRadius = 5f;

    private void Start()
    {
        SpawnRange();
    }

    public GameObject SpawnMelee()
    {
        if (MeleePrefab == null)
        {
            Debug.LogWarning("EnemyGenerator: Assign the MeleeE prefab to MeleePrefab.", this);
            return null;
        }

        Transform spawnPoint = Location != null ? Location : transform;
        NavMeshAgent prefabAgent = MeleePrefab.GetComponent<NavMeshAgent>();
        var filter = new NavMeshQueryFilter
        {
            agentTypeID = prefabAgent != null ? prefabAgent.agentTypeID : 0,
            areaMask = prefabAgent != null ? prefabAgent.areaMask : NavMesh.AllAreas
        };

        if (!NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit,
            Mathf.Max(0.1f, navMeshSearchRadius), filter))
        {
            Debug.LogWarning("EnemyGenerator: No walkable NavMesh near the spawn point. Move the generator onto the baked terrain.", this);
            return null;
        }

        return Instantiate(MeleePrefab, hit.position, spawnPoint.rotation);
    }
    public GameObject SpawnRange()
    {
        if(RangePrefab == null)
        {
            Debug.LogWarning("EnemyGenerator: RangeE g RangePreFab luu hiigeeche", this);
            return null;
        }

        Transform spawnPoint = Location != null ? Location : transform;
        NavMeshAgent prefabAgent = RangePrefab.GetComponent<NavMeshAgent>();

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = prefabAgent != null ? prefabAgent.agentTypeID : 0,
            areaMask = prefabAgent != null ? prefabAgent.areaMask : NavMesh.AllAreas
        };

        if (!NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit,
            Mathf.Max(0, 1f, navMeshSearchRadius), filter))
        {
            Debug.LogWarning("EnemyGenerator: oir havid hiigdsen navmesh alga. navmesh baigaa terrain deer avaach.", this);
            return null;
        }
        return Instantiate(RangePrefab, hit.position, spawnPoint.rotation);
    }
    public GameObject SpawnFlying()
    {
        if(FlyingPrefab == null)
        {
            Debug.LogWarning("EnemyGenerator: FlyingE g FlyingPreFab luu hiigeeche", this);
            return null;
        }

        Transform spawnPoint = Location != null ? Location : transform;
        NavMeshAgent prefabAgent = FlyingPrefab.GetComponent<NavMeshAgent>();

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = prefabAgent != null ? prefabAgent.agentTypeID : 0,
            areaMask = prefabAgent != null ? prefabAgent.areaMask : NavMesh.AllAreas
        };

        if(!NavMesh.SamplePosition(spawnPoint.position,out NavMeshHit hit,
            Mathf.Max(0.1f, navMeshSearchRadius), filter))
        {
            Debug.LogWarning("EnemyGenerator: oir havid hiigdsen navmesh alga. navmesh baigaa terrain deer avaach.", this);
            return null;
        }

        return Instantiate(FlyingPrefab, hit.position, spawnPoint.rotation);
    }
}
