using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] private LayerMask shootMask;
    public Camera cam;
    public GameObject bulletPrefab;
    public float damage = 20f;
    public Transform gunPoint;
    [SerializeField] private TPSAim TPSAim;

    public float bulletSpeed = 100f;
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }
        if(TPSAim == null)
        {
            return;
        }
        if (!TPSAim.IsAiming)
        {
            return;
        }
        Shoot();
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