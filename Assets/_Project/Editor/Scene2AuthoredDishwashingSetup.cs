using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2AuthoredDishwashingSetup
{
    public const string Output = "Tools/DishwashingSimulator";
    private static Transform Require(string name)
    {
        var matches = UnityEngine.Object.FindObjectsOfType<Transform>(true).Where(t => t.gameObject.scene == SceneManager.GetActiveScene() && t.name == name).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException("Expected one scene object: " + name + ", got " + matches.Length);
        return matches[0];
    }
    [MenuItem("KuTy/Scene 2/Bind simulator to user-arranged dishes (keep all poses)")]
    public static void Bind()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity" || scene.isDirty)
            throw new InvalidOperationException("Save Home in Edit Mode first. Unsaved work will not be discarded.");
        var simulator = UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>();
        if (!simulator || simulator.GetComponent<Scene2DishwashingLayout>())
            throw new InvalidOperationException("Simulator missing or already bound; refusing to overwrite authoring.");
        Transform fixture = Require("New_WashStation"), faucet = Require("New_Faucet");
        var sources = new[] { Require("New_Bowl"), Require("New_Plate"), Require("DishBowl") };
        var basin = Require("WashBasin"); var sponge = Require("ScrubSponge"); var soap = Require("SoapBar"); var rack = Require("DryingRack");
        // Validate probes before writing scene bindings.
        var dishes = sources.Select((t, i) => Measure(t, i == 1 ? "Dĩa hoa lam" : i == 0 ? "Chén hoa sen" : "Chén nhỏ")).ToArray();
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Bind authored dishwashing simulator");
        var layout = Undo.AddComponent<Scene2DishwashingLayout>(simulator.gameObject);
        layout.fixture = fixture; layout.basin = basin; layout.sponge = sponge; layout.soap = soap; layout.rack = rack; layout.faucet = faucet;
        layout.dishes = dishes;
        var outlet = new GameObject("Dishwashing_WaterOutlet").transform;
        Undo.RegisterCreatedObjectUndo(outlet.gameObject, "Tap outlet marker");
        outlet.SetParent(faucet, false); outlet.position = FindOutlet(faucet, basin);
        layout.outlet = outlet;
        var entry = fixture.GetComponent<HouseholdChoreInteractable>();
        if (!entry) entry = Undo.AddComponent<HouseholdChoreInteractable>(fixture.gameObject);
        Undo.RecordObject(entry, "Washing entry"); entry.Configure(HouseholdChore.WashDishes, fixture);
        foreach (var mesh in fixture.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mesh.GetComponent<Collider>())
            {
                var solid = Undo.AddComponent<MeshCollider>(mesh.gameObject); solid.sharedMesh = mesh.sharedMesh;
            }
            Undo.RecordObject(mesh.gameObject, "Washing interaction layer"); mesh.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }
        // Remove duplicate E owners by disabling components only. Their objects and local work stay intact.
        foreach (var old in UnityEngine.Object.FindObjectsOfType<HouseholdChoreInteractable>(true))
            if (old != entry && old.Chore == HouseholdChore.WashDishes)
            { Undo.RecordObject(old, "Use the arranged washing area"); old.enabled = false; EditorUtility.SetDirty(old); }
        Undo.RecordObject(simulator, "Bind fixture without moving station roots");
        var serialized = new SerializedObject(simulator);
        var stations = serialized.FindProperty("stations");
        for (int i = 0; i < stations.arraySize; i++)
        {
            var station = stations.GetArrayElementAtIndex(i);
            if (station.FindPropertyRelative("chore").intValue != (int)HouseholdChore.WashDishes) continue;
            station.FindPropertyRelative("existingFixture").objectReferenceValue = fixture.GetComponent<Renderer>();
            // Props now span multiple hierarchies. Runtime hides individual source models, never the whole dressing group.
            station.FindPropertyRelative("idleDressing").objectReferenceValue = null;
        }
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(layout); EditorUtility.SetDirty(entry); EditorUtility.SetDirty(simulator);
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Output);
        var report = new StringBuilder("Bound authored dishwashing; no original position/rotation/scale changed.\n");
        foreach (var t in new[] { fixture, faucet, basin, sponge, soap, rack }.Concat(sources))
            report.AppendLine(t.name + " world=" + t.position.ToString("F5") + " bounds=" + Scene2DishwashingLayout.VisualBounds(t));
        report.AppendLine("Outlet=" + outlet.position.ToString("F5") + "; marker is editable under New_Faucet.");
        foreach (var dish in dishes)
            for (int i = 0; i < 6; i++) report.AppendLine(dish.label + " patch " + i + " world=" + dish.source.TransformPoint(dish.points[i]).ToString("F5") + " normal=" + dish.source.TransformDirection(dish.normals[i]).ToString("F3"));
        File.WriteAllText(Output + "/binding-report.txt", report.ToString());
        Debug.Log(report.ToString());
    }
    private static Scene2DishwashingLayout.Dish Measure(Transform source, string label)
    {
        var dish = new Scene2DishwashingLayout.Dish { source = source, label = label };
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var clone = UnityEngine.Object.Instantiate(source.gameObject);
            SceneManager.MoveGameObjectToScene(clone, preview);
            clone.transform.SetPositionAndRotation(source.position, source.rotation);
            clone.transform.localScale = source.lossyScale;
            var queries = clone.GetComponentsInChildren<MeshFilter>(true).Select(m =>
            { var q = m.gameObject.AddComponent<MeshCollider>(); q.sharedMesh = m.sharedMesh; return q; }).ToArray();
            Physics.SyncTransforms();
            var b = Scene2DishwashingLayout.VisualBounds(clone.transform);
            for (int side = 0; side < 2; side++) for (int i = 0; i < 3; i++)
            {
                float angle = (i + .25f) * Mathf.PI * 2 / 3;
                Vector3 p = b.center + new Vector3(Mathf.Cos(angle) * b.extents.x * .36f, 0, Mathf.Sin(angle) * b.extents.z * .36f);
                p.y = side == 0 ? b.max.y + .1f : b.min.y - .1f;
                var ray = new Ray(p, side == 0 ? Vector3.down : Vector3.up);
                float distance = 2f; bool found = false; RaycastHit hit = default;
                foreach (var query in queries)
                    if (query.Raycast(ray, out var candidate, distance)) { hit = candidate; distance = hit.distance; found = true; }
                if (!found) throw new InvalidOperationException("Missing surface probe " + label + " / " + side + " / " + i);
                int patch = side * 3 + i;
                dish.points[patch] = source.InverseTransformPoint(hit.point);
                dish.normals[patch] = source.InverseTransformDirection(hit.normal).normalized;
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        return dish;
    }
    private static Vector3 FindOutlet(Transform faucet, Transform basin)
    {
        var b = Scene2DishwashingLayout.VisualBounds(faucet);
        Vector3 forward = Scene2DishwashingLayout.VisualBounds(basin).center - b.center; forward.y = 0; forward.Normalize();
        var vertices = faucet.GetComponentsInChildren<MeshFilter>(true)
            .SelectMany(m => m.sharedMesh.vertices.Select(v => m.transform.TransformPoint(v))).ToArray();
        float maximum = vertices.Max(v => Vector3.Dot(v - b.center, forward));
        // Nozzle is the bottom of the forward end, not the low edge of the wall flange.
        var front = vertices.Where(v => Vector3.Dot(v - b.center, forward) >= maximum * .72f).ToArray();
        float bottom = front.Min(v => v.y);
        var lip = front.Where(v => v.y < bottom + b.size.y * .035f).ToArray();
        Vector3 center = lip.Aggregate(Vector3.zero, (sum, v) => sum + v) / lip.Length;
        return center + Vector3.down * .002f;
    }
}
