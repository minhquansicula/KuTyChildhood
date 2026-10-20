using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class Scene2CinematicLighting
{
    public const string ProfilePath = "Assets/_Project/Generated/Act2_CinematicLighting.asset";

    [MenuItem("KuTy/Scene 2/Apply Cinematic Ambient")]
    public static void ApplyAmbient()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.name != "Act2_MemoryWorld_Home")
            throw new InvalidOperationException("Open Act2_MemoryWorld_Home in Edit Mode first.");

        Undo.RecordObject(Unsupported.GetRenderSettings(), "Scene 2 cinematic ambient");
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.15f, .20f, .27f);
        RenderSettings.ambientEquatorColor = new Color(.075f, .095f, .13f);
        RenderSettings.ambientGroundColor = new Color(.035f, .029f, .025f);
        RenderSettings.reflectionIntensity = .45f;

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        var root = GameObject.Find("Act2_Lighting_Depth");
        if (!root) throw new InvalidOperationException("Act2_Lighting_Depth is missing.");
        var volume = root.GetComponent<Volume>();
        if (!volume) volume = Undo.AddComponent<Volume>(root);
        Undo.RecordObject(volume, "Scene 2 grading");
        volume.isGlobal = true;
        volume.priority = 10;
        volume.sharedProfile = profile;

        foreach (var sceneRoot in scene.GetRootGameObjects())
        foreach (var camera in sceneRoot.GetComponentsInChildren<Camera>(true))
        {
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (!data) data = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
            Undo.RecordObject(data, "Enable Scene 2 grading");
            data.renderPostProcessing = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(data);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Scene 2 cinematic ambient and grading volume ready.");
    }
}
