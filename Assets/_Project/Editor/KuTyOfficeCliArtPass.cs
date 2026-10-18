using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Creates a non-destructive visual variant of Scene 1 for Unity CLI batch runs.
/// Gameplay objects and their transforms are intentionally left untouched.
/// </summary>
public static class KuTyOfficeCliArtPass
{
    private const string SourceScenePath = "Assets/_Project/Scenes/Act1_RealWorld.unity";
    private const string OutputScenePath = "Assets/_Project/Scenes/Act1_RealWorld_OfficeArtPass.unity";
    private const string BackupFolder = "Assets/_Project/Scenes/Backups";
    private const string SourceBackupPath = BackupFolder + "/Act1_RealWorld_BeforeOfficeArtPass.unity";
    private const string GeneratedFolder = "Assets/_Project/Generated/OfficeArtPass";
    private const string VolumeProfilePath = GeneratedFolder + "/OfficeArtPassProfile.asset";
    private const string ArtRootName = "OfficeArtPass_CLI";

    [MenuItem("KuTy/CLI/Create Office Art Pass Scene")]
    public static void CreateFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before creating the office art pass.");

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        CreateArtPassScene();
    }

    [MenuItem("KuTy/CLI/Apply Office Art Pass To Scene 1")]
    public static void ApplyToSourceFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before applying the office art pass.");

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder(BackupFolder);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceBackupPath) == null &&
            !AssetDatabase.CopyAsset(SourceScenePath, SourceBackupPath))
            throw new InvalidOperationException("Could not create the Scene 1 safety backup.");

        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        ApplyArtPass(scene);
        Debug.Log("KUTY_OFFICE_ARTPASS_APPLIED: " + SourceScenePath);
    }

    // Entry point used by: unity run . -- -executeMethod KuTyOfficeCliArtPass.CreateBatch -nographics
    public static void CreateBatch()
    {
        try
        {
            CreateArtPassScene();
            Debug.Log("KUTY_OFFICE_CLI_ARTPASS_PASSED: " + OutputScenePath);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void CreateArtPassScene()
    {
        EnsureFolder("Assets/_Project/Generated");
        EnsureFolder(GeneratedFolder);

        Scene scene;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OutputScenePath) == null)
        {
            scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, OutputScenePath, true))
                throw new InvalidOperationException("Could not create the Scene 1 art-pass copy.");
        }

        scene = EditorSceneManager.OpenScene(OutputScenePath, OpenSceneMode.Single);
        ApplyArtPass(scene);
    }

    private static void ApplyArtPass(Scene scene)
    {
        EnsureFolder("Assets/_Project/Generated");
        EnsureFolder(GeneratedFolder);

        GameObject oldRoot = GameObject.Find(ArtRootName);
        if (oldRoot != null)
            UnityEngine.Object.DestroyImmediate(oldRoot);

        GameObject artRoot = new GameObject(ArtRootName);
        GameObject lightingRoot = new GameObject("Lighting");
        lightingRoot.transform.SetParent(artRoot.transform, false);
        GameObject atmosphereRoot = new GameObject("Atmosphere");
        atmosphereRoot.transform.SetParent(artRoot.transform, false);

        ConfigureImportedCameras(scene);
        ConfigureExistingLights(scene);
        Light deskAfternoonLight = BuildCinematicLighting(lightingRoot.transform);
        BuildGameplayAnchors(artRoot.transform, deskAfternoonLight);
        BuildPostProcessing(atmosphereRoot.transform);
        ConfigureEnvironment();
        StyleOfficeHud(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save the configured Scene 1 scene.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = artRoot;
    }

    private static void ConfigureImportedCameras(Scene scene)
    {
        foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
        {
            if (!camera.gameObject.scene.IsValid() || camera.gameObject.scene != scene)
                continue;

            bool isPlayerCamera = camera.transform.root.name == "Player" || camera.CompareTag("MainCamera");
            if (!isPlayerCamera)
                camera.enabled = false;
        }

        Camera playerCamera = GameObject.Find("PlayerCamera")?.GetComponent<Camera>() ?? Camera.main;
        if (playerCamera == null)
            return;

        UniversalAdditionalCameraData cameraData = playerCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = true;
            EditorUtility.SetDirty(cameraData);
        }

        playerCamera.clearFlags = CameraClearFlags.Skybox;
        EditorUtility.SetDirty(playerCamera);
    }

    private static void ConfigureExistingLights(Scene scene)
    {
        Light[] sceneLights = Resources.FindObjectsOfTypeAll<Light>()
            .Where(light => light.gameObject.scene.IsValid() && light.gameObject.scene == scene)
            .ToArray();

        // Older setup passes accidentally created several copies behind the boss glass.
        // Keep the correctly positioned primary and disable only the numbered duplicates.
        foreach (Light light in sceneLights.Where(light => light.name.StartsWith("BossGlassInteriorGlow (", StringComparison.Ordinal)))
            light.enabled = false;

        Light bossGlow = sceneLights.FirstOrDefault(light => light.name == "BossGlassInteriorGlow");
        if (bossGlow != null)
        {
            bossGlow.enabled = true;
            bossGlow.type = LightType.Point;
            bossGlow.color = new Color(.46f, .64f, 1f);
            bossGlow.intensity = .42f;
            bossGlow.range = 4.5f;
            bossGlow.shadows = LightShadows.None;
            EditorUtility.SetDirty(bossGlow);
        }

        Light sun = sceneLights.FirstOrDefault(light => light.name == "Sun");
        if (sun != null)
        {
            sun.color = new Color(.55f, .67f, .9f);
            sun.intensity = .28f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            EditorUtility.SetDirty(sun);
        }

        foreach (Light light in sceneLights.Where(light => light.name.StartsWith("OfficeFluorescentLight_", StringComparison.Ordinal)))
        {
            light.color = new Color(.66f, .78f, 1f);
            light.intensity = .42f;
            light.range = 4.8f;
            light.shadows = LightShadows.None;
            EditorUtility.SetDirty(light);
        }

        // Replaced by Desk_Afternoon_Key below. Keeping both would overexpose the desk.
        Light oldDeskKey = sceneLights.FirstOrDefault(light => light.name == "PlayerDeskAfternoonLight");
        if (oldDeskKey != null)
        {
            oldDeskKey.enabled = false;
            EditorUtility.SetDirty(oldDeskKey);
        }

        // The imported boss-office FBX contains its own presentation light. It clashes with the scene setup.
        Transform importedBossLight = GameObject.Find("boss_office")?.transform.Find("Light");
        Light importedLight = importedBossLight != null ? importedBossLight.GetComponent<Light>() : null;
        if (importedLight != null)
            importedLight.enabled = false;
    }

    private static Light BuildCinematicLighting(Transform parent)
    {
        Transform desk = FindFirst("OfficeDesk_New", "OfficeDesk", "CHOT_desk");
        Transform boss = FindFirst("BossSilhouette", "boss_office");
        Transform player = FindFirst("Player");

        Vector3 deskPoint = desk != null ? BoundsCenter(desk.gameObject) : new Vector3(0f, 1.05f, 0f);
        Vector3 bossPoint = boss != null ? BoundsCenter(boss.gameObject) : new Vector3(0f, 1.5f, 4f);
        Vector3 playerPoint = player != null ? player.position : new Vector3(0f, 1f, -2f);

        Light windowKey = CreateSpot("Desk_Afternoon_Key", parent,
            deskPoint + new Vector3(-3.4f, 3.7f, 2.8f), deskPoint + Vector3.up * .35f,
            new Color(1f, .58f, .28f), 2.6f, 9f, 58f, LightShadows.Soft);
        windowKey.innerSpotAngle = 32f;

        Light deskRim = CreateSpot("Desk_Cold_Rim", parent,
            deskPoint + new Vector3(1.8f, 2.8f, -1.4f), deskPoint + Vector3.up * .45f,
            new Color(.42f, .62f, 1f), 1.05f, 6f, 52f, LightShadows.None);
        deskRim.innerSpotAngle = 25f;

        Light bossRim = CreateSpot("Boss_Silhouette_Backlight", parent,
            bossPoint + new Vector3(0f, 1.8f, 1.2f), bossPoint + Vector3.up * .65f,
            new Color(.42f, .68f, 1f), .9f, 6.5f, 46f, LightShadows.Soft);
        bossRim.innerSpotAngle = 24f;

        GameObject fillObject = new GameObject("Office_Cold_Fill");
        fillObject.transform.SetParent(parent, false);
        fillObject.transform.position = Vector3.Lerp(playerPoint, deskPoint, .45f) + Vector3.up * 2.1f;
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(.38f, .52f, .78f);
        fill.intensity = .32f;
        fill.range = 7.5f;
        fill.shadows = LightShadows.None;
        fill.lightmapBakeType = LightmapBakeType.Realtime;
        return windowKey;
    }

    private static void BuildGameplayAnchors(Transform parent, Light deskAfternoonLight)
    {
        GameObject anchorRootObject = new GameObject("GameplayAnchors");
        anchorRootObject.transform.SetParent(parent, false);

        Transform chair = GameObject.Find("office-chair-main-character")?.transform;
        Transform desk = GameObject.Find("OfficeRoot/Workstation/OfficeDesk/OfficeDesk_New")?.transform;
        Transform oldCollapseTarget = GameObject.Find(
            "OfficeRoot/Workstation/OfficeDesk/OfficeDesk_New/DeskCollapseTarget")?.transform;

        Transform seatAnchor = new GameObject("PlayerSeatAnchor").transform;
        seatAnchor.SetParent(anchorRootObject.transform, false);
        if (chair != null)
            seatAnchor.position = chair.position;

        Vector3 seatForward = Vector3.forward;
        if (chair != null && desk != null)
        {
            seatForward = Vector3.ProjectOnPlane(BoundsCenter(desk.gameObject) - chair.position, Vector3.up);
            if (seatForward.sqrMagnitude < .001f)
                seatForward = Vector3.forward;
        }
        seatAnchor.rotation = Quaternion.LookRotation(seatForward.normalized, Vector3.up);

        Transform collapseAnchor = new GameObject("DeskCollapseAnchor").transform;
        collapseAnchor.SetParent(anchorRootObject.transform, false);
        if (oldCollapseTarget != null)
            collapseAnchor.SetPositionAndRotation(oldCollapseTarget.position, oldCollapseTarget.rotation);
        else if (desk != null)
        {
            Vector3 deskCenter = BoundsCenter(desk.gameObject);
            collapseAnchor.position = deskCenter + new Vector3(-.16f, .28f, .36f);
            collapseAnchor.rotation = Quaternion.Euler(72f, seatAnchor.eulerAngles.y, 8f);
        }

        OfficeSceneController controller = UnityEngine.Object.FindObjectOfType<OfficeSceneController>(true);
        if (controller != null)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("chairAnchor").objectReferenceValue = seatAnchor;
            serializedController.FindProperty("afternoonLight").objectReferenceValue = deskAfternoonLight;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        OfficeExhaustionSequence exhaustion = UnityEngine.Object.FindObjectOfType<OfficeExhaustionSequence>(true);
        if (exhaustion != null)
        {
            SerializedObject serializedExhaustion = new SerializedObject(exhaustion);
            serializedExhaustion.FindProperty("deskCollapseTarget").objectReferenceValue = collapseAnchor;
            serializedExhaustion.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(exhaustion);
        }

        Volume moodVolume = GameObject.Find("OfficeMoodVolume")?.GetComponent<Volume>();
        if (moodVolume != null)
        {
            moodVolume.priority = 50f;
            moodVolume.weight = 1f;
            EditorUtility.SetDirty(moodVolume);
        }
    }

    private static void BuildPostProcessing(Transform parent)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        foreach (VolumeComponent component in profile.components.ToArray())
            profile.Remove(component.GetType());

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(-.2f);
        color.contrast.Override(11f);
        color.saturation.Override(-12f);
        color.colorFilter.Override(new Color(.9f, .94f, 1f));

        WhiteBalance balance = profile.Add<WhiteBalance>(true);
        balance.temperature.Override(-7f);
        balance.tint.Override(-2f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.color.Override(new Color(.018f, .028f, .055f));
        vignette.intensity.Override(.22f);
        vignette.smoothness.Override(.46f);
        vignette.rounded.Override(true);

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(.18f);
        bloom.threshold.Override(1.15f);
        bloom.scatter.Override(.55f);

        EditorUtility.SetDirty(profile);

        GameObject volumeObject = new GameObject("OfficeArtPassVolume");
        volumeObject.transform.SetParent(parent, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 25f;
        volume.weight = 1f;
        volume.sharedProfile = profile;
    }

    private static void ConfigureEnvironment()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.12f, .16f, .23f);
        RenderSettings.reflectionIntensity = .55f;
        RenderSettings.fog = false;
    }

    private static void StyleOfficeHud(Scene scene)
    {
        TextMeshProUGUI objective = FindSceneComponent<TextMeshProUGUI>(scene, "OfficeObjective");
        if (objective != null)
        {
            objective.fontSize = 23f;
            objective.fontStyle = FontStyles.Bold;
            objective.color = new Color(.91f, .95f, 1f, 1f);
            objective.alignment = TextAlignmentOptions.Left;
            objective.enableWordWrapping = true;
            EditorUtility.SetDirty(objective);
        }

        TextMeshProUGUI subtitle = FindSceneComponent<TextMeshProUGUI>(scene, "OfficeSubtitle");
        if (subtitle != null)
        {
            subtitle.fontSize = 27f;
            subtitle.fontStyle = FontStyles.Normal;
            subtitle.color = new Color(.97f, .98f, 1f, 1f);
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.enableWordWrapping = true;
            EditorUtility.SetDirty(subtitle);
        }

        StylePanel(scene, "OfficeObjectivePanel", new Color(.025f, .045f, .085f, .68f));
        StylePanel(scene, "OfficeSubtitlePanel", new Color(.018f, .026f, .052f, .78f));
    }

    private static void StylePanel(Scene scene, string objectName, Color color)
    {
        Image image = FindSceneComponent<Image>(scene, objectName);
        if (image == null)
            return;

        image.color = color;
        image.raycastTarget = false;
        EditorUtility.SetDirty(image);
    }

    private static T FindSceneComponent<T>(Scene scene, string objectName) where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .FirstOrDefault(component => component.gameObject.scene == scene && component.name == objectName);
    }

    private static Light CreateSpot(string name, Transform parent, Vector3 position, Vector3 target,
        Color color, float intensity, float range, float angle, LightShadows shadows)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        lightObject.transform.rotation = Quaternion.LookRotation((target - position).normalized, Vector3.up);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = angle;
        light.shadows = shadows;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        return light;
    }

    private static Transform FindFirst(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
                return found.transform;
        }

        return null;
    }

    private static Vector3 BoundsCenter(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return target.transform.position;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds.center;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        string name = path.Substring(separator + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
