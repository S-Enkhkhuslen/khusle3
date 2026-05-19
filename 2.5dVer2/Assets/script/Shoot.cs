using Unity.VisualScripting;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{

    public Camera cam;
    public GameObject bulletPrefab;
    public float damage = 20f;
    public Transform gunPoint;

    public float bulletSpeed = 40f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        GameObject bullet =
            Instantiate(bulletPrefab,
                        gunPoint.position,
                        gunPoint.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        rb.linearVelocity = gunPoint.forward * bulletSpeed;

    }
}



    /* public Camera cam;
     public float range = 100f;

     void Update()
     {
         if (Input.GetMouseButtonDown(0))
         {
             Shoot();
         }
     }

     void Shoot()
     {
         RaycastHit hit;

         if (Physics.Raycast(cam.transform.position,
                             cam.transform.forward,
                             out hit,
                             100f))
         {
             Debug.Log(hit.transform.name);
         }
         
     }*/