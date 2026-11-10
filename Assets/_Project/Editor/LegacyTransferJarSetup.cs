using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LegacyTransferJarSetup
{
    public const string WaterPath = "HouseLayout/TraditionalHouseInterior/WaterJarVisual";
    public const string RicePath = "HouseLayout/KitchenArea/RiceJar/cailugao";
    private const string Root = WaterRiceAssetSetup.Root + "/LegacyJars";
    [MenuItem("KuTy/Scene 2/Use existing rice and water jars")]
    public static void Bind()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity" || scene.isDirty)
            throw new InvalidOperationException("Open saved Home in Edit Mode first.");
        Directory.CreateDirectory(Root); AssetDatabase.Refresh();
        var water = Bake(WaterPath, "WaterJar"); var rice = Bake(RicePath, "RiceJar");
        var sim = UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>();
        var serialized = new SerializedObject(sim); var stations = serialized.FindProperty("stations");
        for (int i = 0; i < stations.arraySize; i++)
        {
            var station = stations.GetArrayElementAtIndex(i); int chore = station.FindPropertyRelative("chore").intValue;
            if (chore == (int)HouseholdChore.FetchWater || chore == (int)HouseholdChore.MeasureRice)
                station.FindPropertyRelative("transferJar").objectReferenceValue = chore == (int)HouseholdChore.FetchWater ? water : rice;
        }
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(sim); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("[Legacy jars] Bound original lu nuoc/lu gao in place. Original meshes, materials and transforms untouched. Open-body/lid copies are used only during the activity; no new bucket/basket/table.");
    }
    private struct Vertex
    {
        public Vector3 position, normal; public Vector2 uv;
        public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex { position = Vector3.Lerp(a.position, b.position, t), normal = Vector3.Lerp(a.normal, b.normal, t).normalized, uv = Vector2.Lerp(a.uv, b.uv, t) };
    }
    private static HouseholdJarSource Bake(string path, string name)
    {
        var go = GameObject.Find(path); var renderer = go.GetComponent<MeshRenderer>(); var mesh = go.GetComponent<MeshFilter>().sharedMesh;
        if (mesh.subMeshCount != 1) throw new InvalidOperationException("Expected one original jar material.");
        bool alreadyOpen = name == "WaterJar";
        float plane = renderer.bounds.min.y + renderer.bounds.size.y * (alreadyOpen ? .94f : .76f);
        Mesh body = Clip(mesh, go.transform, plane, true, out var outline);
        Mesh lid = alreadyOpen ? null : Clip(mesh, go.transform, plane, false, out _);
        if (alreadyOpen) { UnityEngine.Object.DestroyImmediate(body); body = UnityEngine.Object.Instantiate(mesh); }
        if (outline.Count < 3) throw new InvalidOperationException("No jar opening intersection: " + path);
        Bounds rim = new Bounds(outline[0], Vector3.zero); foreach (var point in outline) rim.Encapsulate(point);
        var data = go.GetComponent<HouseholdJarSource>();
        if (!data) data = Undo.AddComponent<HouseholdJarSource>(go);
        Undo.RecordObject(data, "Bind original jar opening"); data.original = renderer;
        data.openBody = SaveMesh(body, name + "_OpenBody"); data.openedLid = lid ? SaveMesh(lid, name + "_OpenedLid") : null;
        data.mouthLocal = go.transform.InverseTransformPoint(new Vector3(rim.center.x, plane, rim.center.z));
        data.openingRadius = Mathf.Min(rim.extents.x, rim.extents.z) * .86f;
        if (data.openingRadius < .06f) throw new InvalidOperationException("Jar opening unexpectedly small: " + path);
        EditorUtility.SetDirty(data);
        Debug.Log("[Legacy jar] " + path + " mouth=" + data.Mouth.ToString("F3") + " usable radius=" + data.openingRadius.ToString("F3") + ". Fused cap is split in derived meshes, not original asset.");
        return data;
    }
    private static Mesh SaveMesh(Mesh mesh, string name)
    {
        mesh.name = name; string path = Root + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!existing) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); return existing;
    }
    private static Mesh Clip(Mesh source, Transform transform, float plane, bool below, out List<Vector3> outline)
    {
        var positions = source.vertices; var normals = source.normals; var uvs = source.uv; var indices = source.triangles;
        var vertices = new List<Vector3>(); var outputNormals = new List<Vector3>(); var outputUV = new List<Vector2>(); var outputIndices = new List<int>();
        var polygon = new Vertex[4]; outline = new List<Vector3>();
        for (int i = 0; i < indices.Length; i += 3)
        {
            int count = 0;
            for (int edge = 0; edge < 3; edge++)
            {
                int from = indices[i + (edge + 2) % 3], to = indices[i + edge];
                var a = new Vertex { position = positions[from], normal = normals[from], uv = uvs[from] };
                var b = new Vertex { position = positions[to], normal = normals[to], uv = uvs[to] };
                float ay = transform.TransformPoint(a.position).y, by = transform.TransformPoint(b.position).y;
                bool keepA = below ? ay <= plane : ay >= plane, keepB = below ? by <= plane : by >= plane;
                if (keepA != keepB)
                {
                    Vertex cross = Vertex.Lerp(a, b, (plane - ay) / (by - ay)); polygon[count++] = cross;
                    outline.Add(transform.TransformPoint(cross.position));
                }
                if (keepB) polygon[count++] = b;
            }
            for (int p = 1; p < count - 1; p++) foreach (int v in new[] { 0, p, p + 1 })
            { outputIndices.Add(vertices.Count); vertices.Add(polygon[v].position); outputNormals.Add(polygon[v].normal); outputUV.Add(polygon[v].uv); }
        }
        var result = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        result.SetVertices(vertices); result.SetNormals(outputNormals); result.SetUVs(0, outputUV); result.SetTriangles(outputIndices, 0); result.RecalculateTangents(); result.RecalculateBounds(); return result;
    }
    [MenuItem("KuTy/Scene 2/Inspect existing water and rice jars")]
    public static void Inspect()
    {
        var report = new StringBuilder();
        foreach (string path in new[] { WaterPath, RicePath })
        {
            var source = GameObject.Find(path); var renderer = source.GetComponent<MeshRenderer>(); var mesh = source.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices; var tris = mesh.triangles;
            if (path == WaterPath)
            {
                var copy = source.GetComponent<HouseholdJarSource>().openBody.vertices;
                report.AppendLine("Derived water geometry max vertex error=" + (copy.Length == vertices.Length ? vertices.Select((v, i) => Vector3.Distance(v, copy[i])).Max().ToString("R") : "different vertex counts"));
            }
            var parent = Enumerable.Range(0, vertices.Length).ToArray();
            var welded = new Dictionary<Vector3Int, int>();
            Func<int, int> find = null; find = n => parent[n] == n ? n : parent[n] = find(parent[n]);
            Action<int, int> join = (a, b) => parent[find(a)] = find(b);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = source.transform.TransformPoint(vertices[i]);
                var key = new Vector3Int(Mathf.RoundToInt(p.x * 100000), Mathf.RoundToInt(p.y * 100000), Mathf.RoundToInt(p.z * 100000));
                if (welded.TryGetValue(key, out int other)) join(i, other); else welded.Add(key, i);
            }
            for (int i = 0; i < tris.Length; i += 3) { join(tris[i], tris[i + 1]); join(tris[i], tris[i + 2]); }
            var groups = Enumerable.Range(0, vertices.Length).GroupBy(find).OrderByDescending(g => g.Count()).ToArray();
            report.AppendLine(path + " mesh=" + mesh.name + " submeshes=" + mesh.subMeshCount + " bounds=" + renderer.bounds + " islands=" + groups.Length);
            foreach (var group in groups.Take(20))
            {
                var points = group.Select(i => source.transform.TransformPoint(vertices[i])).ToArray(); Bounds b = new Bounds(points[0], Vector3.zero);
                foreach (var p in points) b.Encapsulate(p); report.AppendLine("  vertices=" + points.Length + " bounds=" + b.ToString("F4"));
            }
        }
        Directory.CreateDirectory("Tools/ProjectAudit/Simulator"); File.WriteAllText("Tools/ProjectAudit/Simulator/legacy-jar-inspection.txt", report.ToString());
        Debug.Log(report);
    }
}
