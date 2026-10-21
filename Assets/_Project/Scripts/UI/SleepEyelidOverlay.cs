using UnityEngine;
using UnityEngine.UI;

/// <summary>Screen-space eyelids with a feathered edge; no texture or post-process dependency.</summary>
public sealed class SleepEyelidOverlay : MaskableGraphic
{
    private float closure;

    public static SleepEyelidOverlay Create(Transform parent)
    {
        var go = new GameObject("Eyelids", typeof(RectTransform), typeof(CanvasRenderer), typeof(SleepEyelidOverlay));
        go.transform.SetParent(parent, false);
        var overlay = go.GetComponent<SleepEyelidOverlay>();
        overlay.rectTransform.anchorMin = Vector2.zero;
        overlay.rectTransform.anchorMax = Vector2.one;
        overlay.rectTransform.offsetMin = overlay.rectTransform.offsetMax = Vector2.zero;
        overlay.color = Color.black;
        overlay.raycastTarget = false;
        return overlay;
    }

    public void SetClosure(float value)
    {
        closure = Mathf.Clamp01(value);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (closure <= 0f) return;
        Rect r = rectTransform.rect;
        Color clear = color;
        clear.a = 0f;
        // An asymmetric elliptical aperture, rather than two straight sliding panels.
        const int segments = 64;
        float openness = 1f - closure;
        float feather = r.height * .025f * Mathf.Sin(closure * Mathf.PI);
        for (int i = 0; i < segments; i++)
        {
            float u0 = i / (float)segments;
            float u1 = (i + 1f) / segments;
            float x0 = Mathf.Lerp(r.xMin, r.xMax, u0);
            float x1 = Mathf.Lerp(r.xMin, r.xMax, u1);
            // At zero closure the aperture extends beyond the screen, revealing every corner.
            float a0 = Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((u0 - .5f) * 1.6f, 2f)));
            float a1 = Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((u1 - .5f) * 1.6f, 2f)));
            float top0 = Mathf.Min(r.yMax, r.center.y + r.height * openness * .95f * a0);
            float top1 = Mathf.Min(r.yMax, r.center.y + r.height * openness * .95f * a1);
            float low0 = Mathf.Max(r.yMin, r.center.y - r.height * openness * 1.15f * a0);
            float low1 = Mathf.Max(r.yMin, r.center.y - r.height * openness * 1.15f * a1);
            AddStrip(vh, x0, x1, top0, top1, r.yMax, r.yMax, color, color);
            AddStrip(vh, x0, x1, top0 - feather, top1 - feather, top0, top1, clear, color);
            AddStrip(vh, x0, x1, r.yMin, r.yMin, low0, low1, color, color);
            AddStrip(vh, x0, x1, low0, low1, low0 + feather, low1 + feather, color, clear);
        }
    }

    private static void AddStrip(VertexHelper vh, float x0, float x1, float bottom0, float bottom1,
        float top0, float top1, Color low, Color high)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(x0, bottom0, 0f), low, Vector2.zero);
        vh.AddVert(new Vector3(x1, bottom1, 0f), low, Vector2.zero);
        vh.AddVert(new Vector3(x1, top1, 0f), high, Vector2.zero);
        vh.AddVert(new Vector3(x0, top0, 0f), high, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
