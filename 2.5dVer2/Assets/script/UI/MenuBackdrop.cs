using UnityEngine;

// The landscape is a saved texture. Only the existing dust Images move here.
public sealed class MenuBackdrop : MonoBehaviour
{
    [SerializeField] private RectTransform dustLayer;
    [SerializeField] private RectTransform[] dust;
    private void Update()
    {
        float width = dustLayer.rect.width, height = dustLayer.rect.height;
        if (width <= 0f || height <= 0f) return;
        for (int i = 0; i < dust.Length; i++)
            dust[i].anchoredPosition = new Vector2(Mathf.Repeat(i * 137.9f + Time.unscaledTime * (8f + i % 5), width), -Mathf.Repeat(i * 81.7f, height));
    }
}
