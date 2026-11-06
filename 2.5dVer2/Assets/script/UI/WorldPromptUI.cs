using UnityEngine;

// A saved world-space Canvas that faces the game camera.
public sealed class WorldPromptUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    private Camera view;
    public void Refresh(Vector3 center, bool nearby)
    {
        if (view == null || !view.isActiveAndEnabled) view = StatusBarUI.FindGameCamera();
        bool visible = nearby && view != null && Time.timeScale > 0f;
        if (visible)
        {
            Vector3 anchor = center + Vector3.up * 1.6f + view.transform.right * 1.2f;
            Vector3 point = view.WorldToViewportPoint(anchor);
            visible = point.z > 0f && point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;
            transform.SetPositionAndRotation(anchor, view.transform.rotation); canvas.worldCamera = view;
        }
        if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
    }
}
