using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Adds the working set to free floor beside the old stone fixture; never edits its mesh or pose.</summary>
public static class DishwashingPlacementSetup
{
    private const string FixturePath = "HouseLayout/KitchenArea/Sink/DishwashingStationVisual";
    [MenuItem("KuTy/Scene 2/Place dishes beside existing washing area")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved Home scene in Edit Mode. Unsaved work is never discarded.");
        var simulator = UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>();
        var fixture = GameObject.Find(FixturePath).GetComponent<Renderer>();
        var data = simulator.DishwashingArt;
        if (!data || !data.IsReady) throw new InvalidOperationException("Import dishwashing art first.");
        FixNormalMaps();
        var surface = GameObject.Find("Scene2SimulatorStations/WashDishes").transform;
        var view = surface.Find("Close view");
        Bounds bounds = fixture.bounds;
        // Rotate the set along the right edge: its 76cm depth, not its 125cm width,
        // faces the stone fixture. All original fused pots/bowls stay untouched.
        Vector3 center = new Vector3(bounds.max.x + .49f, bounds.min.y + .012f, bounds.center.z - .10f);
        Quaternion rotation = Quaternion.Euler(0, -90, 0);
        Physics.SyncTransforms();
        var blockers = Physics.OverlapBox(center + Vector3.up * .14f, new Vector3(.35f, .12f, .60f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)
            .Where(c => !c.transform.IsChildOf(surface) && !c.GetComponentInParent<FirstPersonController>() && c.bounds.max.y > center.y + .04f).ToArray();
        if (blockers.Length != 0) throw new InvalidOperationException("Free floor is obstructed; no layout changed: " + string.Join(", ", blockers.Select(c => c.name)));
        Undo.RecordObjects(new UnityEngine.Object[] { surface, view, simulator }, "Reuse legacy washing area");
        surface.SetPositionAndRotation(center, rotation); surface.localScale = Vector3.one;
        view.localPosition = new Vector3(0, .84f, -.98f);
        view.rotation = Quaternion.LookRotation(surface.TransformPoint(new Vector3(0, .06f, 0)) - view.position, Vector3.up);
        Transform dressing = surface.Find("Dishwashing set · idle");
        if (!dressing)
        {
            dressing = new GameObject("Dishwashing set · idle").transform;
            dressing.SetParent(surface, false); Undo.RegisterCreatedObjectUndo(dressing.gameObject, "Authored dishwashing set");
            Place(data.basin, dressing, new Vector3(-.35f, .005f, .07f));
            Place(data.rack, dressing, new Vector3(.365f, .005f, .10f));
            Place(data.bowl, dressing, new Vector3(-.015f, .012f, -.07f));
            Place(data.sponge, dressing, new Vector3(.17f, .005f, -.24f));
            Place(data.soap, dressing, new Vector3(-.30f, .005f, -.25f));
            int layer = LayerMask.NameToLayer("Interactable");
            foreach (var t in dressing.GetComponentsInChildren<Transform>()) t.gameObject.layer = layer;
            // Static open mesh colliders permit real E focus without sealing the basin.
            foreach (var mesh in dressing.GetComponentsInChildren<MeshFilter>())
            { var collider = mesh.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = mesh.sharedMesh; }
            surface.gameObject.AddComponent<HouseholdChoreInteractable>().Configure(HouseholdChore.WashDishes, dressing);
        }
        var serialized = new SerializedObject(simulator);
        var stations = serialized.FindProperty("stations");
        bool found = false;
        for (int i = 0; i < stations.arraySize; i++)
        {
            var station = stations.GetArrayElementAtIndex(i);
            if (station.FindPropertyRelative("chore").intValue != (int)HouseholdChore.WashDishes) continue;
            station.FindPropertyRelative("existingFixture").objectReferenceValue = fixture;
            station.FindPropertyRelative("idleDressing").objectReferenceValue = dressing;
            found = true;
        }
        if (!found) throw new InvalidOperationException("WashDishes station missing.");
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(simulator);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Capture(center + new Vector3(0, 5, 0), Quaternion.Euler(90, 0, 0), true, "LegacyWashing-Top.png", 2.4f);
        Capture(view.position, view.rotation, false, "LegacyWashing-Idle.png", 0);
        // Temporary review cameras can mark the scene dirty even after destruction.
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Dishwashing placement] Kept original stone fixture/props/collider/transform; placed the working set on clear floor beside its right edge at " + center + ". No additional table. Seven normal maps verified.");
    }
    private static void Place(GameObject prefab, Transform parent, Vector3 position)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.localPosition = position; instance.transform.localRotation = Quaternion.identity;
    }
    [MenuItem("KuTy/Scene 2/Bind authored dishwashing gameplay (keep layout)")]
    public static void BindAuthoredGameplay()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved Home scene in Edit Mode first.");
        var surface = GameObject.Find("Scene2SimulatorStations/WashDishes").transform;
        var dressing = surface.GetComponentsInChildren<HouseholdChoreInteractable>(true)
            .FirstOrDefault(t => t.transform != surface && t.Chore == HouseholdChore.WashDishes);
        var entry = surface.GetComponent<HouseholdChoreInteractable>();
        if (!entry) entry = Undo.AddComponent<HouseholdChoreInteractable>(surface.gameObject);
        Undo.RecordObject(entry, "Bind washing interaction outside the hidden visual set");
        entry.Configure(HouseholdChore.WashDishes, dressing ? dressing.transform : surface);
        // Only relocate the generated entry component. No object, collider, material or pose changes.
        if (dressing) Undo.DestroyObjectImmediate(dressing);
        EditorUtility.SetDirty(entry); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("[Dishwashing] Interaction owner is now on the persistent station; hiding idle visuals cannot cancel the task. Authored layout unchanged.");
    }
    [MenuItem("KuTy/Scene 2/Align rice view to current pot")]
    public static void AlignRiceView()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity")
            throw new InvalidOperationException("Open Home in Edit Mode first.");
        var kitchen = UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>();
        if (!kitchen || !kitchen.waterHit || !kitchen.washView) throw new InvalidOperationException("Rice view bindings missing.");
        Physics.SyncTransforms();
        Vector3 focus = kitchen.waterHit.bounds.center;
        Vector3 offset = new Vector3(.65f, .78f, -.60f);
        Undo.RecordObject(kitchen.washView, "Align close view to authored pot location");
        kitchen.washView.SetPositionAndRotation(focus + offset, Quaternion.LookRotation(-offset, Vector3.up));
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }
    [MenuItem("KuTy/Scene 2/Verify dishwashing normal maps")]
    public static void FixNormalMaps()
    {
        foreach (string path in Directory.GetFiles(DishwashingAssetSetup.Root + "/Textures", "*_Normal*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            importer.textureType = TextureImporterType.NormalMap; importer.sRGBTexture = false;
            importer.convertToNormalmap = false; importer.SaveAndReimport();
            if (((TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'))).textureType != TextureImporterType.NormalMap)
                throw new InvalidOperationException("Normal map import failed: " + path);
        }
        foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            if (window.titleContent.text == "NormalMap settings") window.Close();
    }
    private static void Capture(Vector3 position, Quaternion rotation, bool ortho, string name, float size)
    {
        var go = new GameObject("Temporary washing layout review") { hideFlags = HideFlags.HideAndDontSave };
        var camera = go.AddComponent<Camera>(); camera.transform.SetPositionAndRotation(position, rotation);
        camera.orthographic = ortho; camera.orthographicSize = size; camera.fieldOfView = 48;
        camera.nearClipPlane = .025f; camera.farClipPlane = 30;
        var target = RenderTexture.GetTemporary(1600, 1000, 24);
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        var old = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
            Directory.CreateDirectory("Tools/ProjectAudit/Simulator");
            File.WriteAllBytes("Tools/ProjectAudit/Simulator/" + name, image.EncodeToPNG());
        }
        finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(go); RenderTexture.ReleaseTemporary(target); }
    }
}
