using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class ResultsView : MonoBehaviour
{
    [SerializeField] private GameObject panel, breakdownPage, leaderboardPage;
    [SerializeField] private Text titleText, messageText, totalText, rankText, pageText, nextText;
    [SerializeField] private Image accentLine;
    [SerializeField] private ScoreRowUI[] breakdownRows, leaderboardRows;
    [SerializeField] private ScrollRect breakdownScroll, leaderboardScroll;
    private float restartTimeScale = 1f;
    private Color accent;
    private readonly UIKeyActions keys = new UIKeyActions();
    public bool IsShown => panel.activeSelf;
    public void Hide() { panel.SetActive(false); }
    private void Awake()
    {
        keys.Add("r", () => { if (IsShown && GameEndScreen.ShowingLeaderboard) GameEndScreen.Restart(restartTimeScale); });
    }

    public void Show(string title, string message, Color color, float timeScale)
    {
        if (IsShown) return;
        accent = color; restartTimeScale = timeScale;
        titleText.text = title; messageText.text = message;
        totalText.text = GameScore.TotalScore.ToString("N0"); totalText.color = color;
        accentLine.color = color; panel.SetActive(true);
        RefreshPage();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nextText.GetComponentInParent<Button>().gameObject);
    }
    public void RefreshPage()
    {
        bool leaderboard = GameEndScreen.ShowingLeaderboard;
        breakdownPage.SetActive(!leaderboard); leaderboardPage.SetActive(leaderboard);
        pageText.text = leaderboard ? "TOP 10 SCORES" : "SCORE BREAKDOWN";
        nextText.text = leaderboard ? "PLAY AGAIN   [R]" : "NEXT   [ENTER]";
        rankText.text = leaderboard && GameScore.SavedRank > 0 ? "RANK #" + GameScore.SavedRank : "";
        if (leaderboard) FillLeaderboard(); else FillBreakdown();
        Canvas.ForceUpdateCanvases();
        breakdownScroll.verticalNormalizedPosition = leaderboardScroll.verticalNormalizedPosition = 1f;
    }
    private void FillBreakdown()
    {
        string oxygen = GameScore.Escaped ? "Oxygen (" + GameScore.OxygenRemaining.ToString("0.0") + " x " + GameScore.OxygenPoints + ")" : "Oxygen bonus (escape only)";
        string[] labels = { "Enemies defeated", "Enemy points (" + GameScore.EnemiesKilled + " x " + GameScore.PointsPerEnemy + ")", "Combo bonus", "Combat subtotal", oxygen, "TOTAL SCORE" };
        string[] values = { GameScore.EnemiesKilled.ToString("N0"), GameScore.EnemyPoints.ToString("N0"), "+" + GameScore.ComboBonus.ToString("N0"), GameScore.CombatScore.ToString("N0"), "+" + GameScore.OxygenBonus.ToString("N0"), GameScore.TotalScore.ToString("N0") };
        for (int i = 0; i < breakdownRows.Length; i++) breakdownRows[i].Set(labels[i], values[i], i == 4 ? GameUI.Cyan : i == 5 ? accent : GameUI.Sand);
    }
    private void FillLeaderboard()
    {
        var entries = ScoreLeaderboard.Entries;
        for (int i = 0; i < leaderboardRows.Length; i++)
        {
            bool current = GameScore.SavedRank == i + 1;
            string score = i < entries.Count ? entries[i].score.ToString("N0") : "---";
            string result = i < entries.Count ? (entries[i].escaped ? "ESCAPED" : "DEFEATED") : "---";
            leaderboardRows[i].Set((i + 1).ToString("00") + "     " + score, result, current ? accent : GameUI.Sand, current);
        }
    }
    public void NextOrReplay()
    {
        if (GameEndScreen.ShowingLeaderboard) GameEndScreen.Restart(restartTimeScale);
        else GameEndScreen.ContinueToLeaderboard();
    }
    private void OnDestroy() { keys.Dispose(); }
}
