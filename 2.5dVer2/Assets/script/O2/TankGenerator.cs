using UnityEditor;
using UnityEngine;

public class TankGenerator : MonoBehaviour
{
    [SerializeField] GameObject Tank;
    public float minX = 350;
    public float maxX = 400;
    public float minY = 350;
    public float maxY = 400;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateTank(8);
    }
    void CreateTank(int num)
    {
        for(int i=0; i<num; i++)
        {
        float randomX = Random.Range(minX, maxX);
        float randomY= Random.Range(minY, maxY);
        Vector3 spawnPos=new Vector3(randomX, 52,randomY);
            GameObject TankClone = Instantiate(Tank,spawnPos, Quaternion.identity);

        }
    }
}
