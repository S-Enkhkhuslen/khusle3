using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameScore
{
    public const int PointsPerEnemy = 50;
    public const float ComboWindow = 5f;

    public static int TotalScore { get; private set; }
    public static int LastPoints { get; private set; }
    public static float ComboTimeRemaining => Mathf.Max(0f, comboExpiresAt - Time.time);
    public static int ComboCount => ComboTimeRemaining > 0f ? comboCount : 0;

    private static int comboCount;
    private static float comboExpiresAt;

    public static void RegisterKill()
    {
        // Each kill within the deadline continues the chain and starts a fresh five seconds.
        comboCount = Time.time < comboExpiresAt ? comboCount + 1 : 1;
        LastPoints = PointsPerEnemy * comboCount;
        TotalScore += LastPoints;
        comboExpiresAt = Time.time + ComboWindow;
    }

    public static void ResetScore()
    {
        TotalScore = 0;
        LastPoints = 0;
        comboCount = 0;
        comboExpiresAt = 0f;
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
