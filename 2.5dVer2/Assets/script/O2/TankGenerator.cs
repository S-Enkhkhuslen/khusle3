using UnityEditor;
using UnityEngine;

public class TankGenerator : MonoBehaviour
{
    [SerializeField] GameObject Tank;
    public int tank=8;
    public float spawnRadius=30f;
    public float height=0f;

    void Start()
    {
        CreateTank(tank);
    }
    void CreateTank(int num)
    {
        for(int i=0; i<num; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle*spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(
                randomOffset.x,
                height,
                randomOffset.y);
             Instantiate(Tank,spawnPos, Quaternion.identity);

        }
    }
}
