using System.Collections.Generic;
using UnityEngine;

// Remembers the model's colours, flashes red on damage, then restores them.
public sealed class DamageFlash
{
    private class Target
    {
        public Renderer renderer;
        public int index;
        public MaterialPropertyBlock original, red;
    }
    private readonly List<Target> targets = new List<Target>();
    private float started, until;
    private bool tinted;

    public DamageFlash(Renderer[] renderers, Color damageColor)
    {
        foreach (Renderer renderer in renderers)
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null) continue;
                string property = material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
                if (property == null) continue;
                var original = new MaterialPropertyBlock(); renderer.GetPropertyBlock(original, i);
                var red = new MaterialPropertyBlock(); renderer.GetPropertyBlock(red, i);
                if (red.isEmpty) renderer.GetPropertyBlock(red);
                Color color = damageColor; color.a = material.GetColor(property).a;
                red.SetColor(property, color);
                if (material.HasProperty("_EmissionColor")) red.SetColor("_EmissionColor", color * 0.5f);
                targets.Add(new Target { renderer = renderer, index = i, original = original, red = red });
            }
        }
    }
    public void Trigger(float duration)
    {
        started = Time.time; until = started + Mathf.Max(0.1f, duration); SetTint(true);
    }
    public void Update() { SetTint(Time.time < until && Mathf.Repeat(Time.time - started, 0.15f) < 0.09f); }
    public void Clear() { until = 0f; SetTint(false); }
    private void SetTint(bool red)
    {
        if (tinted == red) return;
        tinted = red;
        foreach (Target target in targets)
            if (target.renderer != null)
                target.renderer.SetPropertyBlock(red ? target.red : target.original.isEmpty ? null : target.original, target.index);
    }
}
