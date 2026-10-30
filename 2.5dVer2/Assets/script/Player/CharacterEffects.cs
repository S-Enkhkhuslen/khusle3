using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class CharacterEffects : MonoBehaviour
{
    [Header("Walking Dust")]
    [SerializeField, Min(0.1f)] private float stepDistance = 0.55f;
    [SerializeField, Min(1)] private int dustParticlesPerStep = 16;
    [SerializeField, Min(0.1f)] private float dustSize = 0.75f;
    [SerializeField, Min(0.1f)] private float dustLifetime = 1.2f;
    [SerializeField] private Color dustColor = new Color(0.65f, 0.38f, 0.22f);
    [SerializeField] private LayerMask groundMask = 1 << 3;
    [Header("Damage Flash")]
    [SerializeField, Min(0.1f)] private float flashDuration = 0.45f;
    [SerializeField] private Color damageColor = new Color(1f, 0.07f, 0.04f);

    private CharacterController body;
    private Jetpack jetpack;
    private Sprint sprint;
    private Oxygen oxygen;
    private PlayerHealth health;
    private Enemy enemy;
    private NavMeshAgent agent;
    private bool flyingEnemy;
    private GameObject effectRoot;
    private ParticleSystem dust;
    private readonly ParticleSystem[] jets = new ParticleSystem[2];
    private readonly Renderer[] nozzles = new Renderer[2];
    private Vector3 previousPosition;
    private float distanceSinceStep;
    private bool leftStep;
    private float flashUntil;
    private float flashStarted;
    private bool tinted;
    private readonly List<TintTarget> tintTargets = new List<TintTarget>();

    private class TintTarget
    {
        public Renderer renderer;
        public int index;
        public MaterialPropertyBlock original;
        public MaterialPropertyBlock red;
    }

    private void Awake()
    {
        body = GetComponent<CharacterController>();
        jetpack = GetComponent<Jetpack>();
        sprint = GetComponent<Sprint>();
        oxygen = GetComponent<Oxygen>();
        health = GetComponent<PlayerHealth>();
        enemy = GetComponent<Enemy>();
        agent = GetComponent<NavMeshAgent>();
        flyingEnemy = GetComponent<FlyingEnemyAI>() != null;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.name == "L_ThrusterGlow") nozzles[0] = renderer;
            if (renderer.name == "R_ThrusterGlow") nozzles[1] = renderer;
            if (flyingEnemy && renderer.name == "Thruster_light") nozzles[0] = renderer;
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null) continue;
                string property = material.HasProperty("_BaseColor") ? "_BaseColor"
                    : material.HasProperty("_Color") ? "_Color" : null;
                if (property == null) continue;
                var original = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(original, i);
                var red = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(red, i);
                if (red.isEmpty) renderer.GetPropertyBlock(red);
                Color color = damageColor;
                color.a = material.GetColor(property).a;
                red.SetColor(property, color);
                if (material.HasProperty("_EmissionColor")) red.SetColor("_EmissionColor", color * 0.5f);
                tintTargets.Add(new TintTarget { renderer = renderer, index = i, original = original, red = red });
            }
        }
    }

    private void Start()
    {
        effectRoot = new GameObject("Character Movement Effects");
        effectRoot.transform.SetParent(transform, false);
        if (!flyingEnemy) dust = CreateParticles("Footstep Dust", dustLifetime, dustSize, dustColor);
        if (dust != null)
        {
            var dustMain = dust.main;
            dustMain.startSpeed = 0f;
            dustMain.maxParticles = 240;
            var dustSize = dust.sizeOverLifetime;
            dustSize.enabled = true;
            dustSize.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 2f)));
            var dustFade = dust.colorOverLifetime;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.55f, 0.4f), new GradientAlphaKey(0f, 1f) });
            dustFade.color = gradient;
        }
        int jetCount = jetpack != null ? 2 : 0;
        if (flyingEnemy) jetCount = nozzles[0] != null ? 1 : 2;
        for (int i = 0; i < jetCount; i++)
        {
            jets[i] = CreateParticles(i == 0 ? "Left Jet Exhaust" : "Right Jet Exhaust",
                0.2f, 0.18f, flyingEnemy ? CombatEffects.Orange : CombatEffects.Blue);
            var shape = jets[i].shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 9f;
            shape.radius = 0.045f;
            var size = jets[i].sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var renderer = jets[i].GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.4f;
            renderer.velocityScale = 0.025f;
        }
        previousPosition = transform.position;
    }

    private void LateUpdate()
    {
        bool alive = (health == null || !health.IsDead) && (enemy == null || !enemy.IsDead);
        Vector3 displacement = transform.position - previousPosition;
        previousPosition = transform.position;
        displacement.y = 0f;
        bool jetting = alive && (flyingEnemy || (jetpack != null && jetpack.IsJetting));
        bool sprinting = alive && sprint != null && sprint.IsSprinting;
        UpdateDust(displacement, alive && !jetting);
        UpdateJets(jetting, sprinting);
        bool red = Time.time < flashUntil && Mathf.Repeat(Time.time - flashStarted, 0.15f) < 0.09f;
        SetTint(red);
    }

    private void UpdateDust(Vector3 displacement, bool active)
    {
        float distance = displacement.magnitude;
        bool grounded = body != null ? body.isGrounded
            : agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
        if (dust == null || !active || !grounded
            || distance < 0.001f || distance > 2f)
        {
            distanceSinceStep = 0f;
            return;
        }
        distanceSinceStep += distance;
        if (distanceSinceStep < Mathf.Max(0.1f, stepDistance)) return;
        distanceSinceStep %= Mathf.Max(0.1f, stepDistance);
        leftStep = !leftStep;
        Vector3 feet = (body != null
            ? transform.TransformPoint(body.center - Vector3.up * body.height * 0.5f) : transform.position)
            + transform.right * (leftStep ? -0.16f : 0.16f);
        if (!Physics.Raycast(feet + Vector3.up * 0.4f, Vector3.down, out RaycastHit hit,
            0.8f, groundMask, QueryTriggerInteraction.Ignore)) return;
        if (!dust.isPlaying) dust.Play();
        for (int i = 0; i < Mathf.Max(1, dustParticlesPerStep); i++)
        {
            var particle = new ParticleSystem.EmitParams {
                position = hit.point + hit.normal * 0.06f + transform.right * Random.Range(-0.18f, 0.18f),
                velocity = hit.normal * Random.Range(0.3f, 0.7f)
                    - displacement.normalized * 0.65f + Random.insideUnitSphere * 0.35f };
            dust.Emit(particle, 1);
        }
    }

    private void UpdateJets(bool jetting, bool sprinting)
    {
        bool active = (jetting || sprinting) && (oxygen == null || oxygen.HasOxygen);
        Vector3 direction = sprinting ? (-transform.forward - transform.up * 0.35f).normalized : -transform.up;
        if (jetting && sprinting) direction = (-transform.forward * 0.65f - transform.up).normalized;
        for (int i = 0; i < jets.Length; i++)
        {
            ParticleSystem jet = jets[i];
            if (jet == null) continue;
            jet.transform.position = nozzles[i] != null ? nozzles[i].bounds.center
                : transform.TransformPoint(new Vector3(i == 0 ? -0.22f : 0.22f, 0.55f, -0.3f));
            jet.transform.rotation = Quaternion.LookRotation(direction);
            if (flyingEnemy && nozzles[i] != null)
                jet.transform.position += direction * nozzles[i].bounds.extents.magnitude * 0.65f;
            var main = jet.main;
            main.startSpeed = sprinting ? 7f : 5f;
            var emission = jet.emission;
            emission.enabled = active;
            emission.rateOverTime = sprinting ? 100f : 75f;
            if (active && !jet.isPlaying) jet.Play();
            if (!active && jet.isPlaying) jet.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void FlashDamage()
    {
        if (!isActiveAndEnabled) return;
        flashStarted = Time.time;
        flashUntil = Time.time + Mathf.Max(0.1f, flashDuration);
        SetTint(true);
    }

    private void SetTint(bool red)
    {
        if (tinted == red) return;
        tinted = red;
        foreach (TintTarget target in tintTargets)
            if (target.renderer != null)
                target.renderer.SetPropertyBlock(red ? target.red
                    : target.original.isEmpty ? null : target.original, target.index);
    }

    private ParticleSystem CreateParticles(string name, float lifetime, float size, Color color)
    {
        var root = new GameObject(name);
        root.transform.SetParent(effectRoot.transform, false);
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = CombatEffects.ParticleMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
    }

    private void OnDisable()
    {
        flashUntil = 0f;
        distanceSinceStep = 0f;
        SetTint(false);
        if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (ParticleSystem jet in jets)
            if (jet != null) jet.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDestroy()
    {
        if (effectRoot != null) Destroy(effectRoot);
    }
}
