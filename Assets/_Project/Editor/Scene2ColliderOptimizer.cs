using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Scene 2 activation is dominated by non-convex MeshColliders on dense imported art.
/// Lists them and (when requested) replaces them with box colliders fitted to the visuals.
/// </summary>
public static class Scene2ColliderOptimizer
{
    private const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";

    [MenuItem("KuTy/Scene 2/Audit heavy mesh colliders")]
    public static void Audit() => Process(false);

    [MenuItem("KuTy/Scene 2/Convert heavy mesh colliders to boxes")]
    public static void Convert() => Process(true);

    private static void Process(bool apply)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var report = new StringBuilder();
        var targets = new List<MeshCollider>();
        foreach (var collider in Object.FindObjectsOfType<MeshCollider>(true))
            if (collider.gameObject.scene == scene) targets.Add(collider);

        targets.Sort((a, b) => Vertices(b).CompareTo(Vertices(a)));
        long totalVertices = 0;
        foreach (var collider in targets) totalVertices += Vertices(collider);
        report.AppendLine($"[Scene2Colliders] meshColliders={targets.Count} totalVertices={totalVertices}");

        int converted = 0;
        foreach (var collider in targets)
        {
            var renderers = collider.GetComponents<Renderer>();
            if (renderers.Length == 0) renderers = collider.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0 || collider.sharedMesh == null) continue;

            Bounds world = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);
            Vector3 size = world.size;
            report.AppendLine($"[Scene2Colliders] {Path(collider.transform)} verts={Vertices(collider)} " +
                $"tris={(collider.sharedMesh.triangles.Length / 3)} size=({size.x:F2}, {size.y:F2}, {size.z:F2}) " +
                $"pos=({collider.transform.position.x:F2}, {collider.transform.position.y:F2}, {collider.transform.position.z:F2}) " +
                $"active={collider.gameObject.activeInHierarchy} trigger={collider.isTrigger} convex={collider.convex}");

            if (!apply) continue;
            // Keep exact collision for the walk-in shell; boxes would seal rooms or stairs.
            string path = Path(collider.transform);
            if (path.Contains("gianbep2") || path.Contains("TraditionalHouseInterior") ||
                path.Contains("RusticBedroom_Visual")) continue;
            var box = collider.gameObject.AddComponent<BoxCollider>();
            var transform = collider.transform;
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 corner = transform.InverseTransformPoint(new Vector3(
                    x == 0 ? world.min.x : world.max.x,
                    y == 0 ? world.min.y : world.max.y,
                    z == 0 ? world.min.z : world.max.z));
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
            }
            box.center = (min + max) * .5f;
            box.size = max - min;
            box.isTrigger = collider.isTrigger;
            Object.DestroyImmediate(collider);
            converted++;
        }

        if (apply)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine($"[Scene2Colliders] converted={converted}");
        }
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log(report.ToString());
    }

    private static int Vertices(MeshCollider collider)
        => collider.sharedMesh != null ? collider.sharedMesh.vertexCount : 0;

    private static string Path(Transform transform)
    {
        string path = transform.name;
        for (var parent = transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;
        return path;
    }
}
