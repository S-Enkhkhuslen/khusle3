using UnityEngine;
using UnityEngine.Rendering;

// Only draws the generator's ring and prompt. Spawning stays in EnemyGenerator.
public sealed class GeneratorDisplay
{
    private LineRenderer ring;
    private Material material;
    private Vector3 previousCenter;
    private float previousRadius = -1f;
    private WorldPromptUI prompt;

    public GeneratorDisplay(Transform parent)
    {
        prompt = Object.Instantiate(Resources.Load<GameObject>("UI/GeneratorPrompt"), parent).GetComponent<WorldPromptUI>();
        prompt.gameObject.SetActive(false);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return;
        var root = new GameObject("Generator Interaction Range"); root.transform.SetParent(parent, false);
        ring = root.AddComponent<LineRenderer>(); ring.useWorldSpace = true; ring.loop = true;
        ring.positionCount = 64; ring.widthMultiplier = 0.09f;
        ring.shadowCastingMode = ShadowCastingMode.Off; ring.receiveShadows = false;
        material = new Material(shader); ring.sharedMaterial = material;
    }
    public void UpdateRange(Vector3 center, float radius, bool nearby, LayerMask groundMask)
    {
        if (ring == null) return;
        if (previousCenter != center || previousRadius != radius)
        {
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.positionCount;
                Vector3 point = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                ring.SetPosition(i, GroundUtility.Sample(point, groundMask) + Vector3.up * 0.15f);
            }
            previousCenter = center; previousRadius = radius;
        }
        Color color = nearby ? GameUI.Amber : GameUI.Cyan;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }
    public void UpdatePrompt(Vector3 center, bool nearby) { prompt?.Refresh(center, nearby); }
    public void SetVisible(bool visible)
    {
        if (ring != null) ring.enabled = visible;
        if (!visible && prompt != null) prompt.gameObject.SetActive(false);
    }
    public void Dispose() { if (material != null) Object.Destroy(material); }
}
