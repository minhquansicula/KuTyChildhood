using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2FourActivitiesSetup
{
    public const string Output = "Tools/Scene2FourActivities";
    [MenuItem("KuTy/Scene 2/Bind supported lid rest (keep kitchen layout)")]
    public static void BindLidRest()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity")
            throw new InvalidOperationException("Open Home in Edit Mode first.");
        var kitchen = UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>();
        if (!kitchen || !kitchen.lid || kitchen.lidRestPoint)
            throw new InvalidOperationException("Kitchen/lid missing or a rest point already exists; will not overwrite it.");
        var bounds = Scene2DishwashingLayout.VisualBounds(kitchen.lid);
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        Physics.SyncTransforms();
        RaycastHit support = default; Vector3 center = default; bool found = false;
        foreach (float distance in new[] { .65f, .85f, 1.05f })
        {
            foreach (Vector3 direction in new[] { Vector3.right, Vector3.back, Vector3.left, Vector3.forward })
            {
                Vector3 candidate = bounds.center + direction * (distance + radius * .25f);
                var hits = Physics.RaycastAll(new Vector3(candidate.x, bounds.max.y + .3f, candidate.z), Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => h.normal.y > .95f && h.point.y < bounds.min.y - .25f &&
                        !h.transform.IsChildOf(kitchen.worldRoot) && !h.collider.GetComponentInParent<FirstPersonController>())
                    .OrderBy(h => h.distance).ToArray();
                foreach (var hit in hits)
                {
                    Vector3 check = new Vector3(candidate.x, hit.point.y + .04f, candidate.z);
                    var blockers = Physics.OverlapBox(check, new Vector3(radius + .012f, .022f, radius + .012f), Quaternion.identity,
                        ~0, QueryTriggerInteraction.Ignore).Where(c => c != hit.collider).ToArray();
                    if (blockers.Length > 0) continue;
                    bool fullSupport = true;
                    foreach (Vector3 offset in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                    {
                        Vector3 p = hit.point + offset * radius * .9f;
                        if (!hit.collider.Raycast(new Ray(p + Vector3.up * .08f, Vector3.down), out var edge, .15f) ||
                            Mathf.Abs(edge.point.y - hit.point.y) > .005f) { fullSupport = false; break; }
                    }
                    if (!fullSupport) continue;
                    support = hit; center = new Vector3(candidate.x, hit.point.y, candidate.z); found = true; break;
                }
                if (found) break;
            }
            if (found) break;
        }
        if (!found) throw new InvalidOperationException("No clear, fully supported lid-sized space found beside the stove. No scene changes made.");
        var rest = new GameObject("CookingLid_SupportedRest").transform;
        Undo.RegisterCreatedObjectUndo(rest.gameObject, "Supported lid rest");
        rest.SetParent(kitchen.cookRoot, false);
        rest.rotation = kitchen.lid.rotation;
        rest.position = center + Vector3.up * .003f - new Vector3(bounds.center.x - kitchen.lid.position.x,
            bounds.min.y - kitchen.lid.position.y, bounds.center.z - kitchen.lid.position.z);
        Undo.RecordObject(kitchen, "Bind supported cooking lid");
        kitchen.lidRestPoint = rest; EditorUtility.SetDirty(kitchen);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Output);
        var report = new StringBuilder("Four retained physical minigames; all other chores use deliberate UI confirmation.\n");
        report.AppendLine("Kept: LightFire (place wood), CookRice, WashDishes, SetTable. Story order/IDs/reward counts/checkpoint format unchanged.");
        report.AppendLine("Closed lid: " + kitchen.lid.position.ToString("F6") + " visual bounds=" + bounds);
        report.AppendLine("Rest marker: " + rest.position.ToString("F6") + " rotation=" + rest.eulerAngles.ToString("F3"));
        report.AppendLine("Support collider: " + support.collider.name + " at " + support.point.ToString("F6"));
        report.AppendLine("Lid-bottom rest gap: 0.003m; full footprint supported, surrounding solid overlap checked.");
        report.AppendLine("Only a marker and a reference were added; original pot, lid rest pose in Edit Mode, stoves and props were not moved.");
        File.WriteAllText(Output + "/binding-report.txt", report.ToString());
        Debug.Log(report.ToString());
    }
}
