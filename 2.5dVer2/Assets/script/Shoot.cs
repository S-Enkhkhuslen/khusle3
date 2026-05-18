using UnityEngine;

public class Shoot : MonoBehaviour
{
    [SerializeField] GameObject BulletPrefab;
    private float bulletSpeed = 50f;

    void Start()
    {
        
    }
    void Update()
    {
        
    }
    void shooting()
    {
        Vector3 direction 
        GameObject bullet = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
    }
}
