using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Profile-fitted visual contents. Clips an inset cavity to a horizontal plane and caps it,
/// rather than scaling a reference cylinder through tapered walls. Not a fluid/volume solver.
/// </summary>
public sealed class WaterRiceFill : MonoBehaviour
{
    private const int Segments = 32;
    private Transform vessel, marker;
    private Mesh mesh;
    private MeshRenderer visual;
    private readonly List<Vector3> hull = new List<Vector3>(), vertices = new List<Vector3>(), cap = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private readonly List<Vector2> uv = new List<Vector2>();
    private readonly Vector3[] polygon = new Vector3[4];
    private float previous = -1;
    private Quaternion previousRotation;
    public float Level { get; private set; }
    public float SurfaceY { get; private set; }
    public Mesh VisualMesh => mesh;

    public void Configure(Transform owner, Transform floor, Vector2[] profile, float maximumHeight, Material material)
    {
        vessel = owner; marker = floor;
        mesh = new Mesh { name = "Profile-fitted contents (runtime)" }; mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        visual = gameObject.AddComponent<MeshRenderer>(); visual.sharedMaterial = material;
        if (material && material.shader.name == "KuTy/Household Water") gameObject.AddComponent<HouseholdWaterSurface>();
        var levels = new List<float>();
        foreach (var point in profile) if (point.x < maximumHeight) levels.Add(point.x);
        levels.Add(maximumHeight);
        for (int ring = 0; ring < levels.Count - 1; ring++) for (int segment = 0; segment < Segments; segment++)
        {
            Vector3 a = Point(levels[ring], segment, profile), b = Point(levels[ring + 1], segment, profile);
            Vector3 c = Point(levels[ring + 1], segment + 1, profile), d = Point(levels[ring], segment + 1, profile);
            hull.Add(a); hull.Add(b); hull.Add(c); hull.Add(a); hull.Add(c); hull.Add(d);
        }
        for (int i = 0; i < Segments; i++)
        {
            hull.Add(Vector3.up * (levels[0] + .0007f)); hull.Add(Point(levels[0], i, profile)); hull.Add(Point(levels[0], i + 1, profile));
            hull.Add(Vector3.up * (maximumHeight + .0007f)); hull.Add(Point(maximumHeight, i + 1, profile)); hull.Add(Point(maximumHeight, i, profile));
        }
    }
    public static float Radius(Vector2[] profile, float height)
    {
        for (int i = 1; i < profile.Length; i++) if (height <= profile[i].x)
            return Mathf.Lerp(profile[i - 1].y, profile[i].y, Mathf.InverseLerp(profile[i - 1].x, profile[i].x, height));
        return profile[profile.Length - 1].y;
    }
    private static Vector3 Point(float height, int segment, Vector2[] profile)
    {
        float angle = segment * Mathf.PI * 2 / Segments;
        float radius = Mathf.Max(.001f, Radius(profile, height) - .0015f);
        return new Vector3(Mathf.Cos(angle) * radius, height + .0007f, Mathf.Sin(angle) * radius);
    }
    public void SetLevel(float value)
    {
        Level = Mathf.Clamp01(value);
        SurfaceY += marker.position.y - transform.position.y;
        transform.SetPositionAndRotation(marker.position, Quaternion.identity);
        visual.enabled = Level > .005f;
        if (!visual.enabled || Mathf.Abs(previous - Level) < .0001f && Quaternion.Angle(previousRotation, vessel.rotation) < .01f) return;
        previous = Level; previousRotation = vessel.rotation;
        // Position comes from the exported ContentsRoot. Its rotation is not inherited:
        // vertices follow the cavity, but the clipped/capped top stays horizontal in world space.
        Matrix4x4 cavity = transform.worldToLocalMatrix * Matrix4x4.TRS(marker.position, vessel.rotation, vessel.lossyScale);
        float low = float.PositiveInfinity, high = float.NegativeInfinity;
        foreach (var p in hull) { float y = cavity.MultiplyPoint3x4(p).y; low = Mathf.Min(low, y); high = Mathf.Max(high, y); }
        float rimLow = float.PositiveInfinity;
        for (int i = hull.Count - Segments * 6; i < hull.Count; i += 6)
        { rimLow = Mathf.Min(rimLow, cavity.MultiplyPoint3x4(hull[i + 4]).y, cavity.MultiplyPoint3x4(hull[i + 5]).y); }
        float plane = Mathf.Min(Mathf.Lerp(low, high, Level), rimLow - .0002f);
        SurfaceY = transform.position.y + plane;
        vertices.Clear(); triangles.Clear(); uv.Clear(); cap.Clear();
        for (int i = 0; i < hull.Count; i += 3)
        {
            Vector3 a = cavity.MultiplyPoint3x4(hull[i]), b = cavity.MultiplyPoint3x4(hull[i + 1]), c = cavity.MultiplyPoint3x4(hull[i + 2]);
            int count = 0; ClipEdge(c, a, plane, ref count); ClipEdge(a, b, plane, ref count); ClipEdge(b, c, plane, ref count);
            for (int p = 1; p < count - 1; p++) AddTriangle(polygon[0], polygon[p], polygon[p + 1]);
        }
        if (cap.Count >= 3)
        {
            Vector3 center = Vector3.zero; foreach (var p in cap) center += p; center /= cap.Count;
            cap.Sort((a, b) => Mathf.Atan2(a.z - center.z, a.x - center.x).CompareTo(Mathf.Atan2(b.z - center.z, b.x - center.x)));
            for (int i = 0; i < cap.Count; i++) AddTriangle(center, cap[(i + 1) % cap.Count], cap[i]);
        }
        mesh.Clear(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv);
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
    }
    private void ClipEdge(Vector3 from, Vector3 to, float plane, ref int count)
    {
        bool a = from.y <= plane, b = to.y <= plane;
        if (a != b)
        {
            Vector3 point = Vector3.Lerp(from, to, (plane - from.y) / (to.y - from.y)); point.y = plane;
            polygon[count++] = point;
            bool duplicate = false; foreach (var p in cap) if ((p - point).sqrMagnitude < .00000001f) { duplicate = true; break; }
            if (!duplicate) cap.Add(point);
        }
        if (b) polygon[count++] = to;
    }
    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-14f) return;
        int index = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
        triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
        // One physical grain scale for basket, cup and receiver; never stretch it with fill height.
        uv.Add(new Vector2(a.x, a.z) * 3f); uv.Add(new Vector2(b.x, b.z) * 3f); uv.Add(new Vector2(c.x, c.z) * 3f);
    }
    private void OnDestroy() { if (mesh) Destroy(mesh); }
}
