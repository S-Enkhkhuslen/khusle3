using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

// All panels and controls are serialized scene objects, editable before Play.
public sealed class StartMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "SampleScene";
    [SerializeField] private GameObject mainPage, optionsPage, helpPage;
    [SerializeField] private AudioOptionsUI audioOptions;
    [SerializeField] private MenuHelpTabs helpTabs;
    [SerializeField] private Button startButton, optionsBack, helpBack;
    [SerializeField] private Text errorText, startLabel;
    private MenuAudio menuAudio;
    private readonly UIKeyActions keys = new UIKeyActions();
    private bool loading;
    private void Awake()
    {
        Time.timeScale = 1f; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        menuAudio = new MenuAudio(gameObject); audioOptions.Bind(menuAudio);
        ShowPage(mainPage, startButton);
        keys.Add("escape", () => { if (!loading && !mainPage.activeSelf) Back(); });
        keys.Add("m", () =>
        {
            if (loading) return;
            menuAudio.muted = !menuAudio.muted; menuAudio.Apply(); menuAudio.Save(); audioOptions.Refresh();
        });
    }
    private void ShowPage(GameObject page, Button selected)
    {
        mainPage.SetActive(page == mainPage); optionsPage.SetActive(page == optionsPage); helpPage.SetActive(page == helpPage);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected.gameObject);
    }
    public void OpenOptions() { menuAudio.Click(); audioOptions.Refresh(); ShowPage(optionsPage, optionsBack); }
    public void OpenHelp() { menuAudio.Click(); helpTabs.ShowControls(); ShowPage(helpPage, helpBack); }
    public void Back() { menuAudio.Click(); ShowPage(mainPage, startButton); }
    public void StartGame()
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(gameScene)) { errorText.text = "Gameplay scene is missing from Build Settings."; return; }
        loading = true; menuAudio.Click(); menuAudio.Save(); Time.timeScale = 1f;
        startLabel.text = "DEPLOYING..."; startButton.interactable = false;
        SceneManager.LoadSceneAsync(gameScene, LoadSceneMode.Single);
    }
    public void ExitGame()
    {
        menuAudio.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void OnDestroy() { keys.Dispose(); menuAudio?.Save(); }
}
