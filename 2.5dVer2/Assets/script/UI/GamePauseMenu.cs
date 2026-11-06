using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed class GamePauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text hint;
    [SerializeField] private AudioOptionsUI options;
    [SerializeField] private Button firstButton;
    [SerializeField] private string mainMenuScene = "StartMenu";
    private bool wasOpen, wasMuted, leaving;
    private void LateUpdate()
    {
        GameAudio audio = GameAudio.Existing;
        bool active = audio != null && audio.HasActivePlayer;
        bool open = active && audio.IsPaused;
        if (panel.activeSelf != open) panel.SetActive(open);
        hint.gameObject.SetActive(active && !open);
        if (audio == null) return;
        if (open && !wasOpen)
        {
            transform.SetAsLastSibling();
            options.Bind(audio);
            if (firstButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
        if (audio.IsMuted != wasMuted || !wasOpen)
        {
            hint.text = audio.IsMuted ? "[ESC]  PAUSE     [M]  UNMUTE" : "[ESC]  PAUSE     [M]  MUTE";
            if (open) options.Refresh();
        }
        wasOpen = open; wasMuted = audio.IsMuted;
    }
    public void Resume() { GameAudio.Existing?.SetPaused(false); }
    public void Restart()
    {
        if (leaving) return;
        leaving = true;
        Resume();
        GameEndScreen.Restart();
    }
    public void ExitToMainMenu()
    {
        if (leaving) return;
        leaving = true;
        Resume();
        GameEndScreen.ResetView();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        SceneManager.LoadScene(mainMenuScene);
    }
}
