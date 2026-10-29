using UnityEngine;
using UnityEngine.Rendering;

// Small, self-cleaning effects shared by player and enemy weapons.
public static class CombatEffects
{
    public static readonly Color Blue = new Color(0.12f, 0.8f, 1f);
    public static readonly Color Orange = new Color(1f, 0.38f, 0.06f);
    private static Material material;
    private static Texture2D texture;

    public static void Muzzle(Vector3 position, Vector3 direction, Color color)
    {
        Burst("Muzzle Flash", position, direction, color, 10, 0.2f, 3f, 0.12f, true);
    }

    public static void Impact(Vector3 position, Vector3 normal, Color color)
    {
        Burst("Impact Sparks", position, normal, color, 16, 0.4f, 3f, 0.11f, true);
    }

    public static void Spawn(Vector3 position)
    {
        Burst("Spawn Energy", position, Vector3.up, Blue, 28, 0.7f, 2.5f, 0.22f, false);
    }

    public static void Death(Vector3 position)
    {
        Burst("Enemy Death Sparks", position, Vector3.up, Orange, 36, 0.8f, 4f, 0.2f, false);
    }

    public static void AddTrail(GameObject projectile, Color color)
    {
        Material shared = GetMaterial();
        if (shared == null) return;
        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.sharedMaterial = shared;
        trail.time = 0.12f;
        trail.startWidth = 0.09f;
        trail.endWidth = 0.01f;
        trail.minVertexDistance = 0.03f;
        trail.colorGradient = Fade(color);
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
    }

    private static void Burst(string name, Vector3 position, Vector3 direction,
        Color color, int count, float lifetime, float speed, float size, bool cone)
    {
        Material shared = GetMaterial();
        if (shared == null) return;
        var root = new GameObject(name);
        root.transform.position = position;
        root.transform.rotation = Quaternion.LookRotation(
            direction.sqrMagnitude > 0.0001f ? direction : Vector3.up);
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = lifetime;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.5f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.4f, size);
        main.startColor = Color.white;
        main.gravityModifier = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.shapeType = cone ? ParticleSystemShapeType.Cone : ParticleSystemShapeType.Sphere;
        shape.angle = 35f;
        shape.radius = cone ? 0.025f : 0.35f;
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        fade.color = Fade(color);
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = shared;
        if (cone)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.5f;
            renderer.velocityScale = 0.04f;
        }
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
        particles.Emit(count);
        Object.Destroy(root, lifetime + 0.2f);
    }

    private static Gradient Fade(Color color)
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f),
            new GradientColorKey(color, 0.2f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }

    private static Material GetMaterial()
    {
        if (material != null) return material;
        // This built-in sprite shader is already included in GraphicsSettings.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null || !shader.isSupported) return null;
        texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.name = "Soft Energy Dot";
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float distance = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                float alpha = Mathf.Clamp01(1f - distance);
                pixels[y * 32 + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        material = new Material(shader) { name = "Combat Energy", mainTexture = texture };
        return material;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        if (material != null) Object.Destroy(material);
        if (texture != null) Object.Destroy(texture);
        material = null;
        texture = null;
    }
}
