using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHUD : MonoBehaviour
{
    private PlayerHealth health;
    private Oxygen oxygen;
    private PlayerHUDView view;
    private bool deathShown, previousCursorVisible;
    private CursorLockMode previousCursorLock;
    private void Start()
    {
        health = GetComponent<PlayerHealth>(); oxygen = GetComponent<Oxygen>();
        view = GameInterface.Current.Hud; view.gameObject.SetActive(true);
    }
    private void LateUpdate()
    {
        if (health == null || view == null) return;
        view.Refresh(health, oxygen);
        if (!health.IsDead) return;
        if (!deathShown)
        {
            deathShown = true; previousCursorLock = Cursor.lockState; previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            GameEndScreen.Show("YOU DIED", "Try again and reach the escape rocket.", GameUI.Danger);
        }
    }
    private void OnEnable() { if (view != null) view.gameObject.SetActive(true); }
    private void OnDisable() { if (view != null) view.gameObject.SetActive(false); }
    private void OnDestroy()
    {
        if (deathShown) { Cursor.lockState = previousCursorLock; Cursor.visible = previousCursorVisible; }
    }
}
