using UnityEngine;
using UnityEngine.UI;

// A bar made from rectangles, so an empty fill has no sprite border left over.
public class StatusBarUI
{
    public RectTransform Rect { get; private set; }
    private RectTransform fillRect;
    private Image fillImage;
    private Text valueText;
    private int displayedCurrent = -1;
    private int displayedMaximum = -1;
    private string displayedLabel;

    public StatusBarUI(Transform parent, string name, Vector2 size, Color color, int fontSize)
    {
        Rect = CreateRect(name, parent);
        Rect.anchorMin = Rect.anchorMax = new Vector2(0f, 1f);
        Rect.pivot = new Vector2(0f, 1f);
        Rect.sizeDelta = size;
        Image background = Rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.045f, 0.065f, 0.95f);
        background.raycastTarget = false;

        RectTransform fillArea = CreateRect("Fill Area", Rect);
        Stretch(fillArea);
        fillArea.offsetMin = new Vector2(2f, 2f);
        fillArea.offsetMax = new Vector2(-2f, -2f);
        fillRect = CreateRect("Fill", fillArea);
        Stretch(fillRect);
        fillImage = fillRect.gameObject.AddComponent<Image>();
        fillImage.color = color;
        fillImage.raycastTarget = false;

        RectTransform textRect = CreateRect("Value", Rect);
        Stretch(textRect);
        valueText = textRect.gameObject.AddComponent<Text>();
        valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        valueText.fontSize = fontSize;
        valueText.fontStyle = FontStyle.Bold;
        valueText.alignment = TextAnchor.MiddleCenter;
        valueText.color = Color.white;
        valueText.raycastTarget = false;
        Shadow shadow = textRect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    public void SetValue(float current, float maximum, string label)
    {
        float fraction = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        fillRect.anchorMax = new Vector2(fraction, 1f);
        fillImage.enabled = fraction > 0f;
        int currentNumber = Mathf.CeilToInt(Mathf.Max(0f, current));
        int maximumNumber = Mathf.CeilToInt(Mathf.Max(0f, maximum));
        if (currentNumber == displayedCurrent && maximumNumber == displayedMaximum
            && label == displayedLabel) return;

        displayedCurrent = currentNumber;
        displayedMaximum = maximumNumber;
        displayedLabel = label;
        valueText.text = label + " " + currentNumber + " / " + maximumNumber;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public static Camera FindGameCamera()
    {
        if (Camera.main != null) return Camera.main;
        foreach (Camera camera in Camera.allCameras)
            if (camera.cameraType == CameraType.Game && camera.targetTexture == null)
                return camera;
        return null;
    }
}
