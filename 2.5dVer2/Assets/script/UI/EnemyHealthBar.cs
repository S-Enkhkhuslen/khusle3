using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    private Enemy enemy;
    private string enemyLabel;
    private Canvas canvas;
    private RectTransform canvasRect;
    private StatusBarUI healthBar;
    private Camera view;
    private Renderer[] bodyRenderers;
    private Collider bodyCollider;

    private void Start()
    {
        enemy = GetComponent<Enemy>();
        enemyLabel = GetComponent<FlyingEnemyAI>() != null ? "FLYING"
            : GetComponent<RangedEnemyAI>() != null ? "RANGED" : "MELEE";
        bodyRenderers = GetComponentsInChildren<Renderer>();
        bodyCollider = GetComponent<Collider>();

        GameObject root = Instantiate(Resources.Load<GameObject>("UI/EnemyHealthBar"), transform);
        canvas = root.GetComponent<Canvas>();
        canvasRect = root.GetComponent<RectTransform>();
        healthBar = root.GetComponentInChildren<StatusBarUI>();
    }

    private void LateUpdate()
    {
        if (enemy == null || canvas == null) return;
        healthBar.SetValue(enemy.health, enemy.MaxHealth, enemyLabel);

        if (view == null || !view.isActiveAndEnabled)
            view = StatusBarUI.FindGameCamera();
        canvas.enabled = view != null && !enemy.IsDead;
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
