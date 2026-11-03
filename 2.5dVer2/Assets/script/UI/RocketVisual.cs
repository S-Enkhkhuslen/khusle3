using System.Collections.Generic;
using UnityEngine;

// Small built-in model: no imported assets or Inspector setup required.
public class RocketVisual : MonoBehaviour
{
    private readonly List<Material> materials = new List<Material>();
    private Mesh noseMesh;

    private void Awake()
    {
        Material hull = MakeMaterial(new Color(0.82f, 0.88f, 0.92f));
        Material dark = MakeMaterial(new Color(0.08f, 0.12f, 0.18f));
        Material orange = MakeMaterial(new Color(1f, 0.3f, 0.06f));
        Material blue = MakeMaterial(new Color(0.05f, 0.8f, 1f), true);
        Part("Landing Pad", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(7f, 0.08f, 7f), dark);
        Part("Hull", PrimitiveType.Cylinder, new Vector3(0f, 3.8f, 0f), new Vector3(2.8f, 2.25f, 2.8f), hull);
        Part("Lower Band", PrimitiveType.Cylinder, new Vector3(0f, 2f, 0f), new Vector3(2.85f, 0.18f, 2.85f), orange);
        Part("Upper Band", PrimitiveType.Cylinder, new Vector3(0f, 5.7f, 0f), new Vector3(2.85f, 0.12f, 2.85f), dark);
        Part("Engine", PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 0f), new Vector3(1.7f, 0.45f, 1.7f), dark);
        Part("Engine Glow", PrimitiveType.Cylinder, new Vector3(0f, 0.73f, 0f), new Vector3(1.3f, 0.04f, 1.3f), blue);
        for (int i = 0; i < 4; i++)
        {
            Quaternion turn = Quaternion.Euler(0f, i * 90f, 0f);
            Transform fin = Part("Landing Fin", PrimitiveType.Cube, turn * new Vector3(1.5f, 1.6f, 0f), new Vector3(0.35f, 3f, 1.3f), orange);
            fin.localRotation = turn * Quaternion.Euler(0f, 0f, -25f);
        }
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            Part("Blue Window", PrimitiveType.Sphere, new Vector3(Mathf.Sin(angle) * 1.36f, 4.5f, Mathf.Cos(angle) * 1.36f), Vector3.one * 0.65f, blue);
        }
        MakeNose(hull);

        var ring = new GameObject("Boarding Ring").AddComponent<LineRenderer>();
        ring.transform.SetParent(transform, false);
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 64;
        ring.widthMultiplier = 0.12f;
        ring.sharedMaterial = blue;
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 3f, 0.25f, Mathf.Sin(angle) * 3f));
        }
    }

    private Material MakeMaterial(Color color, bool glow = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (glow && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2f);
        }
        materials.Add(material);
        return material;
    }

    private Transform Part(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        // The boarding area stays clear; visual parts never trap the astronaut.
        Collider collider = part.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        return part.transform;
    }

    private void MakeNose(Material material)
    {
        const int sides = 24;
        var vertices = new Vector3[sides + 2];
        var triangles = new int[sides * 6];
        vertices[0] = new Vector3(0f, 8.55f, 0f);
        vertices[sides + 1] = new Vector3(0f, 6.05f, 0f);
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 1.4f, 6.05f, Mathf.Sin(angle) * 1.4f);
            int next = (i + 1) % sides + 1;
            int start = i * 6;
            triangles[start] = 0; triangles[start + 1] = next; triangles[start + 2] = i + 1;
            triangles[start + 3] = sides + 1; triangles[start + 4] = i + 1; triangles[start + 5] = next;
        }
        noseMesh = new Mesh { name = "Rocket Nose", vertices = vertices, triangles = triangles };
        noseMesh.RecalculateNormals();
        var nose = new GameObject("Nose", typeof(MeshFilter), typeof(MeshRenderer));
        nose.transform.SetParent(transform, false);
        nose.GetComponent<MeshFilter>().sharedMesh = noseMesh;
        nose.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    public ParticleSystem StartExhaust()
    {
        transform.Find("Landing Pad").SetParent(null, true);
        transform.Find("Boarding Ring").gameObject.SetActive(false);
        var root = new GameObject("Rocket Exhaust");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.up * 0.65f;
        root.transform.localRotation = Quaternion.LookRotation(Vector3.down);
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.useUnscaledTime = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 12f;
        main.startSize = 0.9f;
        main.startColor = new Color(0.1f, 0.7f, 1f, 0.9f);
        main.maxParticles = 160;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particles.emission;
        emission.rateOverTime = 120f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.5f;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = CombatEffects.ParticleMaterial;
        particles.Play();
        return particles;
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) if (material != null) Destroy(material);
        if (noseMesh != null) Destroy(noseMesh);
    }
}
