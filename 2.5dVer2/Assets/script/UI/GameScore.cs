using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameScore
{
    public const int PointsPerEnemy = 50;
    public const float ComboWindow = 5f;
    public const int OxygenPoints = 10;

    public static int TotalScore { get; private set; }
    public static int LastPoints { get; private set; }
    public static bool IsFinished { get; private set; }
    public static int SavedRank { get; private set; }
    public static int EnemiesKilled { get; private set; }
    public static int CombatScore { get; private set; }
    public static int EnemyPoints => EnemiesKilled * PointsPerEnemy;
    public static int ComboBonus => CombatScore - EnemyPoints;
    public static float OxygenRemaining { get; private set; }
    public static int OxygenBonus { get; private set; }
    public static bool Escaped { get; private set; }
    public static float ComboTimeRemaining => Mathf.Max(0f, comboExpiresAt - Time.time);
    public static int ComboCount => ComboTimeRemaining > 0f ? comboCount : 0;

    private static int comboCount;
    private static float comboExpiresAt;

    public static void RegisterKill()
    {
        if (IsFinished) return;
        // Each kill within the deadline continues the chain and starts a fresh five seconds.
        comboCount = Time.time < comboExpiresAt ? comboCount + 1 : 1;
        LastPoints = PointsPerEnemy * comboCount;
        EnemiesKilled++;
        CombatScore += LastPoints;
        TotalScore = CombatScore;
        comboExpiresAt = Time.time + ComboWindow;
    }

    public static void FinishRun(bool escaped, float remainingOxygen = 0f)
    {
        if (IsFinished) return;
        IsFinished = true;
        Escaped = escaped;
        // Scores are whole points; the breakdown displays oxygen to one decimal.
        OxygenBonus = escaped ? Mathf.RoundToInt(Mathf.Max(0f, remainingOxygen) * OxygenPoints) : 0;
        OxygenRemaining = OxygenBonus / (float)OxygenPoints;
        TotalScore = CombatScore + OxygenBonus;
        SavedRank = ScoreLeaderboard.Add(TotalScore, escaped);
    }

    public static void ResetScore()
    {
        TotalScore = 0;
        LastPoints = 0;
        comboCount = 0;
        comboExpiresAt = 0f;
        IsFinished = false;
        SavedRank = 0;
        EnemiesKilled = 0;
        CombatScore = 0;
        OxygenRemaining = 0f;
        OxygenBonus = 0;
        Escaped = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        ResetScore();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) ResetScore();
    }
}
