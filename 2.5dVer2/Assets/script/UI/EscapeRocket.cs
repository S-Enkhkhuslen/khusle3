using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// Created only when the final generator successfully spawns its enemies.
public class EscapeRocket : MonoBehaviour
{
    [SerializeField, Min(1f)] private float boardingRadius = 3f;
    [SerializeField, Min(0.1f)] private float launchDuration = 3f;

    private Transform player;
    private PlayerHealth health;
    private bool launchInputArmed;
    private bool launching;
    private bool completed;
    private float launchTime;
    private float previousTimeScale;
    private float finalOxygen;
    private Vector3 launchPosition;
    private Camera launchCamera;
    private ParticleSystem exhaust;
    private GUIStyle textStyle;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    public bool IsCompleted => completed;

    public static EscapeRocket Spawn(Vector3 position, Transform player)
    {
        var root = new GameObject("Escape Rocket");
        root.transform.position = position;
        var rocket = root.AddComponent<EscapeRocket>();
        rocket.player = player;
        rocket.health = player != null ? player.GetComponent<PlayerHealth>() : null;
        root.AddComponent<RocketVisual>();
        GameAudio.RocketReady();
        return rocket;
    }

    private float HorizontalDistance()
    {
        Vector3 difference = player.position - transform.position;
        difference.y = 0f;
        return difference.magnitude;
    }

    private void Update()
    {
        if (completed)
        {
            if (GameEndScreen.HandleInput())
                Restart();
            return;
        }
        if (launching)
        {
            launchTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(launchTime / launchDuration);
            transform.position = launchPosition + Vector3.up * (35f * progress * progress);
            if (launchCamera != null)
            {
                launchCamera.transform.position = transform.position + new Vector3(13f, 7f, -18f);
                launchCamera.transform.LookAt(transform.position + Vector3.up * 4f);
            }
            if (progress >= 1f)
            {
                completed = true;
                GameScore.FinishRun(true, finalOxygen);
                GameAudio.CompleteLaunch();
                exhaust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            return;
        }
        if (player == null || !player.gameObject.activeInHierarchy || health == null
            || health.IsDead || Time.timeScale <= 0f)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        // The generator's E press must be released before a new launch press.
        if (!keyboard.eKey.isPressed) launchInputArmed = true;
        if (launchInputArmed && IsInBoardingRange() && keyboard.eKey.wasPressedThisFrame)
            BeginLaunch();
    }

    private bool IsInBoardingRange()
    {
        return player != null && HorizontalDistance() <= boardingRadius
            && Mathf.Abs(player.position.y - transform.position.y) <= 3f;
    }

    private void BeginLaunch()
    {
        if (launching) return;
        launching = true;
        GameAudio.BeginLaunch();
        Oxygen oxygen = player.GetComponent<Oxygen>();
        finalOxygen = oxygen != null ? oxygen.CurrentOxygen : 0f;
        launchPosition = transform.position;
        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        player.GetComponent<PlayerInput>()?.DeactivateInput();
        player.GetComponent<Sprint>()?.StopSprinting();
        player.GetComponent<Jetpack>()?.StopJetting();
        // Once boarded, the astronaut is inside the ship. Freeze combat while
        // the rocket animation runs with unscaled time.
        player.gameObject.SetActive(false);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        launchCamera = StatusBarUI.FindGameCamera();
        if (launchCamera != null)
        {
            var brain = launchCamera.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;
        }
        exhaust = GetComponent<RocketVisual>().StartExhaust();
    }

    private void Restart()
    {
        GameEndScreen.Restart(previousTimeScale);
    }

    private void OnGUI()
    {
        if (completed)
        {
            if (GameEndScreen.Draw("YOU ESCAPED!", "Mission complete. You left the planet.",
                GameScore.TotalScore, new Color(0.2f, 0.85f, 1f))) Restart();
            return;
        }
        if (textStyle == null)
            textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold, fontSize = 20, wordWrap = true };
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = -2000;
        float scale = Mathf.Max(0.01f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
        GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width * 0.5f, 0f), Quaternion.identity,
            new Vector3(scale, scale, 1f));
        DrawBackground(new Rect(-280f, 24f, 560f, 72f), 0.9f);
        GUI.color = new Color(0.2f, 0.85f, 1f);
        GUI.Label(new Rect(-275f, 26f, 550f, 30f), launching ? "LAUNCHING..." : "ESCAPE ROCKET READY", textStyle);
        GUI.color = Color.white;
        string hint = launching ? "Leaving the planet..."
            : IsInBoardingRange() ? "[E] LAUNCH"
            : "Approach the rocket  -  " + (player != null ? Mathf.CeilToInt(HorizontalDistance()) + " m" : "");
        GUI.Label(new Rect(-275f, 58f, 550f, 32f), hint, textStyle);
        GUI.matrix = previousMatrix;
        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    private static void DrawBackground(Rect rect, float alpha)
    {
        GUI.color = new Color(0.025f, 0.04f, 0.065f, alpha);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
    }

    private void OnDestroy()
    {
        if (!launching) return;
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }
}
