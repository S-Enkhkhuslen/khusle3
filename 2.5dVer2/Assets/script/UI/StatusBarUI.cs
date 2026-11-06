using UnityEngine;
using UnityEngine.UI;

// Updates a saved health / oxygen bar; creates no UI objects.
public sealed class StatusBarUI : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private Text labelText, valueText;
    [SerializeField] private Color normalColor = Color.white;
    private int shownValue = -1, shownLimit = -1;

    public void SetValue(float current, float maximum, string label)
    {
        float fraction = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        fill.fillAmount = fraction;
        fill.enabled = fraction > 0f;
        fill.color = fraction <= 0.2f ? GameUI.Danger : normalColor;
        int value = Mathf.CeilToInt(Mathf.Max(0f, current)), limit = Mathf.CeilToInt(Mathf.Max(0f, maximum));
        if (value == shownValue && limit == shownLimit && labelText.text == label) return;
        shownValue = value; shownLimit = limit;
        labelText.text = label; valueText.text = value + " / " + limit;
    }
    public static Camera FindGameCamera()
    {
        if (Camera.main != null) return Camera.main;
        foreach (Camera camera in Camera.allCameras)
            if (camera.cameraType == CameraType.Game && camera.targetTexture == null) return camera;
        return null;
    }
}
