using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHUDView : MonoBehaviour
{
    [SerializeField] private StatusBarUI healthBar, oxygenBar;
    [SerializeField] private RectTransform scorePanel;
    [SerializeField] private Text scoreText, comboText;
    [SerializeField] private GameObject comboTimer;
    [SerializeField] private Image comboFill;
    private int shownScore = -1, shownTick = -1, shownCombo = -1;

    public void Refresh(PlayerHealth health, Oxygen oxygen)
    {
        healthBar.SetValue(health.CurrentHealth, health.MaxHealth, "HEALTH");
        if (oxygen != null) oxygenBar.SetValue(oxygen.CurrentOxygen, oxygen.Capacity, "OXYGEN");
        if (shownScore != GameScore.TotalScore)
        { shownScore = GameScore.TotalScore; scoreText.text = shownScore.ToString("N0"); }
        bool combo = GameScore.ComboCount > 0 && !GameScore.IsFinished;
        comboText.gameObject.SetActive(combo); comboTimer.SetActive(combo);
        scorePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, combo ? 116f : 78f);
        // On very narrow screens stack the score below the suit readout.
        bool narrow = ((RectTransform)transform).rect.width < 620f;
        Vector2 anchor = narrow ? new Vector2(0f, 1f) : Vector2.one;
        scorePanel.anchorMin = scorePanel.anchorMax = scorePanel.pivot = anchor;
        scorePanel.anchoredPosition = narrow ? new Vector2(24f, -172f) : new Vector2(-24f, -22f);
        if (!combo) return;
        int tick = Mathf.CeilToInt(GameScore.ComboTimeRemaining * 10f);
        if (tick != shownTick || shownCombo != GameScore.ComboCount)
        {
            shownTick = tick; shownCombo = GameScore.ComboCount;
            comboText.text = "CHAIN x" + shownCombo + "   +" + GameScore.LastPoints + "   " + (tick / 10f).ToString("0.0") + "s";
        }
        comboFill.fillAmount = GameScore.ComboTimeRemaining / GameScore.ComboWindow;
    }
}
