using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    private Enemy enemy;
    private Canvas canvas;
    private RectTransform canvasRect;
    private StatusBarUI healthBar;
    private Camera view;
    private Renderer[] bodyRenderers;
    private Collider bodyCollider;

    private void Start()
    {
        enemy = GetComponent<Enemy>();
        bodyRenderers = GetComponentsInChildren<Renderer>();
        bodyCollider = GetComponent<Collider>();

        var root = new GameObject("Enemy HP", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(160f, 28f);
        canvasRect.localScale = Vector3.one * 0.012f;
        healthBar = new StatusBarUI(root.transform, "Health", new Vector2(160f, 28f),
            new Color(0.85f, 0.12f, 0.12f), 17);
        healthBar.Rect.anchorMin = healthBar.Rect.anchorMax = new Vector2(0.5f, 0.5f);
        healthBar.Rect.pivot = new Vector2(0.5f, 0.5f);
        healthBar.Rect.anchoredPosition = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (enemy == null || canvas == null) return;
        healthBar.SetValue(enemy.health, enemy.MaxHealth, "HP");

        if (view == null || !view.isActiveAndEnabled)
            view = StatusBarUI.FindGameCamera();
        canvas.enabled = view != null;
        if (view == null) return;

        float headHeight = bodyCollider != null ? bodyCollider.bounds.max.y : transform.position.y + 1.8f;
        foreach (Renderer body in bodyRenderers)
            if (body != null && body.enabled)
                headHeight = Mathf.Max(headHeight, body.bounds.max.y);
        canvasRect.position = new Vector3(transform.position.x, headHeight + 0.35f, transform.position.z);
        canvasRect.rotation = view.transform.rotation;
        canvas.worldCamera = view;
    }

    private void OnEnable()
    {
        if (canvas != null) canvas.enabled = true;
    }

    private void OnDisable()
    {
        if (canvas != null) canvas.enabled = false;
    }

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
