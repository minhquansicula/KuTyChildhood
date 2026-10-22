using UnityEngine;

/// <summary>Fits a fixed-design mini-game card inside its canvas without clipping narrow screens.</summary>
[DisallowMultipleComponent]
public sealed class MiniGamePanelFit : MonoBehaviour
{
    public Vector2 margin = new Vector2(48f, 48f);
    private RectTransform rect;
    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();
    public void Refresh()
    {
        if (!rect) rect = transform as RectTransform;
        if (!rect || !(rect.parent is RectTransform parent)) return;
        var size = rect.rect.size;
        if (size.x <= 0 || size.y <= 0 || parent.rect.width <= 0 || parent.rect.height <= 0) return;
        float scale = Mathf.Min(1f, (parent.rect.width - margin.x * 2f) / size.x,
            (parent.rect.height - margin.y * 2f) / size.y);
        rect.localScale = Vector3.one * Mathf.Max(.1f, scale);
    }
}
