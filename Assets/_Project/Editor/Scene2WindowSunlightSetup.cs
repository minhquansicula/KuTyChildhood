using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Author and inspect the daylight entering the two Scene 2 windows.</summary>
public static class Scene2WindowSunlightSetup
{
    const string Output = "Temp/Scene2WindowLighting";

    [MenuItem("KuTy/Scene 2/Apply sunlight through bedroom and kitchen windows")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "Act2_MemoryWorld_Home")
            throw new InvalidOperationException("Open Scene 2 in Edit Mode first.");
        var sun = GameObject.Find("Sun")?.GetComponent<Light>();
        var root = GameObject.Find("Act2_Lighting_Depth");
        if (!sun || !root) throw new InvalidOperationException("Scene 2 sunlight or lighting root is missing.");

        Directory.CreateDirectory(Output);
        if (!File.Exists(Output + "/scene-before.unity")) File.Copy(scene.path, Output + "/scene-before.unity");
        Undo.RecordObjects(new UnityEngine.Object[] { sun, sun.transform }, "Sunlight through Scene 2 windows");
        // Both openings face the south/east exterior: one sun must enter both consistently.
        sun.transform.rotation = Quaternion.Euler(32f, 315f, 0f);
        sun.color = new Color(1f, .89f, .72f);
        sun.intensity = 2.4f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = .9f;
        sun.shadowBias = .025f;
        sun.shadowNormalBias = .12f;
        Undo.RecordObject(Unsupported.GetRenderSettings(), "Assign Scene 2 sun");
        RenderSettings.sun = sun;

        // Small, unshadowed fills approximate window bounce; the directional sun supplies the bars' real shadows.
        AddBounce(root.transform, "Bedroom_Window_DaylightBounce", new Vector3(12.25f, 2.35f, 4.4f),
            new Vector3(-1f, -.3f, .2f), .6f, 4.5f);
        AddBounce(root.transform, "Kitchen_Window_DaylightBounce", new Vector3(-4.1f, 2.25f, -7.1f),
            new Vector3(-.25f, -.3f, 1f), .8f, 4.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        SceneView.RepaintAll();
        Debug.Log("WINDOW_SUNLIGHT_APPLIED sun=(32,315,0) intensity=2.4; two local bounce lights; shared URP unchanged.");
    }

    static void AddBounce(Transform parent, string name, Vector3 position, Vector3 direction, float intensity, float range)
    {
        var existing = parent.Find(name);
        var obj = existing ? existing.gameObject : new GameObject(name);
        if (!existing) Undo.RegisterCreatedObjectUndo(obj, "Create window daylight bounce");
        Undo.RecordObject(obj.transform, "Position window daylight bounce");
        obj.transform.SetParent(parent, true);
        obj.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        var light = obj.GetComponent<Light>();
        if (!light) light = Undo.AddComponent<Light>(obj);
        Undo.RecordObject(light, "Configure window daylight bounce");
        light.type = LightType.Spot;
        light.spotAngle = 115f;
        light.innerSpotAngle = 65f;
        light.range = range;
        light.intensity = intensity;
        light.color = new Color(1f, .92f, .8f);
        light.shadows = LightShadows.None;
        light.bounceIntensity = 0f;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        light.renderMode = LightRenderMode.ForcePixel;
    }

    [MenuItem("KuTy/Scene 2/Capture window sunlight views")]
    public static void CaptureViews()
    {
        if (SceneManager.GetActiveScene().name != "Act2_MemoryWorld_Home")
            throw new InvalidOperationException("Open Scene 2 first.");
        Directory.CreateDirectory(Output);
        Capture("kitchen", new Vector3(-2.957269f, 1.88f, -3.832423f), Quaternion.Euler(.2f, 190.4f, 0));
        Capture("bedroom", new Vector3(9.1f, 1.65f, 4.4f), Quaternion.LookRotation(new Vector3(3.6f, .4f, 0)));
        Capture("bedroom-room", new Vector3(6.4f, 1.65f, 3.0f), Quaternion.LookRotation(new Vector3(5.9f, .15f, 1.8f)));
        Capture("kitchen-room", new Vector3(-.4f, 1.7f, -1f), Quaternion.LookRotation(new Vector3(-4.5f, -.4f, -4.2f)));
        Debug.Log("WINDOW_SUNLIGHT_VIEWS " + Output);
    }

    static void Capture(string name, Vector3 position, Quaternion rotation)
    {
        var obj = new GameObject("WindowSunlightPreview") { hideFlags = HideFlags.HideAndDontSave };
        var camera = obj.AddComponent<Camera>();
        var source = Camera.main;
        if (source) camera.CopyFrom(source);
        camera.enabled = false;
        camera.fieldOfView = 65f;
        camera.nearClipPlane = .05f;
        camera.farClipPlane = 150f;
        camera.aspect = 16f / 9f;
        camera.transform.SetPositionAndRotation(position, rotation);
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGBHalf);
        var previous = RenderTexture.active;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    [Serializable] sealed class Pose { public Vector3 position; public Vector3 rotation; }
    [Serializable] sealed class SavedView { public Pose player; public Pose camera; }

    [MenuItem("KuTy/Scene 2/Restore recorded window view in Play Mode")]
    public static void RestorePlayView()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Act2_MemoryWorld_Home" ||
            !File.Exists(Output + "/play-view.json"))
            throw new InvalidOperationException("Requires Scene 2 Play Mode and a recorded play-view.json.");
        var view = JsonUtility.FromJson<SavedView>(File.ReadAllText(Output + "/play-view.json"));
        var bed = UnityEngine.Object.FindObjectOfType<BedWakeUpController>();
        if (bed) typeof(BedWakeUpController).GetMethod("GetOutOfBed", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(bed, null);
        double ready = EditorApplication.timeSinceStartup + 1.2;
        void RestoreWhenReady()
        {
            if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup < ready) return;
            EditorApplication.update -= RestoreWhenReady;
            if (!EditorApplication.isPlaying) return;
            var motor = UnityEngine.Object.FindObjectOfType<FirstPersonController>();
            if (!motor) return;
            motor.TeleportTo(view.player.position, Quaternion.Euler(0, view.player.rotation.y, 0));
            motor.ResetCameraVerticalRotation(Mathf.DeltaAngle(0, view.camera.rotation.x));
            Debug.Log("WINDOW_SUNLIGHT_RESTORED_PLAYER_VIEW");
        }
        EditorApplication.update += RestoreWhenReady;
    }
}
