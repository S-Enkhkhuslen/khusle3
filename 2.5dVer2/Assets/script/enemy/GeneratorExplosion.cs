using UnityEngine;
using UnityEngine.Rendering;

// Visual explosion only; it does not damage the player or enemies.
public class GeneratorExplosion : MonoBehaviour
{
    private Light flash;
    private float age;

    public static void Play(Vector3 position)
    {
        var explosion = new GameObject("Generator Explosion").AddComponent<GeneratorExplosion>();
        explosion.transform.position = position;
        explosion.Emit("Orange Energy", CombatEffects.Orange, 32, 0.9f, 5f, 0.65f, 0.15f);
        explosion.Emit("Metal Sparks", new Color(1f, 0.75f, 0.3f), 64, 0.18f, 9f, 1f, 0.9f);
        explosion.Emit("Explosion Dust", new Color(0.45f, 0.3f, 0.2f, 0.6f), 28, 1.5f, 2.5f, 1.5f, -0.05f);
        explosion.flash = explosion.gameObject.AddComponent<Light>();
        explosion.flash.color = CombatEffects.Orange;
        explosion.flash.type = LightType.Point;
        explosion.flash.range = 12f;
        explosion.flash.intensity = 7f;
        explosion.flash.shadows = LightShadows.None;
        GameAudio.GeneratorExploded(position);
        Destroy(explosion.gameObject, 1.8f);
    }

    private void Emit(string name, Color color, int count, float size, float speed,
        float lifetime, float gravity)
    {
        var root = new GameObject(name);
        root.transform.SetParent(transform, false);
        var particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = lifetime;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;
        var sizeOverLife = particles.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.25f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = CombatEffects.ParticleMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
        particles.Emit(count);
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (flash != null) flash.intensity = 7f * Mathf.Clamp01(1f - age / 0.4f);
    }
}
