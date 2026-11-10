using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2ExistingAssetReview
{
    public const string Output = "Builds/ExistingAssets-20261109";
    [MenuItem("KuTy/Existing assets/Inspect current minigames and available source models")]
    public static void Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != Scene2MotherHomeSetup.Home)
            throw new InvalidOperationException("Saved Home in Edit Mode required; no unsaved changes will be discarded.");
        Directory.CreateDirectory(Output);
        string backup = Output + "/Home-before.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        var log = new StringBuilder();
        var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (var t in transforms)
        {
            var f = t.GetComponent<MeshFilter>();
            var r = t.GetComponent<Renderer>();
            if (!f || !f.sharedMesh || !r) continue;
            log.AppendLine($"SCENE {PathOf(t)} | active={t.gameObject.activeInHierarchy}; pos={t.position}; rot={t.eulerAngles}; scale={t.lossyScale}; bounds={r.bounds}; mesh={AssetDatabase.GetAssetPath(f.sharedMesh)}:{f.sharedMesh.name}; vertices={f.sharedMesh.vertexCount}; submeshes={f.sharedMesh.subMeshCount}; materials={string.Join(",", r.sharedMaterials.Select(m => m ? AssetDatabase.GetAssetPath(m) : "MISSING"))}");
        }
        foreach (string path in new[] {
            "Assets/_Project/Art/Scene2/Models/Kitchen/SM_StovePot.fbx",
            "Assets/_Project/Art/Scene2/Models/Kitchen/SM_RiceCookingStove.fbx",
            "Assets/_Project/Art/Scene2/Models/Kitchen/SM_FirewoodStove_01.fbx",
            "Assets/_Project/Art/Scene2/Models/Kitchen/SM_FirewoodStove_02.fbx",
            "Assets/_Project/Art/Scene2/Models/Kitchen/SM_FirewoodPile.fbx",
            "Assets/_Project/Art/Scene2/Models/Kitchen/SM_DiningTable.fbx" })
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            log.AppendLine("MODEL " + path);
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                var f = t.GetComponent<MeshFilter>();
                log.AppendLine($"  {PathOf(t)} | mesh={(f && f.sharedMesh ? f.sharedMesh.name : "none")}; bounds={(f && f.sharedMesh ? f.sharedMesh.bounds.ToString() : "none")}");
            }
        }
        var kitchen = UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>();
        var serialized = new SerializedObject(kitchen);
        var property = serialized.GetIterator();
        while (property.NextVisible(true))
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue)
                log.AppendLine("KITCHEN " + property.propertyPath + " = " + property.objectReferenceValue.name);
        var simulator = UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>();
        var stations = new SerializedObject(simulator).FindProperty("stations");
        for (int i = 0; i < stations.arraySize; i++)
        {
            var s = stations.GetArrayElementAtIndex(i);
            log.AppendLine("STATION " + s.FindPropertyRelative("chore").enumDisplayNames[s.FindPropertyRelative("chore").enumValueIndex]);
            foreach (string field in new[] { "surface", "view", "existingFixture", "idleDressing" })
            {
                var obj = s.FindPropertyRelative(field).objectReferenceValue;
                log.AppendLine("  " + field + "=" + (obj is Component c ? PathOf(c.transform) : obj ? obj.name : "null"));
            }
        }
        File.WriteAllText(Output + "/inventory.txt", log.ToString());
        Debug.Log("[ExistingAssets] Inventory and immutable Home backup saved. No layout changes.");
    }
    public static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;

    [MenuItem("KuTy/Existing assets/Bind setting table to existing dining furniture")]
    public static void BindTable()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != Scene2MotherHomeSetup.Home)
            throw new InvalidOperationException("Saved Home Edit Mode required.");
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var sim = UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>();
        var pendingLayout = sim.GetComponent<Scene2AuthoredTableLayout>();
        if (pendingLayout && File.Exists(Output + "/table-bind.txt")) throw new InvalidOperationException("Already bound; inspect instead of overwriting.");
        if (scene.isDirty && !pendingLayout) throw new InvalidOperationException("Unrelated unsaved edits; save first.");
        var table = all.Single(t => t.name == "DiningTable").GetComponent<Renderer>();
        var before = all.ToDictionary(t => t, t => (t.localPosition, t.localRotation, t.localScale, t.parent));
        var layout = pendingLayout ? pendingLayout : Undo.AddComponent<Scene2AuthoredTableLayout>(sim.gameObject);
        layout.table = table;
        layout.dishes = new[] { all.Single(t => t.name == "New_Bowl"), all.Single(t => t.name == "New_Plate"), all.Single(t => t.name == "DishBowl") };
        layout.sourceLocal = new[] { new Vector3(-.40f,0,-.38f), new Vector3(0,0,-.38f), new Vector3(.40f,0,-.38f) };
        layout.targetLocal = new[] { new Vector3(-.40f,0,.27f), new Vector3(0,0,.27f), new Vector3(.40f,0,.27f) };
        var serialized = new SerializedObject(sim);
        var stations = serialized.FindProperty("stations");
        var station = Enumerable.Range(0, stations.arraySize).Select(i => stations.GetArrayElementAtIndex(i)).Single(s => s.FindPropertyRelative("chore").intValue == (int)HouseholdChore.SetTable);
        var surface = (Transform)station.FindPropertyRelative("surface").objectReferenceValue;
        var view = (Transform)station.FindPropertyRelative("view").objectReferenceValue;
        Undo.RecordObjects(new UnityEngine.Object[] { surface, view, sim }, "Bind real dining table activity");
        surface.SetPositionAndRotation(new Vector3(table.bounds.center.x, table.bounds.max.y + .003f, table.bounds.center.z), Quaternion.identity);
        view.position = surface.position + new Vector3(0, 1.45f, -1.4f);
        view.rotation = Quaternion.LookRotation(surface.position - view.position);
        station.FindPropertyRelative("existingFixture").objectReferenceValue = table;
        station.FindPropertyRelative("idleDressing").objectReferenceValue = null;
        serialized.ApplyModifiedProperties();
        // Retarget the existing E component; never move the visible model it used to sit on.
        foreach (var target in all.SelectMany(t => t.GetComponents<HouseholdChoreInteractable>()).Where(t => t.Chore == HouseholdChore.SetTable))
        { Undo.RecordObject(target, "Retire former unrelated table target"); target.enabled = false; }
        var newTarget = Undo.AddComponent<HouseholdChoreInteractable>(table.gameObject);
        newTarget.Configure(HouseholdChore.SetTable, table.transform);
        foreach (var old in before)
            if (old.Key != surface && old.Key != view && (old.Key.localPosition != old.Value.Item1 || old.Key.localRotation != old.Value.Item2 || old.Key.localScale != old.Value.Item3 || old.Key.parent != old.Value.Item4))
                throw new InvalidOperationException("Unexpected layout change: " + old.Key.name);
        EditorUtility.SetDirty(layout); Undo.FlushUndoRecordObjects();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Output + "/table-bind.txt", "Uses existing DiningTable and exact New_Bowl/New_Plate/DishBowl meshes/materials. No workstand, primitive chopsticks or generated rice pot. Original poses preserved except two activity-only camera/surface markers.\n");
        Debug.Log("[ExistingAssets] Table minigame bound to authored dining furniture; all visible source props preserved.");
    }

    [MenuItem("KuTy/Existing assets/Inspect original mesh connected parts")]
    public static void Parts()
    {
        var scene = SceneManager.GetActiveScene();
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var report = new StringBuilder();
        foreach (string name in new[] { "StovePotVisual", "beplua_naucom", "FirewoodPile (3)" })
        {
            var t = all.Single(x => x.name == name);
            var mesh = t.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices; var indices = mesh.triangles;
            var groups = ConnectedParts(vertices, indices, t.localToWorldMatrix);
            report.AppendLine(name + " parts=" + groups.Length);
            foreach (var group in groups.Take(40))
            {
                var bounds = new Bounds(t.TransformPoint(vertices[group[0]]), Vector3.zero);
                foreach (int i in group) bounds.Encapsulate(t.TransformPoint(vertices[i]));
                report.AppendLine("  vertices=" + group.Length + "; bounds=" + bounds.ToString("F4"));
            }
            float low = vertices.Min(v => t.TransformPoint(v).y), high = vertices.Max(v => t.TransformPoint(v).y);
            for (int bin = 0; bin < 30; bin++)
            {
                float a = Mathf.Lerp(low, high, bin / 30f), b = Mathf.Lerp(low, high, (bin + 1) / 30f);
                int count = vertices.Count(v => { float y = t.TransformPoint(v).y; return y >= a && y < b; });
                report.AppendLine($"  y={a:F4}..{b:F4}: {count}");
            }
        }
        File.WriteAllText(Output + "/original-parts.txt", report.ToString());
        Debug.Log("[ExistingAssets] Original connected-part survey saved.");
    }
    public static int[][] ConnectedParts(Vector3[] vertices, int[] triangles, Matrix4x4 matrix)
    {
        int[] parents = Enumerable.Range(0, vertices.Length).ToArray();
        int[] sizes = Enumerable.Repeat(1, vertices.Length).ToArray();
        int Find(int n) { while (parents[n] != n) { parents[n] = parents[parents[n]]; n = parents[n]; } return n; }
        void Join(int a, int b) { a = Find(a); b = Find(b); if (a == b) return; if (sizes[a] < sizes[b]) { int s = a; a = b; b = s; } parents[b] = a; sizes[a] += sizes[b]; }
        var welded = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < vertices.Length; i++)
        {
            var p = matrix.MultiplyPoint3x4(vertices[i]);
            var key = new Vector3Int(Mathf.RoundToInt(p.x * 100000), Mathf.RoundToInt(p.y * 100000), Mathf.RoundToInt(p.z * 100000));
            if (welded.TryGetValue(key, out int other)) Join(i, other); else welded.Add(key, i);
        }
        for (int i = 0; i < triangles.Length; i += 3) { Join(triangles[i], triangles[i + 1]); Join(triangles[i], triangles[i + 2]); }
        return Enumerable.Range(0, vertices.Length).GroupBy(Find).Select(g => g.ToArray()).OrderByDescending(g => g.Length).ToArray();
    }

    [MenuItem("KuTy/Existing assets/Render original kitchen model previews")]
    public static void Previews()
    {
        Directory.CreateDirectory(Output);
        foreach (string name in new[] { "SM_StovePot", "SM_RiceCookingStove", "SM_FirewoodStove_01", "SM_FirewoodStove_02", "SM_FirewoodPile", "SM_DiningTable" })
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Scene2/Models/Kitchen/" + name + ".fbx");
            var preview = new PreviewRenderUtility();
            var model = UnityEngine.Object.Instantiate(source);
            try
            {
                preview.AddSingleGO(model);
                if (name == "SM_DiningTable") model.transform.rotation = Quaternion.Euler(90, 0, 0);
                var renderers = model.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                float extent = bounds.extents.magnitude;
                preview.camera.nearClipPlane = extent * .01f;
                preview.camera.farClipPlane = extent * 20;
                preview.camera.fieldOfView = 40;
                preview.camera.transform.position = bounds.center + new Vector3(1.3f, .9f, 1.5f).normalized * extent * 3.2f;
                preview.camera.transform.LookAt(bounds.center);
                preview.lights[0].intensity = 1.4f; preview.lights[0].transform.rotation = Quaternion.Euler(45, -30, 0);
                preview.lights[1].intensity = 1.0f; preview.lights[1].transform.rotation = Quaternion.Euler(25, 140, 0);
                preview.ambientColor = new Color(.55f, .55f, .55f);
                preview.BeginStaticPreview(new Rect(0, 0, 900, 900));
                preview.camera.Render();
                var image = preview.EndStaticPreview();
                File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally { preview.Cleanup(); }
        }
        Debug.Log("[ExistingAssets] Source-only model previews rendered without scene edits.");
    }
}
