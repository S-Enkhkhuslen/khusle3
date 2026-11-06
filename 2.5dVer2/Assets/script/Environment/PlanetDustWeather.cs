using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Local wind and distance haze; independent of the player while the rocket launches.
[DisallowMultipleComponent]
public sealed class PlanetDustWeather : MonoBehaviour
{
    [Header("Light Sandstorm")]
    [Range(0f, 2f)] public float dustAmount = 1.2f;
    [Min(0f)] public float windSpeed = 7f;
    public Vector3 windDirection = new Vector3(1f, 0f, 0.35f);
    [Range(0f, 0.02f)] public float hazeDensity = 0.006f;
    public Color hazeColor = new Color(0.59f, 0.43f, 0.31f);

    private static PlanetDustWeather instance;
    private readonly System.Random random = new System.Random(7183);
    private Transform player;
    private Camera view;
    private UniversalAdditionalCameraData depthCamera;
    private CameraOverrideOption oldDepth;
    private ParticleSystem groundDust, sand;
    private float groundBudget, sandBudget;
    private bool oldFog, capturedFog;
    private FogMode oldMode;
    private Color oldColor;
    private float oldDensity, oldStart, oldEnd;

    public static void Follow(Transform target)
    {
        if (instance == null)
            instance = new GameObject("Planet Dust Weather").AddComponent<PlanetDustWeather>();
        instance.player = target;
    }

    // Keep the local wind field around the astronaut across the map seam.
    public static void OnPlayerWarped(Transform target, Vector3 delta)
    {
        if (instance == null || instance.player != target) return;
        ShiftParticles(instance.groundDust, delta);
        ShiftParticles(instance.sand, delta);
    }

