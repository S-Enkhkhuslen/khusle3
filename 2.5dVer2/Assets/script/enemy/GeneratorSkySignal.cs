using UnityEngine;
using UnityEngine.Rendering;

public class GeneratorSkySignal : MonoBehaviour
{
    public event System.Action Completed;
    private LineRenderer halo, core;
    private Light glow;
    private Vector3 origin;
    private float age, duration, height;

    public static GeneratorSkySignal Play(Vector3 position, float height, float duration)
    {
        var signal = new GameObject("Generator Sky Signal").AddComponent<GeneratorSkySignal>();
        signal.origin = position + Vector3.up * 0.8f;
        signal.height = Mathf.Max(1f, height);
        signal.duration = Mathf.Max(0.1f, duration);
        signal.halo = signal.CreateBeam("Cyan Halo", 0.9f, CombatEffects.Blue);
        signal.core = signal.CreateBeam("Laser Core", 0.18f, Color.white);
        signal.glow = signal.gameObject.AddComponent<Light>();
        signal.glow.type = LightType.Point;
        signal.glow.color = CombatEffects.Blue;
        signal.glow.range = 8f;
        signal.glow.shadows = LightShadows.None;
        signal.transform.position = signal.origin;
        CombatEffects.Muzzle(signal.origin, Vector3.up, CombatEffects.Blue);
        return signal;
    }

    private LineRenderer CreateBeam(string name, float width, Color color)
    {
        var root = new GameObject(name);
        root.transform.SetParent(transform, false);
        var line = root.AddComponent<LineRenderer>();
        line.sharedMaterial = CombatEffects.ParticleMaterial;
        line.positionCount = 2;
        line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.6f);
        line.widthMultiplier = width;
        line.startColor = color;
        line.endColor = new Color(color.r, color.g, color.b, 0.4f);
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.SetPosition(0, origin);
        line.SetPosition(1, origin);
        return line;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float progress = Mathf.Clamp01(age / duration);
        Vector3 top = origin + Vector3.up * height * Mathf.Clamp01(age / 0.2f);
        halo.SetPosition(1, top);
        core.SetPosition(1, top);
        float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 1f, progress));
        halo.widthMultiplier = 0.9f * fade;
        core.widthMultiplier = 0.18f * fade;
        glow.intensity = 5f * fade;
        if (progress < 1f) return;
        Completed?.Invoke();
        Destroy(gameObject);
    }
}
