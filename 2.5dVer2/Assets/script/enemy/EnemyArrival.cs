using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

// Suspends the prefab's normal movement until it reaches its chosen NavMesh point.
[DefaultExecutionOrder(-50)]
public class EnemyArrival : MonoBehaviour
{
    private Vector3 landingPosition, startPosition;
    private float age, delay, duration;
    private bool visible;
    private Renderer[] renderers;
    private bool[] rendererStates;
    private readonly List<Behaviour> suspended = new List<Behaviour>();
    private readonly List<Collider> colliders = new List<Collider>();
    private Rigidbody body;
    private bool wasKinematic, usedGravity, hadCollisions;
    private TrailRenderer trail;
    private GeneratorSkySignal signal;

    public static void Drop(GameObject enemy, float height, float duration, float delay)
    {
        var arrival = enemy.AddComponent<EnemyArrival>();
        arrival.landingPosition = enemy.transform.position;
        arrival.startPosition = arrival.landingPosition + Vector3.up * Mathf.Max(1f, height);
        arrival.duration = Mathf.Max(0.1f, duration);
        arrival.delay = Mathf.Max(0f, delay);

        // Keep Enemy enabled so battle music can track the incoming wave.
        foreach (Behaviour component in enemy.GetComponentsInChildren<Behaviour>())
        {
            if (!component.enabled || component == arrival || component is Enemy) continue;
            arrival.suspended.Add(component);
            component.enabled = false;
        }
        foreach (Collider collider in enemy.GetComponentsInChildren<Collider>())
        {
            if (!collider.enabled) continue;
            arrival.colliders.Add(collider);
            collider.enabled = false;
        }
        arrival.body = enemy.GetComponent<Rigidbody>();
        if (arrival.body != null)
        {
            arrival.wasKinematic = arrival.body.isKinematic;
            arrival.usedGravity = arrival.body.useGravity;
            arrival.hadCollisions = arrival.body.detectCollisions;
            arrival.body.isKinematic = true;
            arrival.body.useGravity = false;
            arrival.body.detectCollisions = false;
        }
        arrival.renderers = enemy.GetComponentsInChildren<Renderer>();
        arrival.rendererStates = new bool[arrival.renderers.Length];
        for (int i = 0; i < arrival.renderers.Length; i++)
        {
            arrival.rendererStates[i] = arrival.renderers[i].enabled;
            arrival.renderers[i].enabled = false;
        }
        enemy.transform.position = arrival.startPosition;

        var exhaust = new GameObject("Arrival Trail");
        exhaust.transform.SetParent(enemy.transform, false);
        exhaust.transform.localPosition = Vector3.up;
        arrival.trail = exhaust.AddComponent<TrailRenderer>();
        arrival.trail.sharedMaterial = CombatEffects.ParticleMaterial;
        arrival.trail.time = 0.25f;
        arrival.trail.startWidth = 0.65f;
        arrival.trail.endWidth = 0f;
        arrival.trail.startColor = CombatEffects.Blue;
        arrival.trail.endColor = new Color(0.12f, 0.8f, 1f, 0f);
        arrival.trail.shadowCastingMode = ShadowCastingMode.Off;
        arrival.trail.receiveShadows = false;
        arrival.trail.emitting = false;
    }

    public void WaitForSignal(GeneratorSkySignal signal)
    {
        this.signal = signal;
    }

    private void Update()
    {
        if (signal != null) return;
        age += Time.deltaTime;
        if (age < delay) return;
        if (!visible)
        {
            visible = true;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = rendererStates[i];
            trail.Clear();
            trail.emitting = true;
        }
        float progress = Mathf.Clamp01((age - delay) / duration);
        transform.position = Vector3.Lerp(startPosition, landingPosition, progress * progress);
        if (progress < 1f) return;

        Physics.SyncTransforms();
        foreach (Collider collider in colliders)
            if (collider != null) collider.enabled = true;
        if (body != null)
        {
            body.isKinematic = wasKinematic;
            body.useGravity = usedGravity;
            body.detectCollisions = hadCollisions;
        }
        // Restore the agent first, before the AI starts using it again.
        foreach (Behaviour component in suspended)
            if (component is NavMeshAgent) component.enabled = true;
        foreach (Behaviour component in suspended)
            if (component != null && !(component is NavMeshAgent)) component.enabled = true;
        trail.emitting = false;
        Destroy(trail.gameObject, trail.time);
        CombatEffects.Spawn(landingPosition + Vector3.up);
        Destroy(this);
    }
}
