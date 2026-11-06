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
        if (completed) return;
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
                GameInterface.Current.Rocket.Hide();
                GameEndScreen.Show("YOU ESCAPED!", "Mission complete. You left the planet.", GameUI.Cyan, previousTimeScale);
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

    private void LateUpdate()
    {
        if (completed) return;
        GameInterface.Current.Rocket.Refresh(IsInBoardingRange(), launching,
            launchTime / launchDuration, player != null ? HorizontalDistance() : 0f);
    }

    private void OnDestroy()
    {
        if (!launching) return;
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }
}
