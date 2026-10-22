using UnityEngine;

/// <summary>Keep screen-space UI inside the display safe area with a consistent breathing room.</summary>
[DisallowMultipleComponent]
public sealed class GameUiSafeArea : MonoBehaviour
{
    public float inset = 32f;
    private RectTransform rect;
    private Rect lastSafe;
    private Vector2 lastScreen;
    private Vector2 lastParent;

    private void OnEnable() { rect = transform as RectTransform; Apply(); }
    private void LateUpdate() { Apply(); }
    private void Apply()
    {
        if (!rect || !(rect.parent is RectTransform parent) || Screen.width <= 0 || Screen.height <= 0) return;
        var screen = new Vector2(Screen.width, Screen.height);
        Rect safe = Screen.safeArea;
        if (lastSafe == safe && lastScreen == screen && lastParent == parent.rect.size) return;
        lastSafe = safe; lastScreen = screen; lastParent = parent.rect.size;
        rect.anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
        rect.anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }
}
