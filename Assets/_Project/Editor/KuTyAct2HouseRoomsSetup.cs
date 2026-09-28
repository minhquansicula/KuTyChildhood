using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KuTyAct2HouseRoomsSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string ModelFolder = "Assets/_Project/Art/Models/Act2Home";

    [MenuItem("KuTy/Setup/Preview Act 2 only")]
    public static void PreviewAct2Only()
    {
        foreach (Scene scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
        {
            if (scene.path == ScenePath) continue;
            foreach (GameObject root in scene.GetRootGameObjects())
                SceneVisibilityManager.instance.Hide(root, true);
        }
    }

    [MenuItem("KuTy/Setup/Restore scene visibility")]
    public static void RestoreSceneVisibility()
    {
        SceneVisibilityManager.instance.ShowAll();
    }

    [MenuItem("KuTy/Setup/Inspect Act 2 house rooms")]
    public static void Inspect()
    {
        foreach (string modelName in new[] { "house_1", "house_2" })
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{modelName}.fbx");
            if (asset == null) throw new InvalidOperationException($"Missing {modelName}.fbx");

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, previewScene);
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                string materialSummary = string.Join(" | ", renderers.Select(r =>
                    $"{r.name}:{string.Join(",", r.sharedMaterials.Select(m => m == null ? "null" : m.name + "/" + (m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap")?.name : "no BaseMap")))}"));
                Debug.Log($"KUTY_ACT2_MODEL {modelName} bounds={bounds} renderers={renderers.Length} materials={materialSummary}");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "BedBlock" || root.name == "Sink" || root.name == "JournalTable" || root.name == "ChainedDoor" || root.name == "Act2 Imported Home")
                    Debug.Log($"KUTY_ACT2_ANCHOR {root.name} position={root.transform.position} scale={root.transform.lossyScale}");
            }
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
