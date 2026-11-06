using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// Restores the exact time, cursor and input state that existed before pausing.
public sealed class PauseState
{
    private PlayerHealth player;
    private PlayerInput input;
    private TPSAim aim;
    private CinemachineInputAxisController[] cameras;
    private bool[] cameraStates;
    private bool inputWasActive, aimWasEnabled, cursorWasVisible;
    private CursorLockMode cursorWasLocked;
    private float previousTimeScale;

    public void Pause(PlayerHealth health)
    {
        player = health; input = player != null ? player.GetComponent<PlayerInput>() : null;
        aim = player != null ? player.GetComponent<TPSAim>() : null;
        previousTimeScale = Time.timeScale; cursorWasLocked = Cursor.lockState; cursorWasVisible = Cursor.visible;
        inputWasActive = input != null && input.inputIsActive; aimWasEnabled = aim != null && aim.enabled;
        cameras = Object.FindObjectsByType<CinemachineInputAxisController>(); cameraStates = new bool[cameras.Length];
        for (int i = 0; i < cameras.Length; i++) { cameraStates[i] = cameras[i].enabled; cameras[i].enabled = false; }
        input?.DeactivateInput(); if (aim != null) aim.enabled = false;
        Time.timeScale = 0f; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
    public void Resume()
    {
        Time.timeScale = previousTimeScale;
        if (player != null && !player.IsDead)
        {
            if (inputWasActive && input != null) input.ActivateInput();
            if (aim != null) aim.enabled = aimWasEnabled;
        }
        if (cameras != null)
            for (int i = 0; i < cameras.Length; i++) if (cameras[i] != null) cameras[i].enabled = cameraStates[i];
        Cursor.lockState = cursorWasLocked; Cursor.visible = cursorWasVisible;
    }
}
