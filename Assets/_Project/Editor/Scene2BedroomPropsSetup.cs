using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene 2 only: inspect and install the three bedroom FBX props.</summary>
public static class Scene2BedroomPropsSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string ModelFolder = "Assets/_Project/Models/";

    [MenuItem("KuTy/Scene 2/Preview bedroom props unobstructed")]
    private static void PreviewUnobstructed()
    {
        if (!Ready()) return;
        var roof = GameObject.Find("HouseLayout/MaiNha");
        var shell = GameObject.Find("BedroomArea/RusticBedroom_Visual");
        if (roof != null) SceneVisibilityManager.instance.Hide(roof, true);
        if (shell != null) SceneVisibilityManager.instance.Hide(shell, false);
    }

    [MenuItem("KuTy/Scene 2/Restore bedroom view")]
    private static void RestoreView() => SceneVisibilityManager.instance.ShowAll();

    [MenuItem("KuTy/Scene 2/Install new bedroom props")]
    private static void Install()
    {
        if (!Ready()) return;
        var bedroom = GameObject.Find("BedroomArea");
        var bed = GameObject.Find("BedroomArea/RusticBedroom_Visual/BedBlock");
        if (bedroom == null || bed == null) throw new InvalidOperationException("Scene 2 bedroom or bed is missing.");

        var root = bedroom.transform.Find("BedroomProps_New");
        if (root == null)
        {
            var holder = new GameObject("BedroomProps_New");
            Undo.RegisterCreatedObjectUndo(holder, "Create bedroom props root");
            holder.transform.SetParent(bedroom.transform, false);
            root = holder.transform;
        }

        var bedPosition = bed.transform.position;
        var cabinet = InstallVisual("tudocanhan", "TuDoCaNhan", root, 1.5f,
            Quaternion.Euler(0f, 90f, 0f), new Vector3(bedPosition.x - 2.6f, 0f, bedPosition.z - .6f), true);
        var cabinetBounds = BoundsOf(cabinet);
        InstallVisual("tivi", "Tivi", root, 1.05f,
            Quaternion.Euler(0f, 90f, 0f),
            new Vector3(cabinetBounds.center.x, cabinetBounds.max.y + .025f, cabinetBounds.center.z), true);
        InstallVisual("tranh", "Tranh", root, 1f,
            Quaternion.Euler(0f, -90f, 0f),
            new Vector3(bedPosition.x + 1.6f, 2.05f, bedPosition.z + .4f), false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("SCENE2_PROPS_INSTALLED TuDoCaNhan, Tivi, Tranh beside BedBlock.");
    }

    private static GameObject InstallVisual(string assetName, string objectName, Transform parent,
        float scale, Quaternion rotation, Vector3 target, bool alignBottom)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + assetName + ".fbx");
        if (source == null) throw new InvalidOperationException("Missing imported FBX: " + assetName);
        var existing = parent.Find(objectName);
        var visual = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        if (visual == null) throw new InvalidOperationException("Cannot instantiate FBX: " + assetName);
        if (existing == null) Undo.RegisterCreatedObjectUndo(visual, "Add " + objectName);
        visual.name = objectName;
        Undo.RecordObject(visual.transform, "Position " + objectName);
        visual.transform.SetPositionAndRotation(Vector3.zero, rotation);
        // FBX roots carry the importer's file-unit scale (often 100); preserve it.
        visual.transform.localScale = source.transform.localScale * scale;
        var bounds = BoundsOf(visual);
        visual.transform.position += alignBottom
            ? new Vector3(target.x - bounds.center.x, target.y - bounds.min.y, target.z - bounds.center.z)
            : target - bounds.center;
        return visual;
    }

    [MenuItem("KuTy/Scene 2/Inspect new bedroom props")]
    private static void Inspect()
    {
        if (!Ready()) return;
        foreach (var name in new[] { "tudocanhan", "tivi", "tranh" })
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + name + ".fbx");
            if (source == null) { Debug.LogError("Missing imported FBX: " + name); continue; }
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
                Debug.Log("SCENE2_PROP " + name + " rootScale=" + visual.transform.localScale + " upright=" + BoundsOf(visual) +
                    " renderers=" + visual.GetComponentsInChildren<Renderer>(true).Length);
                visual.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                Debug.Log("SCENE2_PROP " + name + " rotated=" + BoundsOf(visual));
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        foreach (var path in new[] {
            "BedroomArea/RusticBedroom_Visual",
            "BedroomArea/RusticBedroom_Visual/BedBlock",
            "BedroomArea/BedroomProps_New/TuDoCaNhan",
            "BedroomArea/BedroomProps_New/Tivi",
            "BedroomArea/BedroomProps_New/Tranh",
            "HouseLayout/lunuoc/tripo_part_10", "HouseLayout/KitchenArea/CaiLu/cailugao" })
        {
            var obj = GameObject.Find(path);
            if (obj != null)
            {
                Debug.Log("SCENE2_PROP scene " + path + " bounds=" + BoundsOf(obj));
                var collider = obj.GetComponent<Collider>();
                if (collider != null) Debug.Log("SCENE2_PROP collider " + path + " bounds=" + collider.bounds);
            }
        }
    }

    private static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException("No renderers: " + root.name);
        var result = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) result.Encapsulate(renderers[i].bounds);
        return result;
    }

    private static bool Ready()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError("Open Act2_MemoryWorld_Home in Edit Mode first.");
            return false;
        }
        return true;
    }
}
