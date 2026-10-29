using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerHUD : MonoBehaviour
{
    private PlayerHealth health;
    private Oxygen oxygen;
    private Canvas canvas;
    private StatusBarUI healthBar;
    private StatusBarUI oxygenBar;

    private void Start()
    {
        health = GetComponent<PlayerHealth>();
        oxygen = GetComponent<Oxygen>();

        var root = new GameObject("Player Status HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        healthBar = new StatusBarUI(root.transform, "Player HP", new Vector2(280f, 28f),
            new Color(0.15f, 0.72f, 0.3f), 18);
        healthBar.Rect.anchoredPosition = new Vector2(22f, -22f);
        oxygenBar = new StatusBarUI(root.transform, "Player Oxygen", new Vector2(280f, 28f),
            new Color(0.05f, 0.65f, 0.9f), 18);
        oxygenBar.Rect.anchoredPosition = new Vector2(22f, -60f);
        RefreshBars();
    }

    private void LateUpdate()
    {
        RefreshBars();
    }

    private void RefreshBars()
    {
        if (healthBar != null && health != null)
            healthBar.SetValue(health.CurrentHealth, health.MaxHealth, "HP");
        if (oxygenBar != null && oxygen != null)
            oxygenBar.SetValue(oxygen.CurrentOxygen, oxygen.Capacity, "OXYGEN");
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
