using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Readable, warm daylight for the childhood memory, with paired review views.</summary>
public static class Scene2MemoryLighting
{
    private const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string ProfilePath = "Assets/_Project/Generated/Act2_WarmReadableMemory.asset";
    private const string BackupPath = "Assets/_Project/Scenes/Backups/Act2_BeforeWarmMemoryLighting.unity";
    private const string Output = "Temp/Scene2MemoryLighting";

    [MenuItem("KuTy/Scene 2/Capture memory lighting before")]
    public static void CaptureBefore() => Capture("Before");

    [MenuItem("KuTy/Scene 2/Capture memory lighting after")]
    public static void CaptureAfter() => Capture("After");

    [MenuItem("KuTy/Scene 2/Apply warm readable memory lighting")]
    public static void Apply()
    {
        var scene = RequireScene();
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
        if (!File.Exists(BackupPath) && !EditorSceneManager.SaveScene(scene, BackupPath, true))
            throw new IOException("Could not save the memory lighting backup.");
        var root = GameObject.Find("Act2_Lighting_Depth");
        var volumeObject = GameObject.Find("Act2_GlobalVolume");
        if (!root || !volumeObject) throw new InvalidOperationException("Scene 2 lighting or volume is missing.");
        Undo.RegisterFullObjectHierarchyUndo(root, "Warm readable childhood memory");
        Undo.RecordObject(Unsupported.GetRenderSettings(), "Lift memory shadow illumination");
        // The interior needs bounced daylight, rather than stronger direct sun or exposure.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.72f, .71f, .65f);
        RenderSettings.ambientEquatorColor = new Color(.52f, .50f, .45f);
        RenderSettings.ambientGroundColor = new Color(.32f, .29f, .25f);
        RenderSettings.reflectionIntensity = .65f;
        DynamicGI.UpdateEnvironment();

        Bounce(root.transform, "Memory_Bedroom_DiffuseFill", new Vector3(9.8f, 2.45f, 4.5f),
            new Color(1f, .94f, .84f), 1.25f, 7f);
        Bounce(root.transform, "Memory_Kitchen_DiffuseFill", new Vector3(-3.1f, 2.5f, -4.7f),
            new Color(1f, .95f, .86f), 1.15f, 7.5f);
        Bounce(root.transform, "Memory_Living_DiffuseFill", new Vector3(5f, 2.5f, -2.5f),
            new Color(1f, .96f, .88f), .9f, 8f);

        var sun = RenderSettings.sun;
        if (sun && sun.gameObject.scene == scene)
        {
            Undo.RecordObject(sun, "Soften memory sun shadows");
            sun.shadowStrength = .75f;
            EditorUtility.SetDirty(sun);
        }
        var volume = volumeObject.GetComponent<Volume>();
        if (!volume) throw new InvalidOperationException("The Scene 2 global volume is missing.");
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (!profile)
        {
            if (!volume.sharedProfile || !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile), ProfilePath))
                throw new InvalidOperationException("Could not copy the existing memory profile.");
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        }
        Get<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
        var color = Get<ColorAdjustments>(profile);
        color.postExposure.Override(.2f);
        color.contrast.Override(2f);
        color.saturation.Override(4f);
        Get<WhiteBalance>(profile).temperature.Override(6f);
        var vignette = Get<Vignette>(profile);
        vignette.intensity.Override(.07f);
        vignette.smoothness.Override(.65f);
        var bloom = Get<Bloom>(profile);
        bloom.intensity.Override(.22f);
        bloom.threshold.Override(1.15f);
        bloom.scatter.Override(.6f);
        Undo.RecordObject(volume, "Use warm readable memory grading");
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(volume);
        foreach (var component in profile.components) if (component) EditorUtility.SetDirty(component);
        EditorUtility.SetDirty(profile);
        foreach (var sceneRoot in scene.GetRootGameObjects())
        foreach (var wake in sceneRoot.GetComponentsInChildren<BedWakeUpController>(true))
        {
            // Its camera-mounted point light was compensating for the old dark ambient.
            Undo.RecordObject(wake, "Balance wake-up light for brighter memory");
            var serialized = new SerializedObject(wake);
            serialized.FindProperty("wakeBounceIntensity").floatValue = .25f;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(wake);
        }
        foreach (var sceneRoot in scene.GetRootGameObjects())
        foreach (var camera in sceneRoot.GetComponentsInChildren<Camera>(true))
        {
            var data = camera.GetUniversalAdditionalCameraData();
            Undo.RecordObject(data, "Enable memory grading");
            data.renderPostProcessing = true;
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SceneView.RepaintAll();
        Debug.Log("SCENE2_WARM_MEMORY_APPLIED: lifted diffuse ambient, 3 soft fills, gentle grading; original window beams retained.");
    }

    private static void Bounce(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        var child = parent.Find(name);
        if (!child)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Memory daylight bounce");
            child = go.transform;
            child.SetParent(parent, false);
        }
        child.position = position;
        var light = child.GetComponent<Light>();
        if (!light) light = Undo.AddComponent<Light>(child.gameObject);
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        EditorUtility.SetDirty(light);
    }

    private static Scene RequireScene(bool allowPlay = false)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || (!allowPlay && EditorApplication.isPlayingOrWillChangePlaymode))
            throw new InvalidOperationException("Open Act2_MemoryWorld_Home in Edit Mode first.");
        return scene;
    }

    private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var component)) return component;
        component = profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static void Capture(string phase)
    {
        RequireScene(true);
        Directory.CreateDirectory(Output + "/" + phase);
        var source = Camera.main;
        var go = new GameObject("MemoryLightingReviewCamera") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var camera = go.AddComponent<Camera>();
            if (source) camera.CopyFrom(source);
            camera.enabled = false;
            camera.fieldOfView = 65f;
            camera.aspect = 16f / 9f;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 150f;
            camera.allowHDR = true;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            if (source)
            {
                var sourceData = source.GetUniversalAdditionalCameraData();
                data.volumeLayerMask = sourceData.volumeLayerMask;
                Shot(camera, source.transform.position, source.transform.rotation, phase, "CameraView");
            }
            Shot(camera, new Vector3(6.4f, 1.65f, 3f), Quaternion.LookRotation(new Vector3(5.9f, .15f, 1.8f)), phase, "Bedroom");
            Shot(camera, new Vector3(-.4f, 1.7f, -1f), Quaternion.LookRotation(new Vector3(-4.5f, -.4f, -4.2f)), phase, "Kitchen");
            Shot(camera, new Vector3(4.5f, 1.7f, -2f), Quaternion.LookRotation(new Vector3(5.5f, -.3f, -2f)), phase, "Living");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        Debug.Log("SCENE2_MEMORY_REVIEW " + phase + " " + Output);
    }

    private static void Shot(Camera camera, Vector3 position, Quaternion rotation, string phase, string name)
    {
        camera.transform.SetPositionAndRotation(position, rotation);
        var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGBHalf);
        var previous = RenderTexture.active;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            var png = image.EncodeToPNG();
            File.WriteAllBytes(Output + "/" + phase + "/" + name + ".png", png);
            if (phase == "After")
            {
                Directory.CreateDirectory("Assets/Screenshots");
                File.WriteAllBytes("Assets/Screenshots/Scene2Memory_" + name + ".png", png);
            }
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
