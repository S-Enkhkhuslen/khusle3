using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps the result flow; its visuals live in GameInterface.prefab.
public static class GameEndScreen
{
    public static bool ShowingLeaderboard { get; private set; }
    public static void Show(string title, string message, Color accent, float restartTimeScale = 1f)
    { GameInterface.Current.Results.Show(title, message, accent, restartTimeScale); }
    public static void ContinueToLeaderboard()
    {
        if (!GameScore.IsFinished) return;
        ShowingLeaderboard = true; GameInterface.Current.Results.RefreshPage();
    }
    public static void Restart(float timeScale = 1f)
    {
        ResetView(); Time.timeScale = timeScale;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetView() { ShowingLeaderboard = false; }
}
