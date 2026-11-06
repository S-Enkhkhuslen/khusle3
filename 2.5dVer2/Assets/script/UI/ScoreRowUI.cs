using UnityEngine;
using UnityEngine.UI;

public sealed class ScoreRowUI : MonoBehaviour
{
    [SerializeField] private Text labelText, valueText;
    [SerializeField] private Image background;
    public void Set(string label, string value, Color color, bool highlight = false)
    {
        labelText.text = label; valueText.text = value;
        labelText.color = valueText.color = color;
        if (background != null) background.color = highlight ? GameUI.Line : GameUI.PanelColor;
    }
}
