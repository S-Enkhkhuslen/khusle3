using UnityEngine;
using UnityEngine.EventSystems;

// Scene-owned Canvas, also available as an editable prefab for other gameplay scenes.
public sealed class GameInterface : MonoBehaviour
{
    [SerializeField] private PlayerHUDView hud;
    [SerializeField] private GamePauseMenu pause;
    [SerializeField] private ResultsView results;
    [SerializeField] private RocketPromptView rocket;
    public PlayerHUDView Hud => hud;
    public GamePauseMenu Pause => pause;
    public ResultsView Results => results;
    public RocketPromptView Rocket => rocket;
    public static GameInterface Existing { get; private set; }
    public static GameInterface Current
    {
        get
        {
            if (Existing == null) Existing = FindAnyObjectByType<GameInterface>();
            if (Existing == null) Existing = Instantiate(Resources.Load<GameObject>("UI/GameInterface")).GetComponent<GameInterface>();
            return Existing;
        }
    }
    private void Awake()
    {
        if (Existing != null && Existing != this) { Destroy(gameObject); return; }
        Existing = this;
        GameEndScreen.ResetView();
        results.Hide(); rocket.Hide();
        if (FindAnyObjectByType<EventSystem>() == null) Instantiate(Resources.Load<GameObject>("UI/UIEventSystem"));
    }
    private void OnDestroy() { if (Existing == this) Existing = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() { Existing = null; }
}
