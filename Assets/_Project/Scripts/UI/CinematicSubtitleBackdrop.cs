using UnityEngine;
using UnityEngine.UI;

/// <summary>A feathered scrim behind subtitles, without a hard rectangular plate.</summary>
public sealed class CinematicSubtitleBackdrop : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        const int columns = 24, rows = 12;
        for (int y = 0; y <= rows; y++)
        for (int x = 0; x <= columns; x++)
        {
            float u = x / (float)columns, v = y / (float)rows;
            float horizontal = Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) / .18f);
            float vertical = Mathf.Sin(v * Mathf.PI);
            vh.AddVert(new Vector3(Mathf.Lerp(r.xMin, r.xMax, u), Mathf.Lerp(r.yMin, r.yMax, v), 0f),
                new Color(.018f, .016f, .012f, .62f * horizontal * vertical), Vector2.zero);
        }
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
        {
            int i = y * (columns + 1) + x;
            vh.AddTriangle(i, i + columns + 1, i + 1);
            vh.AddTriangle(i + 1, i + columns + 1, i + columns + 2);
        }
    }
}
