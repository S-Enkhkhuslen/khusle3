using UnityEngine;
using UnityEngine.Rendering;

// Scene-local weather keeps running when the astronaut boards the escape rocket.
[DisallowMultipleComponent]
public sealed class PlanetDustWeather : MonoBehaviour
{
    [Header("Light Sandstorm")]
    [Range(0f, 2f)] public float dustAmount = 1f;
    [Min(0f)] public float windSpeed = 7f;
    public Vector3 windDirection = new Vector3(1f, 0f, 0.35f);
    [Range(0f, 0.02f)] public float hazeDensity = 0.008f;
    public Color hazeColor = new Color(0.62f, 0.43f, 0.29f);

    private static PlanetDustWeather instance;
    private readonly System.Random random = new System.Random(7183);
    private Transform player;
    private Camera view;
    private ParticleSystem groundDust, sand;
    private Material material;
    private Texture2D texture;
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        material = CreateMaterial();
        if (material == null) { enabled = false; return; }
        groundDust = CreateParticles("Windblown Ground Dust", 110, 6f, 10f, 6f, 9f,
            new Color(0.64f, 0.41f, 0.24f, 0.22f));
        sand = CreateParticles("Airborne Sand", 400, 0.045f, 0.11f, 3f, 5f,
            new Color(0.8f, 0.61f, 0.39f, 0.55f));
        var renderer = sand.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 1.2f;
        renderer.velocityScale = 0.025f;
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
        // Start with a settled weather field instead of waiting for particles to fill it.
        for (int i = 0; i < 65; i++) EmitGround();
        for (int i = 0; i < 220; i++) EmitSand();
        if (groundDust != null) groundDust.Simulate(2f, false, false);
        if (sand != null) sand.Simulate(1.5f, false, false);
        if (groundDust != null) groundDust.Play();
        if (sand != null) sand.Play();
    }

    private void LateUpdate()
    {
        ApplyHaze();
        if (Time.deltaTime <= 0f || material == null) return;
        FindView();
        // A slow variation reads as gentle gusts; the random stream never affects gameplay.
        float gust = 0.8f + 0.2f * Mathf.Sin(Time.time * 0.45f);
        groundBudget = Mathf.Min(groundBudget + Time.deltaTime * 10f * dustAmount * gust, 12f);
        sandBudget = Mathf.Min(sandBudget + Time.deltaTime * 75f * dustAmount * gust, 80f);
        while (groundBudget >= 1f) { EmitGround(); groundBudget--; }
        while (sandBudget >= 1f) { EmitSand(); sandBudget--; }
    }

    private void FindView()
    {
        if (view == null || !view.isActiveAndEnabled) view = StatusBarUI.FindGameCamera();
    }

    private Vector3 Center => view != null ? view.transform.position
        : player != null ? player.position : transform.position;
    private float Between(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
    private Vector3 Wind => new Vector3(windDirection.x, 0f, windDirection.z).normalized * windSpeed;

    private void EmitGround()
    {
        if (groundDust == null || dustAmount <= 0f) return;
        Vector3 center = player != null && player.gameObject.activeInHierarchy ? player.position : Center;
        float angle = Between(0f, Mathf.PI * 2f), radius = Between(6f, 38f);
        Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        // Each puff starts on the actual terrain, including slopes and crater floors.
        if (!Physics.Raycast(point + Vector3.up * 60f, Vector3.down, out RaycastHit hit,
            180f, 1 << 3, QueryTriggerInteraction.Ignore)) return;
        groundDust.Emit(new ParticleSystem.EmitParams {
            position = hit.point + Vector3.up * Between(0.4f, 1.6f),
            velocity = Wind * Between(0.65f, 1.1f) + Vector3.up * 0.06f,
            rotation = Between(0f, 360f)
        }, 1);
    }

    private void EmitSand()
    {
        if (sand == null || dustAmount <= 0f) return;
        sand.Emit(new ParticleSystem.EmitParams {
            position = Center + new Vector3(Between(-24f, 24f), Between(-6f, 10f), Between(-24f, 24f)),
            velocity = Wind * Between(0.9f, 1.4f) + Vector3.down * Between(0.1f, 0.35f)
        }, 1);
    }

    private void ApplyHaze()
    {
        RenderSettings.fog = hazeDensity > 0f;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = hazeColor;
        RenderSettings.fogDensity = hazeDensity;
    }

    private ParticleSystem CreateParticles(string name, int limit, float minSize, float maxSize,
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
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
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
        var size = particles.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.3f));
        var renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.minParticleSize = 0f; renderer.maxParticleSize = 0.35f;
        particles.useAutoRandomSeed = false;
        particles.randomSeed = name == "Airborne Sand" ? 18u : 17u;
        return particles;
    }

    private Material CreateMaterial()
    {
        // Sprites/Default is already retained by the project's GraphicsSettings.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null || !shader.isSupported) return null;
        const int side = 64;
        texture = new Texture2D(side, side, TextureFormat.RGBA32, false) {
            name = "Soft Wind Dust", wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color[side * side];
        for (int y = 0; y < side; y++) for (int x = 0; x < side; x++) {
            float u = (x + 0.5f) / side, v = (y + 0.5f) / side;
            float edge = Mathf.Clamp01(1f - new Vector2((u - 0.5f) * 2f, (v - 0.5f) * 2f).magnitude);
            float noise = Mathf.PerlinNoise(u * 5.5f + 17f, v * 5.5f + 8f);
            pixels[y * side + x] = new Color(1f, 1f, 1f, edge * edge * (0.55f + noise * 0.45f));
        }
        texture.SetPixels(pixels); texture.Apply(false, true);
        return new Material(shader) { name = "Planet Wind Dust", mainTexture = texture };
    }

    private void OnDisable()
    {
        if (groundDust != null) groundDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sand != null) sand.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (!capturedFog) return;
        RenderSettings.fog = oldFog; RenderSettings.fogMode = oldMode;
        RenderSettings.fogColor = oldColor; RenderSettings.fogDensity = oldDensity;
        RenderSettings.fogStartDistance = oldStart; RenderSettings.fogEndDistance = oldEnd;
        capturedFog = false;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (texture != null) Destroy(texture);
        if (instance == this) instance = null;
    }
}
