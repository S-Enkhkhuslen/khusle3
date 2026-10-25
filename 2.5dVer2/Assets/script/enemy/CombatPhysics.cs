using UnityEngine;

public static class CombatPhysics
{
    public static bool CanMeleeHit(Transform attacker, Transform target,
        float horizontalRange, float verticalRange)
    {
        if (target == null) return false;
        PlayerHealth health = target.GetComponentInParent<PlayerHealth>();
        if (health == null || health.IsDead) return false;
        Vector3 difference = target.position - attacker.position;
        if (Mathf.Abs(difference.y) > verticalRange) return false;
        difference.y = 0f;
        if (difference.sqrMagnitude > horizontalRange * horizontalRange) return false;

        Vector3 origin = attacker.position + Vector3.up;
        CharacterController controller = target.GetComponent<CharacterController>();
        Vector3 targetPoint = controller != null ? controller.bounds.center : target.position + Vector3.up;
        Vector3 direction = targetPoint - origin;
        float distance = direction.magnitude;
        if (distance < 0.001f) return true;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction / distance, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(attacker)
                || hit.collider.GetComponentInParent<SweptProjectile>() != null)
                continue;
            return hit.collider.GetComponentInParent<PlayerHealth>() == health;
        }
        return true;
    }
}
