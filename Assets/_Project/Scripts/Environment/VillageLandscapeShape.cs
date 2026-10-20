using UnityEngine;

// One coordinate mapping keeps curved paddies, their rice and water contacts aligned.
public static class VillageLandscapeShape
{
    public static float RoadBend(float z)
    {
        float t = Mathf.Clamp01((z + 5f) / 13f);
        return 2.6f * Mathf.Sin((z - 18.5f) * Mathf.PI / 25.5f) * t * t * (3f - 2f * t);
    }
    public static Vector3 Warp(Vector3 point) { point.x += RoadBend(point.z); return point; }
    public static Vector3 Unwarp(Vector3 point) { point.x -= RoadBend(point.z); return point; }
}
