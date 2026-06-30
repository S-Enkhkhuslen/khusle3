using Unity.VisualScripting;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{

    public Camera cam;
    public GameObject bulletPrefab;
    public float damage = 20f;
    public Transform gunPoint;
    [SerializeField] private TPSAim TPSAim;

    public float bulletSpeed = 100f;
    void Update()
    {
        if (TPSAim.IsAiming && Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100f);
        }

        Vector3 direction =
            (targetPoint - gunPoint.position).normalized;

        GameObject bullet =
            Instantiate(bulletPrefab,
                        gunPoint.position,
                        Quaternion.LookRotation(direction));

        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        rb.linearVelocity = direction * bulletSpeed;

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