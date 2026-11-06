using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Scoped scene-instance furniture correction. Never scales the house, player or mother.</summary>
public static class Scene2FurnitureScaleSetup
{
    private const string Home = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string Reports = "Tools/Scene2Scale";
    private const float Floor = .02f;
    private static readonly string[] Paths = {
        "HouseLayout/TraditionalHouseInterior/tripo_part_3",
        "HouseLayout/KitchenArea/Refrigerator",
        "HouseLayout/KitchenArea/DishCabinet/DishCabinetVisual",
        "HouseLayout/YardProps/BambooTable"
    };
    private static readonly float[] Heights = { .76f, 1.70f, 1.80f, .76f };

    private sealed class Item
    {
        public Transform root;
        public Renderer visual;
        public Bounds before;
        public float height, factor;
        public readonly List<Transform> supported = new List<Transform>();
    }

    private static Scene RequireHome()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != Home) throw new InvalidOperationException("Open the home scene first.");
        return scene;
    }

    private static List<Item> Plan()
    {
        var scene = RequireHome();
        var items = new List<Item>();
        for (int i = 0; i < Paths.Length; i++)
        {
            var target = GameObject.Find(Paths[i]);
            var renderer = target ? target.GetComponent<MeshRenderer>() : null;
            if (!renderer || !renderer.enabled || !target.GetComponent<MeshFilter>()?.sharedMesh)
                throw new InvalidOperationException("Missing visible furniture: " + Paths[i]);
            var bounds = renderer.bounds;
            if (bounds.size.y < .2f || bounds.size.y > 4f) throw new InvalidOperationException("Unexpected furniture dimensions: " + target.name);
            items.Add(new Item { root = target.transform, visual = renderer, before = bounds,
                height = Heights[i], factor = Heights[i] / bounds.size.y });
        }
        // Some decorations are authored as siblings, not children of their supporting furniture.
        foreach (var renderer in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (!renderer.enabled || renderer.gameObject.scene != scene ||
                items.Any(i => renderer.transform.IsChildOf(i.root) || i.root.IsChildOf(renderer.transform))) continue;
            Bounds b = renderer.bounds;
            foreach (var item in items)
            {
                var top = item.before;
                if (Mathf.Abs(b.min.y - top.max.y) > .12f || b.size.y > 1f ||
                    b.size.x > top.size.x || b.size.z > top.size.z ||
                    b.center.x < top.min.x || b.center.x > top.max.x ||
                    b.center.z < top.min.z || b.center.z > top.max.z) continue;
                item.supported.Add(renderer.transform);
                break;
            }
        }
        return items;
    }

    [MenuItem("KuTy/Scene 2/Audit realistic furniture scale")]
    public static void Audit()
    {
        var items = Plan();
        var report = new StringBuilder("Scene 2 furniture scale plan (metres)\n");
        report.AppendLine("Original scene dirty=" + SceneManager.GetActiveScene().isDirty);
        foreach (var item in items)
            report.AppendLine($"{item.root.name}: height {item.before.size.y:F4} -> {item.height:F2}; uniform multiplier {item.factor:F5}; floor {item.before.min.y:F4} -> {Floor:F2}; supported=[{string.Join(", ", item.supported.Select(t => t.name))}]");
        report.AppendLine("Mother, player, house structure, kitchen tasks/cameras, source prefabs and quest data are excluded.");
        Directory.CreateDirectory(Reports);
        File.WriteAllText(Reports + "/plan.txt", report.ToString());
        Debug.Log(report.ToString());
    }

    [MenuItem("KuTy/Scene 2/Apply realistic furniture scale (keep scene unsaved)")]
    public static void Apply()
    {
        var items = Plan();
        var scene = SceneManager.GetActiveScene();
        Directory.CreateDirectory("Assets/_Project/Scenes/Backups");
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string beforeCopy = "Assets/_Project/Scenes/Backups/FurnitureScale_Before_" + stamp + ".unity";
        if (!EditorSceneManager.SaveScene(scene, beforeCopy, true)) throw new IOException("Cannot back up the live scene.");
        // A copy is not permission to save the user's unrelated dirty edits into the original file.
        EditorSceneManager.MarkSceneDirty(scene);
        var protectedTransforms = ProtectedTransforms();
        var poses = protectedTransforms.Select(t => new Pose(t.position, t.rotation)).ToArray();
        var scales = protectedTransforms.Select(t => t.localScale).ToArray();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Scene 2 realistic furniture proportions");
        try
        {
            foreach (var item in items)
            {
                Undo.RecordObject(item.root, "Resize " + item.root.name);
                item.root.localScale *= item.factor;
                Bounds scaled = item.visual.bounds;
                Vector3 correction = item.before.center - scaled.center;
                correction.y = Floor - scaled.min.y;
                item.root.position += correction;
                Bounds after = item.visual.bounds;
                foreach (var support in item.supported)
                {
                    Undo.RecordObject(support, "Keep decoration on " + item.root.name);
                    Bounds b = support.GetComponent<Renderer>().bounds;
                    Vector3 center = b.center;
                    Vector3 desired = after.center + (center - item.before.center) * item.factor;
                    desired.y = center.y + after.max.y - item.before.max.y;
                    support.position += desired - center;
                }
                // MeshCollider follows the same transform; box solids use local mesh bounds.
                var box = item.root.GetComponent<BoxCollider>();
                if (box && box.enabled && !box.isTrigger)
                {
                    Undo.RecordObject(box, "Fit solid furniture collider");
                    var local = item.root.GetComponent<MeshFilter>().sharedMesh.bounds;
                    box.center = local.center;
                    box.size = local.size;
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.root);
            }
            Physics.SyncTransforms();
            for (int i = 0; i < protectedTransforms.Length; i++)
                if ((protectedTransforms[i].position - poses[i].position).sqrMagnitude > .000001f ||
                    Quaternion.Angle(protectedTransforms[i].rotation, poses[i].rotation) > .001f ||
                    (protectedTransforms[i].localScale - scales[i]).sqrMagnitude > .000001f)
                    throw new InvalidOperationException("Protected actor/house/task transform changed: " + protectedTransforms[i].name);
            Validate();
            string afterCopy = "Assets/_Project/Scenes/Backups/FurnitureScale_After_" + stamp + ".unity";
            if (!EditorSceneManager.SaveScene(scene, afterCopy, true)) throw new IOException("Cannot back up corrected scene.");
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            File.WriteAllText(Reports + "/applied.txt", "PASS\nLive scene corrected with one Undo group. Original home file NOT saved.\nBefore=" + beforeCopy + "\nAfter=" + afterCopy + "\nProtected transforms unchanged=" + protectedTransforms.Length + "\n");
            Debug.Log("Furniture scale corrected. Scene intentionally remains unsaved; live before/after copies are in Scenes/Backups.");
        }
        catch
        {
            Undo.RevertAllDownToGroup(group);
            EditorSceneManager.MarkSceneDirty(scene);
            throw;
        }
    }

    private static Transform[] ProtectedTransforms()
    {
        string[] roots = { "Scene2MotherDemo", "Player", "Scene2KitchenHandsOnWorld", "Scene2RadioKitchenWorld", "Act2_RusticWalls", "KitchenStructureAnchor", "WindowVillageBackdrop" };
        return roots.Select(GameObject.Find).Where(g => g).SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
    }

    [MenuItem("KuTy/Scene 2/Validate realistic furniture scale")]
    public static void Validate()
    {
        RequireHome();
        Physics.SyncTransforms();
        var report = new StringBuilder("Furniture scale validation\n");
        for (int i = 0; i < Paths.Length; i++)
        {
            var target = GameObject.Find(Paths[i]);
            Bounds b = target.GetComponent<Renderer>().bounds;
            if (Mathf.Abs(b.size.y - Heights[i]) > .005f || Mathf.Abs(b.min.y - Floor) > .005f)
                throw new InvalidOperationException("Unexpected height/floor: " + target.name + " " + b);
            var collider = target.GetComponent<Collider>();
            if (!collider || !collider.enabled || collider.isTrigger)
                throw new InvalidOperationException("Furniture needs its solid collider: " + target.name);
            if ((collider.bounds.center - b.center).magnitude > .03f || (collider.bounds.size - b.size).magnitude > .06f)
                throw new InvalidOperationException("Solid collider does not match visual: " + target.name);
            report.AppendLine($"PASS {target.name}: size={b.size}, floor={b.min.y:F3}, top={b.max.y:F3}, collider aligned");
            if ((i == 0 || i == 2) && !CanReach(target.transform, b))
                throw new InvalidOperationException("No clear child-height approach/raycast to chore furniture: " + target.name);
            if (i == 0 || i == 2) report.AppendLine("PASS " + target.name + ": child-height approach, capsule clearance and first-hit raycast within 3m");
        }
        var cabinet = GameObject.Find(Paths[2]);
        if (!cabinet.GetComponent<HouseholdChoreInteractable>() ||
            cabinet.GetComponent<HouseholdChoreInteractable>().Chore != HouseholdChore.PutAwayDishes)
            throw new InvalidOperationException("Dish storage binding changed.");
        if (GameObject.Find(Paths[0]).GetComponent<MeshCollider>().sharedMesh.name != "tripo_part_3 Instance")
            throw new InvalidOperationException("Dining mesh binding changed.");
        report.AppendLine("PASS dining mesh and dish storage chore bindings preserved");
        Directory.CreateDirectory(Reports);
        File.WriteAllText(Reports + "/validation.txt", report.ToString());
        Debug.Log(report.ToString());
    }

    private static bool CanReach(Transform target, Bounds b)
    {
        foreach (float gap in new[] { .45f, .7f, 1f })
        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.PI / 12;
            var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            float extent = Mathf.Abs(direction.x) * b.extents.x + Mathf.Abs(direction.z) * b.extents.z;
            Vector3 feet = b.center + direction * (extent + gap);
            feet.y = Floor + .04f;
            var overlaps = Physics.OverlapCapsule(feet + Vector3.up * .28f, feet + Vector3.up * 1.02f, .28f, ~0, QueryTriggerInteraction.Ignore);
            if (overlaps.Any(c => c.bounds.max.y > Floor + .08f)) continue;
            Vector3 eye = feet + Vector3.up * 1.2f;
            Vector3 focus = b.center;
            focus.y = Mathf.Min(b.max.y - .025f, 1.1f);
            if (Physics.Raycast(eye, (focus - eye).normalized, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) &&
                hit.transform.IsChildOf(target)) return true;
        }
        return false;
    }
}