    private static void ShiftParticles(ParticleSystem system, Vector3 delta)
    {
        if (system == null || system.particleCount == 0) return;
        var particles = new ParticleSystem.Particle[system.particleCount];
        int count = system.GetParticles(particles);
        for (int i = 0; i < count; i++) particles[i].position += delta;
        system.SetParticles(particles, count);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        if (instance == null) instance = this;
        var groundMaterial = Resources.Load<Material>("Weather/WindDust");
        var sandMaterial = Resources.Load<Material>("Weather/SandGrain");
        if (groundMaterial == null || sandMaterial == null)
        {
            Debug.LogWarning("PlanetDustWeather: Weather materials are missing.", this);
            enabled = false;
            return;
        }
        groundDust = CreateParticles("Windblown Ground Dust", groundMaterial, 100, 6f, 9f,
            new Color(0.78f, 0.56f, 0.36f, 0.48f));
        var main = groundDust.main;
        main.startSize = new ParticleSystem.MinMaxCurve(1f, 1.8f);
        var groundRenderer = groundDust.GetComponent<ParticleSystemRenderer>();
        groundRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        groundRenderer.lengthScale = 6f;
        groundRenderer.velocityScale = 0.02f;
        groundRenderer.maxParticleSize = 0.24f;

        sand = CreateParticles("Airborne Sand", sandMaterial, 310, 2.5f, 4f,
            new Color(0.86f, 0.68f, 0.45f, 0.42f));
        main = sand.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.022f, 0.055f);
        var sandRenderer = sand.GetComponent<ParticleSystemRenderer>();
        sandRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        sandRenderer.lengthScale = 1.3f;
        sandRenderer.velocityScale = 0.008f;
        sandRenderer.maxParticleSize = 0.022f;
    }

    private void OnEnable()
    {
        oldFog = RenderSettings.fog; oldMode = RenderSettings.fogMode;
        oldColor = RenderSettings.fogColor; oldDensity = RenderSettings.fogDensity;
        oldStart = RenderSettings.fogStartDistance; oldEnd = RenderSettings.fogEndDistance;
        capturedFog = true;
        ApplyHaze();
        if (groundDust != null) groundDust.Play();
        if (sand != null) sand.Play();
    }

    private void Start()
    {
        FindView();
        for (int i = 0; i < 55; i++) EmitGround();
        for (int i = 0; i < 155; i++) EmitSand();
        if (groundDust != null) { groundDust.Simulate(2f, false, false); groundDust.Play(); }
        if (sand != null) { sand.Simulate(1f, false, false); sand.Play(); }
    }

    private void LateUpdate()
    {
        FindView();
        ApplyHaze();
        if (Time.deltaTime <= 0f || groundDust == null) return;
        float gust = 0.8f + 0.2f * Mathf.Sin(Time.time * 0.45f);
        groundBudget = Mathf.Min(groundBudget + Time.deltaTime * 7f * dustAmount * gust, 10f);
        sandBudget = Mathf.Min(sandBudget + Time.deltaTime * 48f * dustAmount * gust, 60f);
        while (groundBudget >= 1f) { EmitGround(); groundBudget--; }
        while (sandBudget >= 1f) { EmitSand(); sandBudget--; }
    }

    private void FindView()
    {
        if (view == null || !view.isActiveAndEnabled) view = StatusBarUI.FindGameCamera();
        var data = view != null ? view.GetComponent<UniversalAdditionalCameraData>() : null;
        if (data == depthCamera) return;
        RestoreDepth();
        depthCamera = data;
        if (depthCamera != null)
        {
            oldDepth = depthCamera.requiresDepthOption;
            depthCamera.requiresDepthTexture = true;
        }
    }

    private Vector3 Center => player != null && player.gameObject.activeInHierarchy ? player.position
        : view != null ? view.transform.position : transform.position;
    private float Between(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
    private Vector3 Wind => new Vector3(windDirection.x, 0f, windDirection.z).normalized * windSpeed;

    private void EmitGround()
    {
        if (groundDust == null || dustAmount <= 0f) return;
        float angle = Between(0f, Mathf.PI * 2f), radius = Between(10f, 38f);
        Vector3 point = Center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        if (!Physics.Raycast(point + Vector3.up * 60f, Vector3.down, out RaycastHit hit,
            180f, 1 << 3, QueryTriggerInteraction.Ignore)) return;
        groundDust.Emit(new ParticleSystem.EmitParams {
            position = hit.point + Vector3.up * Between(0.7f, 1.3f),
            velocity = Wind * Between(0.3f, 0.5f)
        }, 1);
    }

    private void EmitSand()
    {
        if (sand == null || dustAmount <= 0f) return;
        sand.Emit(new ParticleSystem.EmitParams {
            position = Center + new Vector3(Between(-22f, 22f), Between(0.4f, 9f), Between(-22f, 22f)),
            velocity = Wind * Between(0.9f, 1.2f) + Vector3.down * Between(0.1f, 0.25f)
        }, 1);
    }

    private void ApplyHaze()
    {
        RenderSettings.fog = hazeDensity > 0f;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = hazeColor;
        RenderSettings.fogDensity = hazeDensity;
    }

    private ParticleSystem CreateParticles(string name, Material material, int limit,
        float minLife, float maxLife, Color color)
    {
        var root = new GameObject(name);
        root.transform.SetParent(transform, false);
        var particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false; main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.maxParticles = limit; main.startSpeed = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startColor = color;
        var emission = particles.emission; emission.enabled = false;
        var shape = particles.shape; shape.enabled = false;
        var fade = particles.colorOverLifetime; fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.minParticleSize = 0f;
        particles.useAutoRandomSeed = false;
        particles.randomSeed = name == "Airborne Sand" ? 18u : 17u;
        return particles;
    }

    private void RestoreDepth()
    {
        if (depthCamera != null) depthCamera.requiresDepthOption = oldDepth;
        depthCamera = null;
    }

    private void OnDisable()
    {
        if (groundDust != null) groundDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sand != null) sand.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        RestoreDepth();
        if (!capturedFog) return;
        RenderSettings.fog = oldFog; RenderSettings.fogMode = oldMode;
        RenderSettings.fogColor = oldColor; RenderSettings.fogDensity = oldDensity;
        RenderSettings.fogStartDistance = oldStart; RenderSettings.fogEndDistance = oldEnd;
        capturedFog = false;
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
