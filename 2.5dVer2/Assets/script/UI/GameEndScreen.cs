using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// Shared presentation for death and successful escape.
public static class GameEndScreen
{
    private static GUIStyle titleStyle;
    private static GUIStyle textStyle;
    private static GUIStyle buttonStyle;
    private static GUIStyle rowStyle, scoreStyle, headingStyle;
    private static Vector2 scoreScroll;
    public static bool ShowingLeaderboard { get; private set; }

    public static void ContinueToLeaderboard()
    {
        if (!GameScore.IsFinished) return;
        ShowingLeaderboard = true;
        scoreScroll = Vector2.zero;
    }

    public static bool HandleInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;
        if (!ShowingLeaderboard)
        {
            if (keyboard.enterKey.wasPressedThisFrame) ContinueToLeaderboard();
            return false;
        }
        return keyboard.rKey.wasPressedThisFrame;
    }

    public static bool Draw(string title, string message, int score, Color accent)
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold, fontSize = 32 };
            textStyle = new GUIStyle(titleStyle) { fontSize = 20, wordWrap = true };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
            scoreStyle = new GUIStyle(rowStyle) { alignment = TextAnchor.MiddleRight };
            headingStyle = new GUIStyle(rowStyle) { fontSize = 16, fontStyle = FontStyle.Bold };
        }
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = -3000;
        // Keep the text readable in a narrow Game view. Shrink the panel's
        // width and wrap its message instead of reducing every letter to a few pixels.
        float scale = Mathf.Max(0.65f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
        GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width * 0.5f, 0f), Quaternion.identity,
            new Vector3(scale, scale, 1f));
        float height = Screen.height / scale;
        float width = Mathf.Min(600f, Mathf.Max(1f, (Screen.width - 24f) / scale));
        GUI.color = new Color(0.025f, 0.04f, 0.065f, 0.86f);
        GUI.DrawTexture(new Rect(-Screen.width / scale * 0.5f, 0f, Screen.width / scale, height), Texture2D.whiteTexture);
        float top = Mathf.Max(12f, (height - 560f) * 0.5f);
        GUI.color = accent;
        GUI.Label(new Rect(-width * 0.5f, top, width, 55f), title, titleStyle);
        GUI.color = Color.white;
        string rank = ShowingLeaderboard && GameScore.SavedRank > 0 ? "   |   RANK #" + GameScore.SavedRank : "";
        GUI.Label(new Rect(-width * 0.5f, top + 55f, width, 36f), "FINAL SCORE  " + score + rank, textStyle);
        GUI.Label(new Rect(-width * 0.5f, top + 92f, width, 42f), message, textStyle);
        float boardTop = top + 144f;
        float boardHeight = Mathf.Clamp(height - boardTop - 100f, 70f, 260f);
        if (ShowingLeaderboard)
            DrawLeaderboard(new Rect(-width * 0.5f, boardTop, width, boardHeight + 30f), accent);
        else
            DrawBreakdown(new Rect(-width * 0.5f, boardTop, width, boardHeight + 30f), accent);
        float buttonWidth = Mathf.Min(280f, width);
        bool pressed = GUI.Button(new Rect(-buttonWidth * 0.5f, boardTop + boardHeight + 44f, buttonWidth, 45f),
            ShowingLeaderboard ? "PLAY AGAIN  (R)" : "NEXT  (ENTER)", buttonStyle);
        bool restart = pressed && ShowingLeaderboard;
        if (pressed && !ShowingLeaderboard) ContinueToLeaderboard();
        GUI.matrix = previousMatrix;
        GUI.color = previousColor;
        GUI.depth = previousDepth;
        return restart;
    }

    private static void DrawBreakdown(Rect rect, Color accent)
    {
        GUI.color = accent;
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 28f), "SCORE BREAKDOWN", textStyle);
        GUI.color = Color.white;
        float contentWidth = Mathf.Max(1f, rect.width - 22f);
        Rect viewport = new Rect(rect.x, rect.y + 30f, rect.width, rect.height - 30f);
        scoreScroll = GUI.BeginScrollView(viewport, scoreScroll, new Rect(0f, 0f, contentWidth, 252f));
        BreakdownRow(0, contentWidth, "Enemies defeated", GameScore.EnemiesKilled.ToString("N0"));
        BreakdownRow(1, contentWidth, "Enemy points (" + GameScore.EnemiesKilled + " x " + GameScore.PointsPerEnemy + ")", GameScore.EnemyPoints.ToString("N0"));
        BreakdownRow(2, contentWidth, "Combo bonus", "+" + GameScore.ComboBonus.ToString("N0"));
        BreakdownRow(3, contentWidth, "Combat subtotal", GameScore.CombatScore.ToString("N0"));
        string oxygenLabel = GameScore.Escaped
            ? "Oxygen (" + GameScore.OxygenRemaining.ToString("0.0") + " x " + GameScore.OxygenPoints + ")"
            : "Oxygen bonus (escape only)";
        BreakdownRow(4, contentWidth, oxygenLabel, "+" + GameScore.OxygenBonus.ToString("N0"));
        GUI.color = accent;
        BreakdownRow(5, contentWidth, "TOTAL SCORE", GameScore.TotalScore.ToString("N0"));
        GUI.EndScrollView();
        GUI.color = Color.white;
    }

    private static void BreakdownRow(int index, float width, string label, string value)
    {
        float y = index * 42f;
        GUI.Label(new Rect(10f, y, width * 0.73f - 10f, 38f), label, rowStyle);
        GUI.Label(new Rect(width * 0.73f, y, width * 0.27f - 10f, 38f), value, scoreStyle);
    }

    private static void DrawLeaderboard(Rect rect, Color accent)
    {
        GUI.color = accent;
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 28f), "TOP 10 SCORES", textStyle);
        float contentWidth = Mathf.Max(1f, rect.width - 22f);
        Rect viewport = new Rect(rect.x, rect.y + 30f, rect.width, rect.height - 30f);
        GUI.color = Color.white;
        scoreScroll = GUI.BeginScrollView(viewport, scoreScroll, new Rect(0f, 0f, contentWidth, 260f));
        var entries = ScoreLeaderboard.Entries;
        for (int i = 0; i < ScoreLeaderboard.Capacity; i++)
        {
            bool highlight = GameScore.SavedRank == i + 1;
            GUI.color = highlight ? new Color(accent.r * 0.25f, accent.g * 0.25f, accent.b * 0.25f, 0.9f)
                : new Color(0.08f, 0.11f, 0.15f, i % 2 == 0 ? 0.9f : 0.5f);
            GUI.DrawTexture(new Rect(0f, i * 26f, contentWidth, 25f), Texture2D.whiteTexture);
            GUI.color = highlight ? accent : Color.white;
            GUI.Label(new Rect(10f, i * 26f, 45f, 25f), (i + 1).ToString(), headingStyle);
            string points = i < entries.Count ? entries[i].score.ToString("N0") : "---";
            string result = i < entries.Count ? (entries[i].escaped ? "ESCAPED" : "DEFEATED") : "---";
            GUI.Label(new Rect(55f, i * 26f, contentWidth * 0.45f - 55f, 25f), points, scoreStyle);
            GUI.Label(new Rect(contentWidth * 0.55f, i * 26f, contentWidth * 0.45f - 8f, 25f), result, rowStyle);
        }
        GUI.EndScrollView();
        GUI.color = Color.white;
    }

    public static void Restart(float timeScale = 1f)
    {
        scoreScroll = Vector2.zero;
        ShowingLeaderboard = false;
        Time.timeScale = timeScale;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStyles()
    {
        titleStyle = null;
        textStyle = null;
        buttonStyle = null;
        rowStyle = scoreStyle = headingStyle = null;
        scoreScroll = Vector2.zero;
        ShowingLeaderboard = false;
    }
}
