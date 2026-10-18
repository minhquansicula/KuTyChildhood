using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class KuTyOfficeSunsetLighting
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    private const string BackupPath = "Assets/_Project/Scenes/Backups/Office_BeforeSunsetLighting.unity";
    private const string ProfilePath = "Assets/_Project/Art/Environment/City/Materials/OfficeSunsetMood.asset";

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        var comp = go.GetComponent<T>();
        if (!comp) comp = go.AddComponent<T>();
        return comp;
    }

    [MenuItem("KuTy/Office Intro/Apply Oppressive Sunset Lighting")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || EditorApplication.isPlaying)
            throw new InvalidOperationException("Open office in Edit Mode first.");

        var root = GameObject.Find("OFFICE_BLOCKOUT/06_Lighting");
        if (!root) throw new InvalidOperationException("Lighting group missing.");

        // Safety backup
        if (!File.Exists(BackupPath))
        {
            var dir = Path.GetDirectoryName(BackupPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!EditorSceneManager.SaveScene(scene, BackupPath, true))
                throw new IOException("Could not save lighting backup.");
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Apply Cinematic Office Lighting");

        // 1. DIRECTIONAL SUNLIGHT (AfternoonSun) - Golden Hour streaming through East windows
        var sunObj = root.transform.Find("AfternoonSun");
        if (!sunObj)
        {
            var go = new GameObject("AfternoonSun");
            go.transform.SetParent(root.transform, false);
            sunObj = go.transform;
        }
        var sun = GetOrAddComponent<Light>(sunObj.gameObject);
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(12.5f, -88f, 0f);
        sun.color = new Color(1.0f, 0.62f, 0.30f);
        sun.intensity = 2.85f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.88f;
        sun.shadowBias = 0.02f;
        sun.shadowNormalBias = 0.12f;
        RenderSettings.sun = sun;
        EditorUtility.SetDirty(sun);

        // 2. WINDOW BOUNCE / WARM DIFFUSE FILL
        var bounceObj = root.transform.Find("WindowSunsetBounce");
        if (!bounceObj)
        {
            var go = new GameObject("WindowSunsetBounce");
            go.transform.SetParent(root.transform, false);
            bounceObj = go.transform;
        }
        bounceObj.position = new Vector3(6.2f, 1.8f, 0.0f);
        var bounce = GetOrAddComponent<Light>(bounceObj.gameObject);
        bounce.type = LightType.Point;
        bounce.color = new Color(1.0f, 0.72f, 0.48f);
        bounce.intensity = 2.2f;
        bounce.range = 14f;
        bounce.shadows = LightShadows.None;
        EditorUtility.SetDirty(bounce);

        // 3. CEILING FLUORESCENT TROFFERS (Downward directional spot cones - zero ceiling burn!)
        var slotPositions = new (string name, Vector3 pos, float intensity)[]
        {
            ("CeilingLight_H1", new Vector3(-4.7f, 2.75f, -2.6f), 2.4f),
            ("CeilingLight_H2", new Vector3( 0.0f, 2.75f, -2.6f), 2.6f),
            ("CeilingLight_H3", new Vector3( 4.4f, 2.75f, -2.6f), 2.2f),
            ("CeilingLight_W1", new Vector3(-4.7f, 2.75f,  3.2f), 2.4f),
            ("CeilingLight_W2", new Vector3( 0.0f, 2.75f,  3.2f), 2.6f),
            ("CeilingLight_W3", new Vector3( 4.4f, 2.75f,  3.2f), 2.2f),
        };

        var ceilingGroup = root.transform.Find("CeilingFluorescents");
        if (!ceilingGroup)
        {
            var go = new GameObject("CeilingFluorescents");
            go.transform.SetParent(root.transform, false);
            ceilingGroup = go.transform;
        }

        foreach (var slot in slotPositions)
        {
            var lightTr = ceilingGroup.Find(slot.name);
            if (!lightTr)
            {
                var go = new GameObject(slot.name);
                go.transform.SetParent(ceilingGroup, false);
                lightTr = go.transform;
            }
            lightTr.position = slot.pos;
            lightTr.rotation = Quaternion.Euler(90f, 0f, 0f); // Pointing straight DOWN
            var l = GetOrAddComponent<Light>(lightTr.gameObject);
            l.type = LightType.Spot;
            l.color = new Color(0.84f, 0.91f, 0.95f); // Cool fluorescent 4200K
            l.intensity = slot.intensity;
            l.range = 5.5f;
            l.spotAngle = 115f;
            l.innerSpotAngle = 70f;
            l.shadows = LightShadows.None;
            EditorUtility.SetDirty(l);
        }

        // Disable any legacy point lights under CeilingLight_SLOT
        foreach (var legacyLight in root.GetComponentsInChildren<Light>(true))
        {
            if (legacyLight.transform.parent != null && legacyLight.transform.parent.name == "Visual_REPLACE")
            {
                legacyLight.enabled = false;
                EditorUtility.SetDirty(legacyLight);
            }
        }

        // Clean up or absorb any stray root Spot Light
        var straySpot = GameObject.Find("Spot Light");
        if (straySpot && straySpot.transform.parent == null)
        {
            straySpot.SetActive(false);
            EditorUtility.SetDirty(straySpot);
        }

        // 4. BOSS OFFICE LIGHTING
        var bossGroup = root.transform.Find("BossOfficeLighting");
        if (!bossGroup)
        {
            var go = new GameObject("BossOfficeLighting");
            go.transform.SetParent(root.transform, false);
            bossGroup = go.transform;
        }

        // Boss overhead troffer (downward spot)
        var bossOverhead = bossGroup.Find("BossOverheadLight");
        if (!bossOverhead)
        {
            var go = new GameObject("BossOverheadLight");
            go.transform.SetParent(bossGroup, false);
            bossOverhead = go.transform;
        }
        bossOverhead.position = new Vector3(-4.8f, 2.75f, 3.5f);
        bossOverhead.rotation = Quaternion.Euler(90f, 0f, 0f);
        var bLight = GetOrAddComponent<Light>(bossOverhead.gameObject);
        bLight.type = LightType.Spot;
        bLight.color = new Color(0.82f, 0.89f, 0.95f);
        bLight.intensity = 2.6f;
        bLight.range = 5.2f;
        bLight.spotAngle = 110f;
        bLight.innerSpotAngle = 65f;
        bLight.shadows = LightShadows.None;
        EditorUtility.SetDirty(bLight);

        // Boss desk accent spot (gives intimidating noir silhouette)
        var bossSpot = bossGroup.Find("BossDeskSpot");
        if (!bossSpot)
        {
            var go = new GameObject("BossDeskSpot");
            go.transform.SetParent(bossGroup, false);
            bossSpot = go.transform;
        }
        bossSpot.position = new Vector3(-4.8f, 2.65f, 3.5f);
        bossSpot.rotation = Quaternion.Euler(80f, 0f, 0f);
        var bsLight = GetOrAddComponent<Light>(bossSpot.gameObject);
        bsLight.type = LightType.Spot;
        bsLight.color = new Color(0.92f, 0.95f, 1.0f);
        bsLight.intensity = 2.8f;
        bsLight.range = 3.5f;
        bsLight.spotAngle = 50f;
        bsLight.innerSpotAngle = 30f;
        bsLight.shadows = LightShadows.Soft;
        EditorUtility.SetDirty(bsLight);

        // 5. TÝ'S HERO DESK LIGHTING
        var tyGroup = root.transform.Find("TyDeskLighting");
        if (!tyGroup)
        {
            var go = new GameObject("TyDeskLighting");
            go.transform.SetParent(root.transform, false);
            tyGroup = go.transform;
        }

        // CRT phosphor screen glow onto keyboard and paperwork
        var screenGlow = tyGroup.Find("TyScreenGlow");
        if (!screenGlow)
        {
            var go = new GameObject("TyScreenGlow");
            go.transform.SetParent(tyGroup, false);
            screenGlow = go.transform;
        }
        screenGlow.position = new Vector3(4.4f, 1.15f, -2.05f);
        var sgLight = GetOrAddComponent<Light>(screenGlow.gameObject);
        sgLight.type = LightType.Point;
        sgLight.color = new Color(0.65f, 0.90f, 0.85f);
        sgLight.intensity = 0.85f;
        sgLight.range = 1.4f;
        sgLight.shadows = LightShadows.None;
        EditorUtility.SetDirty(sgLight);

        // Focused desk pad task light
        var tySpot = tyGroup.Find("TyDeskSpot");
        if (!tySpot)
        {
            var go = new GameObject("TyDeskSpot");
            go.transform.SetParent(tyGroup, false);
            tySpot = go.transform;
        }
        tySpot.position = new Vector3(4.25f, 2.30f, -2.10f);
        tySpot.rotation = Quaternion.Euler(70f, 35f, 0f);
        var tsLight = GetOrAddComponent<Light>(tySpot.gameObject);
        tsLight.type = LightType.Spot;
        tsLight.color = new Color(1.0f, 0.88f, 0.72f);
        tsLight.intensity = 1.6f;
        tsLight.range = 2.2f;
        tsLight.spotAngle = 45f;
        tsLight.innerSpotAngle = 25f;
        tsLight.shadows = LightShadows.Soft;
        EditorUtility.SetDirty(tsLight);

        // 6. UTILITY AREA ACCENT (Clock & water cooler downlight)
        var utilLight = root.transform.Find("UtilityAccentLight");
        if (!utilLight)
        {
            var go = new GameObject("UtilityAccentLight");
            go.transform.SetParent(root.transform, false);
            utilLight = go.transform;
        }
        utilLight.position = new Vector3(3.2f, 2.70f, 3.8f);
        utilLight.rotation = Quaternion.Euler(85f, 0f, 0f);
        var uLight = GetOrAddComponent<Light>(utilLight.gameObject);
        uLight.type = LightType.Spot;
        uLight.color = new Color(0.85f, 0.90f, 0.94f);
        uLight.intensity = 1.9f;
        uLight.range = 4.5f;
        uLight.spotAngle = 90f;
        uLight.innerSpotAngle = 50f;
        uLight.shadows = LightShadows.None;
        EditorUtility.SetDirty(uLight);

        // 7. REFLECTION PROBES (Box Projection for realistic glossy materials)
        SetupReflectionProbe(root.transform, "OfficeMain_ReflectionProbe",
            new Vector3(0.0f, 1.5f, 0.5f), new Vector3(15.0f, 3.5f, 11.0f), 0.85f);

        SetupReflectionProbe(root.transform, "BossOffice_ReflectionProbe",
            new Vector3(-4.8f, 1.5f, 3.5f), new Vector3(5.5f, 3.5f, 5.0f), 0.80f);

        // 8. ENVIRONMENT & AMBIENT SETTINGS (Trilight)
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.34f, 0.27f, 0.23f);     // Warm sunset sky
        RenderSettings.ambientEquatorColor = new Color(0.18f, 0.19f, 0.21f); // Muted slate neutral
        RenderSettings.ambientGroundColor = new Color(0.09f, 0.08f, 0.09f);  // Clean deep shadow
        RenderSettings.reflectionIntensity = 0.85f;

        // 9. VOLUME POST-PROCESSING PROFILE (Preserves sunset & clouds while giving cinematic office look)
        var t = root.transform.Find("SunsetMoodVolume");
        var volumeObj = t ? t.gameObject : new GameObject("SunsetMoodVolume");
        if (!t)
        {
            Undo.RegisterCreatedObjectUndo(volumeObj, "Office mood volume");
            volumeObj.transform.SetParent(root.transform, false);
        }
        var volume = GetOrAddComponent<Volume>(volumeObj);

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        // ACES Tonemapping
        var tone = Get<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.ACES);

        // Bloom: threshold 1.15f keeps sky clouds intact and unblown, while still giving lights and reflections a soft photographic bloom
        var bloom = Get<Bloom>(profile);
        bloom.threshold.Override(1.15f);
        bloom.intensity.Override(0.35f);
        bloom.scatter.Override(0.72f);
        bloom.tint.Override(new Color(1.0f, 0.96f, 0.90f));
        bloom.highQualityFiltering.Override(true);

        // Color Adjustments: lifted exposure, rich contrast, balanced saturation
        var color = Get<ColorAdjustments>(profile);
        color.postExposure.Override(0.20f);
        color.contrast.Override(18f);
        color.saturation.Override(6f);
        color.colorFilter.Override(new Color(1.0f, 0.98f, 0.95f));

        // White Balance: subtle sunset warmth
        var wb = Get<WhiteBalance>(profile);
        wb.active = true;
        wb.temperature.Override(10f);
        wb.tint.Override(2f);

        // Split Toning / Shadows & Highlights: cool shadows, golden highlights
        var smh = Get<ShadowsMidtonesHighlights>(profile);
        smh.active = true;
        smh.shadows.Override(new Vector4(0.94f, 0.97f, 1.02f, 0f));
        smh.highlights.Override(new Vector4(1.05f, 0.98f, 0.90f, 0f));

        // Subtle Vignette
        var vignette = Get<Vignette>(profile);
        vignette.intensity.Override(0.20f);
        vignette.smoothness.Override(0.58f);

        // Subtle 90s Film Grain
        var grain = Get<FilmGrain>(profile);
        grain.active = true;
        grain.intensity.Override(0.10f);
        grain.type.Override(FilmGrainLookup.Medium1);

        volume.isGlobal = true;
        volume.priority = 30;
        volume.sharedProfile = profile;

        // 10. CAMERA SETUP
        foreach (var camera in UnityEngine.Object.FindObjectsOfType<Camera>())
        {
            if (camera.gameObject.scene != scene) continue;
            var data = camera.GetUniversalAdditionalCameraData();
            Undo.RecordObject(data, "Enable office post-processing");
            data.renderPostProcessing = true;
            EditorUtility.SetDirty(data);
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        Capture();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("OFFICE_CINEMATIC_LIGHTING_APPLIED: Preserved rich sunset & cloud details with full cinematic office illumination.");
    }

    private static void SetupReflectionProbe(Transform parent, string name, Vector3 pos, Vector3 size, float intensity)
    {
        var tr = parent.Find(name);
        if (!tr)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            tr = go.transform;
        }
        tr.position = pos;
        var probe = GetOrAddComponent<ReflectionProbe>(tr.gameObject);
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        probe.boxProjection = true;
        probe.size = size;
        probe.center = Vector3.zero;
        probe.intensity = intensity;
        probe.blendDistance = 0.8f;
        EditorUtility.SetDirty(probe);
    }

    private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var item)) return item;
        item = profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(item, profile);
        return item;
    }

    [MenuItem("KuTy/Office Intro/Capture Sunset Lighting")]
    public static void Capture()
    {
        var go = new GameObject("TemporaryMoodPreview");
        var camera = go.AddComponent<Camera>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camera.fieldOfView = 68;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 150;

        try
        {
            // 1. Workspace looking toward window
            Shot(camera, new Vector3(-1.1f, 1.65f, 2f), new Vector3(4f, 1.3f, -2f), "OfficeSunset_Workspace.png");
            // 2. Looking out window at skyline & clouds
            Shot(camera, new Vector3(4.5f, 1.65f, -0.4f), new Vector3(17f, 3f, 0f), "OfficeSunset_Window.png");
            // 3. Boss office perspective
            Shot(camera, new Vector3(-4.8f, 1.55f, 1.8f), new Vector3(-4.8f, 1.3f, 4.35f), "OfficeSunset_Boss.png");
            // 4. Seated at Tý's desk
            Shot(camera, new Vector3(4.4f, 1.35f, -2.25f), new Vector3(4.4f, 1.05f, -1.7f), "OfficeSunset_Desk.png");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static void Shot(Camera camera, Vector3 from, Vector3 to, string name)
    {
        camera.transform.position = from;
        camera.transform.LookAt(to);
        var rt = RenderTexture.GetTemporary(1366, 768, 24);
        var previous = RenderTexture.active;
        var tex = new Texture2D(1366, 768, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1366, 768), 0, 0);
            tex.Apply();
            var dir = "Assets/Screenshots";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name), tex.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
