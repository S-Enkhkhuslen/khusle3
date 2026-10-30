using UnityEngine;

public class Bullet : SweptProjectile
{
    public float damage = 20f;
    public void Launch(Vector3 direction, float speed, float damageAmount, Transform owner)
    {
        damage = Mathf.Max(0f, damageAmount);
        Initialize(direction, speed, owner);
        CombatEffects.AddBlueGlow(gameObject);
    }

    protected override bool IgnoreTarget(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null) return true;
        return other.isTrigger && other.GetComponentInParent<Enemy>() == null;
    }

    protected override void OnImpact(Collider other)
    {
        other.GetComponentInParent<Enemy>()?.TakeDamage(damage);
    }
}
