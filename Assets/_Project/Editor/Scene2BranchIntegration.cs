using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Selective dat-dev room integration; never runs placement/install helpers.</summary>
public static class Scene2BranchIntegration
{
    private const string Home = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string Output = "Builds/Integration-20261109";
    private const string Source = "Assets/__KuTyDatIntegrationSource.unity";
    [Serializable] private sealed class Pose
    {
        public string id, name, parent;
        public Vector3 position, scale;
        public Quaternion rotation;
        public bool active;
    }
    [Serializable] private sealed class Poses { public Pose[] items; }
    private static Scene RequireHome()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != Home || scene.isDirty)
            throw new InvalidOperationException("Saved Home in Edit Mode required; unsaved changes will not be discarded.");
        return scene;
    }
    private static Transform[] Transforms(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
    private static string Id(UnityEngine.Object obj) => obj ? GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString() : "";
    private static Pose Snapshot(Transform t) => new Pose {
        id = Id(t), name = t.name, parent = Id(t.parent), position = t.localPosition,
        rotation = t.localRotation, scale = t.localScale, active = t.gameObject.activeSelf };
    [MenuItem("KuTy/Integration/Capture saved local layout")]
    public static void Capture()
    {
        var scene = RequireHome();
        Directory.CreateDirectory(Output);
        string path = Output + "/editor-layout-before.json";
        if (File.Exists(path)) throw new InvalidOperationException("Baseline already exists; refusing overwrite.");
        File.WriteAllText(path, JsonUtility.ToJson(new Poses { items = Transforms(scene).Select(Snapshot).ToArray() }, true));
        Debug.Log("[BranchIntegration] Saved immutable live-Editor layout baseline.");
    }
    [MenuItem("KuTy/Integration/Apply dat living room only")]
    public static void ApplyRoom()
    {
        var scene = RequireHome();
        if (Transforms(scene).Any(t => t.name == "BoBanGhe_TiepKhach"))
            throw new InvalidOperationException("Room already integrated; inspect instead of recreating.");
        if (!File.Exists(Output + "/editor-layout-before.json") || !File.Exists(Output + "/dat.unity"))
            throw new InvalidOperationException("Capture baseline and export reviewed incoming scene first.");
        if (File.Exists(Source)) throw new InvalidOperationException("Temporary source path occupied.");
        Scene incoming = default;
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Integrate dat living room, preserve local kitchen");
        var log = new StringBuilder();
        try
        {
            File.Copy(Output + "/dat.unity", Source);
            AssetDatabase.ImportAsset(Source, ImportAssetOptions.ForceSynchronousImport);
            incoming = EditorSceneManager.OpenScene(Source, OpenSceneMode.Additive);
            var dat = Transforms(incoming);
            var local = Transforms(scene);
            var room = dat.Single(t => t.name == "BoBanGhe_TiepKhach");
            var copy = UnityEngine.Object.Instantiate(room.gameObject);
            copy.name = room.name;
            SceneManager.MoveGameObjectToScene(copy, scene);
            Undo.RegisterCreatedObjectUndo(copy, "Import committed living room");
            log.AppendLine("Copied BoBanGhe_TiepKhach from b64c1c5, including committed model/material/pose overrides.");
            foreach (string name in new[] { "DiningTable", "DiningChair_02", "DiningChair_02 (1)", "DiningChair_02 (2)" })
            {
                var from = dat.Single(t => t.name == name);
                var to = local.SingleOrDefault(t => t.name == name);
                if (!to)
                {
                    var added = UnityEngine.Object.Instantiate(from.gameObject);
                    added.name = name;
                    SceneManager.MoveGameObjectToScene(added, scene);
                    // Instantiate without a parent preserves the committed world pose.
                    Undo.RegisterCreatedObjectUndo(added, "Import committed dining chair");
                    log.AppendLine("Added " + name);
                }
                else
                {
                    Undo.RecordObject(to, "Use committed dining furniture pose");
                    to.SetPositionAndRotation(from.position, from.rotation);
                    var parentScale = to.parent ? to.parent.lossyScale : Vector3.one;
                    var worldScale = from.lossyScale;
                    to.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, worldScale.z / parentScale.z);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(to);
                    log.AppendLine("Updated committed world pose: " + name);
                }
            }
            // Preserve old objects/references as inactive overrides instead of deleting prefab children.
            foreach (string name in new[] { "DiningChair_01", "DiningChair_03", "DiningChair_04" })
            {
                var old = local.Single(t => t.name == name);
                Undo.RecordObject(old.gameObject, "Retire dining chair replaced by dat");
                old.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);
                log.AppendLine("Disabled replaced chair (original preserved): " + name);
            }
            EditorSceneManager.CloseScene(incoming, true);
            incoming = default;
            SceneManager.SetActiveScene(scene);
            Undo.FlushUndoRecordObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Output + "/room-integration.txt", log.ToString());
            Debug.Log("[BranchIntegration] " + log);
        }
        catch { Undo.RevertAllDownToGroup(undo); throw; }
        finally
        {
            if (incoming.IsValid() && incoming.isLoaded) EditorSceneManager.CloseScene(incoming, true);
            if (File.Exists(Source)) AssetDatabase.DeleteAsset(Source);
            SceneManager.SetActiveScene(scene);
        }
        Verify();
    }
    [MenuItem("KuTy/Integration/Verify preserved layout and Scene 2 assets")]
    public static void Verify()
    {
        var scene = RequireHome();
        var original = JsonUtility.FromJson<Poses>(File.ReadAllText(Output + "/editor-layout-before.json"));
        var current = Transforms(scene).ToDictionary(Id, t => t);
        var allowedPose = new HashSet<string> { "DiningTable", "DiningChair_02" };
        var allowedInactive = new HashSet<string> { "DiningChair_01", "DiningChair_03", "DiningChair_04" };
        int preserved = 0, issues = 0;
        var log = new StringBuilder();
        foreach (var before in original.items)
        {
            if (!current.TryGetValue(before.id, out var t)) { log.AppendLine("FAIL missing original transform " + before.name); issues++; continue; }
            var after = Snapshot(t);
            bool same = before.position == after.position && before.rotation == after.rotation && before.scale == after.scale && before.parent == after.parent;
            if (!same && !allowedPose.Contains(before.name)) { log.AppendLine("FAIL unexpected pose/parent change " + before.name); issues++; }
            else if (same) preserved++;
            if (before.active != after.active && !allowedInactive.Contains(before.name)) { log.AppendLine("FAIL unexpected active change " + before.name); issues++; }
        }
        foreach (var t in current.Values)
        {
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            if (missing > 0) { issues += missing; log.AppendLine("FAIL missing script " + t.name); }
            foreach (var component in t.GetComponents<Component>().Where(c => c))
            using (var serialized = new SerializedObject(component))
            {
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && !property.objectReferenceValue && property.objectReferenceInstanceIDValue != 0)
                    { issues++; log.AppendLine("FAIL missing reference " + t.name + "." + property.propertyPath); }
            }
            foreach (var renderer in t.GetComponents<Renderer>())
            foreach (var material in renderer.sharedMaterials)
                if (!material || !material.shader || !material.shader.isSupported)
                { issues++; log.AppendLine("FAIL missing/unsupported material " + t.name); }
        }
        var room = current.Values.SingleOrDefault(t => t.name == "BoBanGhe_TiepKhach");
        if (room)
        {
            foreach (var mesh in room.GetComponentsInChildren<MeshFilter>(true))
                log.AppendLine($"Furniture {mesh.name}: mesh={mesh.sharedMesh != null}, colliders={mesh.GetComponents<Collider>().Length}, bounds={mesh.GetComponent<Renderer>()?.bounds}");
        }
        log.AppendLine($"Original transforms={original.items.Length}; preserved poses={preserved}; deliberate dining-pose overrides={original.items.Length - preserved}; current={current.Count}; issues={issues}");
        File.WriteAllText(Output + "/scene2-layout-and-assets.txt", log.ToString());
        if (issues != 0) throw new InvalidOperationException(log.ToString());
        Debug.Log("[BranchIntegration] " + log);
    }

    [MenuItem("KuTy/Integration/Add missing living room collision")]
    public static void AddRoomCollision()
    {
        var scene = RequireHome();
        var room = Transforms(scene).Single(t => t.name == "BoBanGhe_TiepKhach");
        int added = 0;
        foreach (var filter in room.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!filter.sharedMesh || filter.GetComponent<Collider>()) continue;
            var collider = Undo.AddComponent<MeshCollider>(filter.gameObject);
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            added++;
        }
        Undo.FlushUndoRecordObjects();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Output + "/room-collision.txt", "Added static mesh colliders=" + added + "; no furniture transforms changed.\n");
        Verify();
    }

    [MenuItem("KuTy/Integration/Capture room and kitchen review images")]
    public static void CaptureImages()
    {
        var scene = RequireHome();
        var host = new GameObject("Integration review camera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = host.AddComponent<Camera>();
        camera.fieldOfView = 65; camera.nearClipPlane = .05f; camera.farClipPlane = 90;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.15f, .17f, .18f);
        var rt = new RenderTexture(1600, 1000, 24);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = rt;
            var all = Transforms(scene);
            var room = all.Single(t => t.name == "BoBanGhe_TiepKhach");
            var bounds = new Bounds(room.position, Vector3.zero);
            foreach (var renderer in room.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            Render("living-room.png", bounds.center + new Vector3(-4.5f, 3.2f, 4.5f), bounds.center);
            var sink = all.Single(t => t.name == "New_WashStation");
            var sinkBounds = new Bounds(sink.position, Vector3.zero);
            foreach (var renderer in sink.GetComponentsInChildren<Renderer>()) sinkBounds.Encapsulate(renderer.bounds);
            Render("local-dishwashing.png", sinkBounds.center + new Vector3(1.7f, 1.8f, -2.4f), sinkBounds.center);
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(host);
        }
        void Render(string file, Vector3 position, Vector3 look)
        {
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(look - position));
            camera.Render(); RenderTexture.active = rt;
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            try { image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply(); File.WriteAllBytes(Output + "/" + file, image.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
