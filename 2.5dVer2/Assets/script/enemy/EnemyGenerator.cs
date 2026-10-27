using UnityEngine;

public class EnemyGenerator : MonoBehaviour
{
    [Header("Spawn Enemy's")]
    [SerializeField] public GameObject MeleePrefab;
    [SerializeField] public GameObject RangePrefab;
    [SerializeField] public GameObject FlyingPrefab;

    private Transform Location;
    TankGenerator tankGenerator;
    void Start()
    {
        if (Location == null) return;
        if (MeleePrefab == null) return;
        if(RangePrefab  == null) return;
        if(FlyingPrefab == null) return;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
