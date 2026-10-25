using UnityEngine;

public class EnemyProjectile : SweptProjectile
{
    private float damage;
    public void Launch(Vector3 direction, float speed, float damageAmount, Transform owner = null)
    {
        damage = Mathf.Max(0f, damageAmount);
        Initialize(direction, speed, owner);
    }

    protected override bool IgnoreTarget(Collider other)
    {
        if (other.GetComponentInParent<Enemy>() != null) return true;
        return other.isTrigger && other.GetComponentInParent<PlayerHealth>() == null;
    }

    protected override void OnImpact(Collider other)
    {
        other.GetComponentInParent<PlayerHealth>()?.TakeDamage(damage);
    }
}
