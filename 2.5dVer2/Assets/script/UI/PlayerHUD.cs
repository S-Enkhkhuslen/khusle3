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
    private RectTransform scorePanel;
    private Text scoreText;
    private Text comboText;
    private RectTransform comboTimer;
    private RectTransform comboFill;
    private int displayedScore = -1;
    private int displayedCombo = -1;
    private int displayedTenths = -1;

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
        CreateScorePanel(root.transform);
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
        RefreshScore();
    }

    private void CreateScorePanel(Transform parent)
    {
        scorePanel = CreateRect("Score Panel", parent, new Vector2(260f, 52f));
        scorePanel.anchorMin = scorePanel.anchorMax = Vector2.one;
        scorePanel.pivot = Vector2.one;
        scorePanel.anchoredPosition = new Vector2(-22f, -22f);
        Image background = scorePanel.gameObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.045f, 0.065f, 0.95f);
        background.raycastTarget = false;
        scoreText = CreateText("Total Score", scorePanel, new Vector2(14f, -8f), 26);
        comboText = CreateText("Combo", scorePanel, new Vector2(14f, -42f), 18);
        comboText.color = new Color(1f, 0.72f, 0.22f);
        comboText.resizeTextForBestFit = true;
        comboText.resizeTextMinSize = 14;
        comboText.resizeTextMaxSize = 18;

        comboTimer = CreateRect("Combo Timer", scorePanel, new Vector2(232f, 5f));
        comboTimer.anchoredPosition = new Vector2(14f, -76f);
        Image track = comboTimer.gameObject.AddComponent<Image>();
        track.color = new Color(0.15f, 0.2f, 0.27f);
        track.raycastTarget = false;
        comboFill = CreateRect("Fill", comboTimer, Vector2.zero);
        comboFill.anchorMin = Vector2.zero;
        comboFill.anchorMax = Vector2.one;
        comboFill.offsetMin = comboFill.offsetMax = Vector2.zero;
        Image fill = comboFill.gameObject.AddComponent<Image>();
        fill.color = new Color(0.12f, 0.8f, 1f);
        fill.raycastTarget = false;
    }

    private void RefreshScore()
    {
        if (scoreText == null) return;
        if (displayedScore != GameScore.TotalScore)
        {
            displayedScore = GameScore.TotalScore;
            scoreText.text = "SCORE " + displayedScore;
        }

        float remaining = GameScore.ComboTimeRemaining;
        int combo = GameScore.ComboCount;
        bool active = combo > 0;
        comboText.gameObject.SetActive(active);
        comboTimer.gameObject.SetActive(active);
        scorePanel.sizeDelta = new Vector2(260f, active ? 94f : 52f);
        comboFill.anchorMax = new Vector2(remaining / GameScore.ComboWindow, 1f);
        int tenths = Mathf.CeilToInt(remaining * 10f);
        if (displayedCombo == combo && displayedTenths == tenths) return;
        displayedCombo = combo;
        displayedTenths = tenths;
        if (active)
            comboText.text = "COMBO x" + combo + "   +" + GameScore.LastPoints
                + "   " + (tenths / 10f).ToString("0.0") + "s";
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        return rect;
    }

    private static Text CreateText(string name, Transform parent, Vector2 position, int fontSize)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(232f, 30f));
        rect.anchoredPosition = position;
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
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
