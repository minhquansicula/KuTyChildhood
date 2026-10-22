using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class OfficeLightingPolish
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    private const string Folder = "Assets/_Project/Art/Office/Lighting";
    private const string Backup = "Assets/_Project/Scenes/Backups/Office_BeforeLightingPolish.unity";

    [MenuItem("KuTy/Office/Capture Polished Lighting")]
    public static void Capture()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Open the office first.");
        var previous = QualitySettings.renderPipeline;
        var previousAA = QualitySettings.antiAliasing;
        var go = new GameObject("TemporaryPolishedLightingPreview") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder + "/OfficeCinematicURP.asset");
            var camera = go.AddComponent<Camera>();
            camera.enabled = false;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.fieldOfView = 68f;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 180f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            Shot(camera, new Vector3(-1.1f, 1.65f, 2f), new Vector3(4f, 1.3f, -2f), "Workspace");
            Shot(camera, new Vector3(4.5f, 1.65f, -.4f), new Vector3(17f, 3f, 0f), "Window");
            Shot(camera, new Vector3(-4.8f, 1.55f, 1.8f), new Vector3(-4.8f, 1.3f, 4.35f), "Boss");
            Shot(camera, new Vector3(4.4f, 1.35f, -2.25f), new Vector3(4.4f, 1.05f, -1.7f), "Desk");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            QualitySettings.renderPipeline = previous;
            QualitySettings.antiAliasing = previousAA;
            AssetDatabase.SaveAssets();
        }
    }

    private static void Shot(Camera camera, Vector3 from, Vector3 to, string name)
    {
        camera.transform.position = from;
        camera.transform.LookAt(to);
        var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGBHalf,
            RenderTextureReadWrite.Default, 4);
        var previous = RenderTexture.active;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Assets/Screenshots");
            File.WriteAllBytes("Assets/Screenshots/OfficePolished_" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    [MenuItem("KuTy/Office/Polish Sunset Lighting")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || EditorApplication.isPlaying)
            throw new InvalidOperationException("Open Act1_Office_Blockout in Edit Mode first.");
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Path.GetDirectoryName(Backup));
        if (!File.Exists(Backup) && !EditorSceneManager.SaveScene(scene, Backup, true))
            throw new IOException("Could not save lighting backup.");
        AssetDatabase.Refresh();

        var root = GameObject.Find("OFFICE_BLOCKOUT/06_Lighting");
        if (!root) throw new InvalidOperationException("Office lighting group missing.");
        Undo.RegisterFullObjectHierarchyUndo(root, "Polish office sunset lighting");

        var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        var pipelinePath = Folder + "/OfficeCinematicURP.asset";
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (!pipeline)
        {
            if (!source || !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), pipelinePath))
                throw new InvalidOperationException("Could not clone active URP asset.");
            pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        }
        var serialized = new SerializedObject(pipeline);
        var renderers = serialized.FindProperty("m_RendererDataList");
        var rendererPath = Folder + "/OfficeRenderer.asset";
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (!renderer)
        {
            var original = renderers.GetArrayElementAtIndex(0).objectReferenceValue;
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), rendererPath))
                throw new InvalidOperationException("Could not clone office renderer.");
            renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        }
        renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
        serialized.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = 2048;
        serialized.FindProperty("m_MainLightShadowmapResolution").intValue = 4096;
        serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
        serialized.FindProperty("m_ReflectionProbeBlending").boolValue = true;
        serialized.FindProperty("m_ReflectionProbeBoxProjection").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        pipeline.supportsHDR = true;
        pipeline.msaaSampleCount = 4;
        pipeline.shadowCascadeCount = 4;
        pipeline.shadowDistance = 35f;
        pipeline.colorGradingMode = ColorGradingMode.HighDynamicRange;
        renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
        // AO should anchor furniture, not outline every object in black.
        foreach (var feature in renderer.rendererFeatures)
        {
            if (!feature || feature.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
            var ao = new SerializedObject(feature);
            ao.FindProperty("m_Settings.Intensity").floatValue = 1.15f;
            ao.FindProperty("m_Settings.Radius").floatValue = .16f;
            ao.FindProperty("m_Settings.DirectLightingStrength").floatValue = .12f;
            ao.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feature);
        }
        renderer.SetDirty();
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(pipeline);
        var scope = root.GetComponent<OfficeRenderQuality>();
        if (!scope) scope = Undo.AddComponent<OfficeRenderQuality>(root);
        scope.Configure(pipeline);
        EditorUtility.SetDirty(scope);

        foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>(true))
        {
            if (light.gameObject.scene != scene) continue;
            Undo.RecordObject(light, "Balance office lights");
            switch (light.name)
            {
                case "AfternoonSun": light.intensity = 2.35f; light.shadowStrength = .83f; break;
                case "WindowSunsetBounce": light.intensity = 1.25f; light.range = 11f; break;
                case "BossOverheadLight": light.intensity = 1.65f; break;
                case "BossDeskSpot": light.intensity = 1.3f; light.spotAngle = 65f; break;
                case "TyDeskSpot": light.intensity = .55f; light.shadows = LightShadows.None; break;
                case "TyScreenGlow": light.intensity = .22f; light.range = 1.1f; break;
                case "TyDeskLamp_Spot": light.intensity = 1.05f; light.shadows = LightShadows.Soft; break;
                case "TyMonitorGlow": light.intensity = .18f; break;
                case "TyMonitor_ScreenGlow": light.intensity = .4f; break;
                case "TySeat_HeroSpot": light.intensity = .8f; light.shadows = LightShadows.None; break;
                case "TyChairAccent": light.intensity = .3f; break;
                case "UtilityAccentLight": light.intensity = 1.15f; break;
            }
            if (light.name.StartsWith("CeilingLight_")) light.intensity = 2.1f;
            if (light.name == "BossDeskSpot" || light.name == "TyDeskLamp_Spot")
            {
                light.shadows = LightShadows.Soft;
                light.shadowStrength = .65f;
                light.shadowBias = .035f;
                light.shadowNormalBias = .15f;
                light.shadowNearPlane = .08f;
            }
            EditorUtility.SetDirty(light);
        }

        // Broad low-energy fills simulate diffuse bounce without flattening the key light.
        Fill(root.transform, "CeilingDiffuseBounce", new Vector3(0f, 1.1f, .2f),
            new Color(.77f, .83f, 1f), 1.4f, 9f, Vector3.up);
        Fill(root.transform, "BossWallBounce", new Vector3(-4.6f, 1.1f, 3.3f),
            new Color(1f, .79f, .60f), .8f, 5f, Vector3.up);
        Fill(root.transform, "WestDiffuseFill", new Vector3(-3.8f, 2.1f, -.8f),
            new Color(.76f, .85f, 1f), 1.1f, 7f, Vector3.down);
        Fill(root.transform, "EastDiffuseFill", new Vector3(3.6f, 2.1f, .5f),
            new Color(1f, .85f, .70f), .9f, 7f, Vector3.down);
        RenderSettings.ambientSkyColor = new Color(.44f, .39f, .35f);
        RenderSettings.ambientEquatorColor = new Color(.36f, .38f, .42f);
        RenderSettings.ambientGroundColor = new Color(.22f, .215f, .23f);
        DynamicGI.UpdateEnvironment();

        var profilePath = Folder + "/OfficeCinematicMood.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (!profile)
        {
            AssetDatabase.CopyAsset("Assets/_Project/Art/Environment/City/Materials/OfficeSunsetMood.asset", profilePath);
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        }
        Get<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
        var color = Get<ColorAdjustments>(profile);
        color.postExposure.Override(0f);
        color.contrast.Override(10f);
        color.saturation.Override(1f);
        color.colorFilter.Override(new Color(1f, .99f, .98f));
        var bloom = Get<Bloom>(profile);
        bloom.threshold.Override(1.25f);
        bloom.intensity.Override(.16f);
        bloom.scatter.Override(.6f);
        Get<WhiteBalance>(profile).temperature.Override(4f);
        Get<Vignette>(profile).intensity.Override(.13f);
        Get<FilmGrain>(profile).intensity.Override(.035f);
        foreach (var volume in UnityEngine.Object.FindObjectsOfType<Volume>(true))
        {
            if (volume.gameObject.scene != scene) continue;
            Undo.RecordObject(volume, "Resolve office volume priority");
            if (volume.name == "SunsetMoodVolume")
            {
                volume.sharedProfile = profile;
                volume.priority = 20f;
            }
            // This legacy profile has missing sub-assets; leave it as an inactive reference.
            if (volume.name == "OfficeGameplayMoodVolume") volume.enabled = false;
            EditorUtility.SetDirty(volume);
        }
        foreach (var camera in UnityEngine.Object.FindObjectsOfType<Camera>(true))
        {
            if (camera.gameObject.scene != scene) continue;
            var data = camera.GetUniversalAdditionalCameraData();
            Undo.RecordObject(data, "Polish office camera");
            data.renderPostProcessing = true;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(data);
        }
        foreach (var probe in UnityEngine.Object.FindObjectsOfType<ReflectionProbe>())
        {
            if (probe.gameObject.scene != scene) continue;
            Undo.RecordObject(probe, "Refresh office reflections");
            probe.boxProjection = true;
            probe.resolution = 256;
            probe.RenderProbe();
            EditorUtility.SetDirty(probe);
        }
        AddPracticalDiffusers(root.transform);
        foreach (var item in profile.components) if (item) EditorUtility.SetDirty(item);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("OFFICE_LIGHTING_POLISHED: dedicated URP, balanced key/fill, selective shadows, ACES, MSAA 4x.");
    }

    private static void AddPracticalDiffusers(Transform root)
    {
        var materialPath = Folder + "/CeilingDiffuser.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetColor("_BaseColor", new Color(.83f, .88f, .9f));
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(.83f, .91f, 1f) * 2.2f);
        material.SetFloat("_Smoothness", .3f);
        EditorUtility.SetDirty(material);
        foreach (Transform slot in root)
        {
            if (slot.name != "CeilingLight_SLOT") continue;
            foreach (Transform fixture in slot)
            {
                if (!fixture.name.StartsWith("ImportedOfficeCeilingLight")) continue;
                var meshes = fixture.GetComponentsInChildren<MeshRenderer>().Where(r => !r.name.StartsWith("PracticalDiffuser")).ToArray();
                if (meshes.Length == 0) continue;
                var bounds = meshes[0].bounds;
                foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
                bool alongX = bounds.size.x > bounds.size.z;
                float length = Mathf.Clamp((alongX ? bounds.size.x : bounds.size.z) * .72f, .3f, 2.2f);
                for (int i = 0; i < 2; i++)
                {
                    var strip = fixture.Find("PracticalDiffuser_" + i);
                    if (!strip)
                    {
                        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.name = "PracticalDiffuser_" + i;
                        Undo.RegisterCreatedObjectUndo(go, "Office light diffuser");
                        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                        strip = go.transform;
                        strip.SetParent(fixture, true);
                    }
                    // Set world dimensions before parenting back under imported FBX scales.
                    strip.SetParent(null, true);
                    strip.rotation = Quaternion.identity;
                    strip.localScale = alongX ? new Vector3(length, .012f, .045f) : new Vector3(.045f, .012f, length);
                    strip.position = new Vector3(bounds.center.x, bounds.min.y - .008f, bounds.center.z) +
                        (alongX ? Vector3.forward : Vector3.right) * (i == 0 ? -.07f : .07f);
                    strip.SetParent(fixture, true);
                    var r = strip.GetComponent<MeshRenderer>();
                    r.sharedMaterial = material;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
            }
        }
    }

    private static void Fill(Transform parent, string name, Vector3 position, Color color,
        float intensity, float range, Vector3 direction)
    {
        var child = parent.Find(name);
        if (!child)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Office diffuse bounce");
            child = go.transform;
            child.SetParent(parent, false);
        }
        child.position = position;
        child.rotation = Quaternion.LookRotation(direction, Vector3.forward);
        var light = child.GetComponent<Light>();
        if (!light) light = Undo.AddComponent<Light>(child.gameObject);
        light.type = LightType.Spot;
        light.spotAngle = 150f;
        light.innerSpotAngle = 100f;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        EditorUtility.SetDirty(light);
    }

    private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var value)) return value;
        value = profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(value, profile);
        return value;
    }
}
