using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] private LayerMask shootMask = ~0;
    public Camera cam;
    public GameObject bulletPrefab;
    public float damage = 20f;
    public Transform gunPoint;
    [SerializeField] private TPSAim TPSAim;
    public float bulletSpeed = 100f;
    private PlayerAnimationController playerAnimation;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerAnimation = GetComponent<PlayerAnimationController>();
        playerHealth = GetComponent<PlayerHealth>();
        if (TPSAim == null) TPSAim = GetComponent<TPSAim>();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!context.performed || (playerHealth != null && playerHealth.IsDead)
            || TPSAim == null || !TPSAim.IsAiming)
            return;
        Shoot();
    }

    private void Shoot()
    {
        if (cam == null || gunPoint == null || bulletPrefab == null
            || (playerHealth != null && playerHealth.IsDead))
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 target = ray.GetPoint(100f);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, shootMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)
                || hit.collider.GetComponentInParent<SweptProjectile>() != null)
                continue;
            if (hit.collider.isTrigger && hit.collider.GetComponentInParent<Enemy>() == null)
                continue;
            target = hit.point;
            break;
        }

        Vector3 direction = (target - gunPoint.position).normalized;
        if (direction.sqrMagnitude < 0.001f) return;
        GameObject projectile = Instantiate(bulletPrefab, gunPoint.position, Quaternion.LookRotation(direction));
        Bullet bullet = projectile.GetComponent<Bullet>();
        if (bullet == null)
        {
            Debug.LogError("The player bullet prefab needs a Bullet component.", this);
            Destroy(projectile);
            return;
        }
        bullet.Launch(direction, bulletSpeed, damage, transform);
        playerAnimation?.PlayShootAnimation();
    }
}
