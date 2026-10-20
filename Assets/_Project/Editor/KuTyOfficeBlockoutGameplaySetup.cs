using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Installs the Excel-only office work flow into the existing Blockout scene while
/// preserving every environment root, imported visual and authored light.
/// </summary>
public static class KuTyOfficeBlockoutGameplaySetup
{
    private const string TargetScenePath = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    private const string SourceScenePath = "Assets/_Project/Scenes/Act1_RealWorld_OfficeArtPass.unity";
    private const string GeneratedPath = "Assets/_Project/Generated";
    private const string SystemsName = "OfficeGameplaySystems";
    private const string UiName = "OfficeGameplayUI";
    private const string EventSystemName = "OfficeGameplayEventSystem";

    [MenuItem("KuTy/Setup/Install Act1 Office Blockout gameplay")]
    public static void Build()
    {
        Scene targetScene = EditorSceneManager.GetActiveScene();
        if (targetScene.path != TargetScenePath)
        {
            Debug.LogError("[KuTy] Open Act1_Office_Blockout before installing office gameplay.");
            return;
        }

        EditorSceneManager.SaveScene(targetScene);
        WriteSafetyBackup();

        OfficeIntroController intro = FindInScene<OfficeIntroController>(targetScene);
        FirstPersonController motor = FindInScene<FirstPersonController>(targetScene);
        GameObject player = motor != null ? motor.gameObject : null;
        Camera playerCamera = motor != null ? motor.GetComponentInChildren<Camera>(true) : null;
        Transform seat = FindByPath(targetScene, "OFFICE_BLOCKOUT/03_Workstations/TySeatAnchor")?.transform;
        GameObject chairSlot = FindByPath(targetScene, "OFFICE_BLOCKOUT/03_Workstations/TyChair_SLOT");
        GameObject deskSlot = FindByPath(targetScene, "OFFICE_BLOCKOUT/03_Workstations/TyDesk_SLOT");
        GameObject doorHinge = FindByPath(targetScene, "OFFICE_BLOCKOUT/02_BossOffice/BossDoor_Hinge");
        GameObject bossSlot = FindByPath(targetScene, "OFFICE_BLOCKOUT/02_BossOffice/BossPlaceholder_SLOT");
        GameObject printerSlot = FindByPath(targetScene, "OFFICE_BLOCKOUT/04_UtilityArea/Printer_SLOT");
        GameObject clockSlot = FindByPath(targetScene, "OFFICE_BLOCKOUT/04_UtilityArea/Clock_SLOT");
        GameObject waterSlot = FindByPath(targetScene, "OFFICE_BLOCKOUT/04_UtilityArea/WaterCooler_SLOT");
        GameObject windowCollision = FindByPath(targetScene, "OFFICE_BLOCKOUT/01_Architecture/WindowCollision");

        if (intro == null || player == null || playerCamera == null || seat == null || chairSlot == null ||
            deskSlot == null || doorHinge == null || bossSlot == null || printerSlot == null ||
            clockSlot == null || waterSlot == null || windowCollision == null)
        {
            Debug.LogError("[KuTy] Blockout anchors are incomplete. Gameplay was not installed.");
            return;
        }

        RemoveGeneratedRoots(targetScene);
        RemoveGeneratedComponents(player, doorHinge, chairSlot, bossSlot, printerSlot, clockSlot, waterSlot, windowCollision);

        Scene sourceScene = GetOrOpenSourceScene();
        if (!sourceScene.IsValid() || !sourceScene.isLoaded)
        {
            Debug.LogError("[KuTy] Could not load the OfficeArtPass reference scene.");
            return;
        }

        GameObject sourceUi = sourceScene.GetRootGameObjects().FirstOrDefault(root => root.name == "UI");
        GameObject sourceEventSystem = sourceScene.GetRootGameObjects().FirstOrDefault(root => root.name == "EventSystem");
        if (sourceUi == null)
        {
            Debug.LogError("[KuTy] The OfficeArtPass gameplay Canvas is missing.");
            CloseReferenceScene(sourceScene);
            return;
        }

        GameObject gameplayUi = UnityEngine.Object.Instantiate(sourceUi);
        gameplayUi.name = UiName;
        SceneManager.MoveGameObjectToScene(gameplayUi, targetScene);
        PrepareGameplayUi(gameplayUi);

        if (FindInScene<EventSystem>(targetScene) == null && sourceEventSystem != null)
        {
            GameObject eventSystem = UnityEngine.Object.Instantiate(sourceEventSystem);
            eventSystem.name = EventSystemName;
            SceneManager.MoveGameObjectToScene(eventSystem, targetScene);
        }

        CloseReferenceScene(sourceScene);

        GameObject systems = new GameObject(SystemsName);
        SceneManager.MoveGameObjectToScene(systems, targetScene);
        GameObject targets = new GameObject("InteractionTargets");
        targets.transform.SetParent(systems.transform, false);

        OfficeSceneController office = systems.AddComponent<OfficeSceneController>();
        systems.AddComponent<OfficeShiftHUD>();
        OfficeAtmosphere atmosphere = systems.AddComponent<OfficeAtmosphere>();
        OfficeExhaustionSequence exhaustion = systems.AddComponent<OfficeExhaustionSequence>();
        OfficeBlockoutGameplayBridge bridge = systems.AddComponent<OfficeBlockoutGameplayBridge>();

        ExcelDataAuditorGame excelMiniGame = gameplayUi.GetComponentInChildren<ExcelDataAuditorGame>(true);
        HUDController hud = gameplayUi.GetComponent<HUDController>();
        PauseUI pause = gameplayUi.GetComponent<PauseUI>();
        TextMeshProUGUI objective = gameplayUi.transform.Find("OfficeObjectivePanel/OfficeObjective")?.GetComponent<TextMeshProUGUI>();
        Transform subtitlePanelTransform = gameplayUi.transform.Find("OfficeSubtitlePanel");
        TextMeshProUGUI subtitle = subtitlePanelTransform?.Find("OfficeSubtitle")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI controlsHint = gameplayUi.GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(item => item.text != null && item.text.Contains("WASD"));
        Image blackout = gameplayUi.transform.Find("OfficeExhaustionFade")?.GetComponent<Image>();

        if (excelMiniGame == null || hud == null || pause == null || objective == null || controlsHint == null ||
            subtitlePanelTransform == null || subtitle == null || blackout == null)
        {
            Debug.LogError("[KuTy] Cloned gameplay UI is incomplete. Check the OfficeArtPass reference scene.");
            return;
        }

        objective.text = string.Empty;
        subtitle.text = string.Empty;
        subtitlePanelTransform.gameObject.SetActive(false);
        ConfigureGameplayHud(objective, subtitle, subtitlePanelTransform, controlsHint);
        EnsureVietnameseGlyphs(subtitle.font);
        blackout.color = new Color(0f, 0f, 0f, 0f);
        blackout.raycastTarget = false;

        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction == null) interaction = player.AddComponent<PlayerInteraction>();
        SetObject(interaction, "raycastOrigin", playerCamera.transform);
        SetInt(interaction, "interactableLayer", LayerMask.GetMask("Interactable"));
        SetFloat(interaction, "interactDistance", 3.2f);

        ConfigureAtmosphere(systems.transform, atmosphere);
        Volume moodVolume = CreateMoodVolume(systems.transform);
        Transform collapseAnchor = CreateCollapseAnchor(systems.transform, deskSlot, seat);

        Bounds deskBounds = BoundsOf(deskSlot, new Bounds(deskSlot.transform.position + Vector3.up, new Vector3(1.8f, 1.2f, 1f)));
        Renderer monitor = FindNearestRenderer(targetScene, "PF_Office_Monitor_CRT_04", deskBounds.center);
        Renderer coffee = FindNearestRenderer(targetScene, "PF_Office_CoffeeMug_02", deskBounds.center);

        SetObject(exhaustion, "cameraTransform", playerCamera.transform);
        SetObject(exhaustion, "deskCollapseTarget", collapseAnchor);
        SetObject(exhaustion, "blackout", blackout);
        SetObject(exhaustion, "moodVolume", moodVolume);
        SetObject(exhaustion, "atmosphere", atmosphere);

        Light afternoon = FindByPath(targetScene, "OFFICE_BLOCKOUT/06_Lighting/WindowSunsetBounce")?.GetComponent<Light>();
        SetObject(office, "objectiveText", objective);
        SetObject(office, "subtitleText", subtitle);
        SetObject(office, "controlsHintText", controlsHint);
        SetObject(office, "subtitlePanel", subtitlePanelTransform.gameObject);
        SetObject(office, "screenWarning", null);
        SetObject(office, "clockText", null);
        SetObject(office, "atmosphere", atmosphere);
        SetObject(office, "excelMiniGame", excelMiniGame);
        SetObject(office, "openingSequence", null);
        SetObject(office, "exhaustionSequence", exhaustion);
        SetObject(office, "afternoonLight", afternoon);
        SetObject(office, "playerTransform", player.transform);
        SetObject(office, "chairAnchor", seat);
        SetBool(office, "openingControlledExternally", true);
        SetFloat(office, "seatedCamY", intro.seatedEyeHeight);

        SetObject(bridge, "intro", intro);
        SetObject(bridge, "office", office);
        SetObject(bridge, "playerInteraction", interaction);
        SetObject(bridge, "pauseUI", pause);
        SetBool(intro, "useExternalInteractionInput", true);

        ConfigureDoor(doorHinge, intro);
        ConfigureChair(chairSlot, intro, office);
        ConfigureOfficeInteraction(windowCollision, office, OfficeInteractionKind.Window);
        ConfigureOfficeInteraction(bossSlot, office, OfficeInteractionKind.Boss);
        ConfigureMicro(printerSlot, office, OfficeMicroInteractionKind.PrinterPaper, true, 0f);
        ConfigureMicro(clockSlot, office, OfficeMicroInteractionKind.Clock, false, 1f);
        ConfigureMicro(waterSlot, office, OfficeMicroInteractionKind.WaterCooler, false, 2.5f);

        Bounds monitorBounds = monitor != null ? monitor.bounds : new Bounds(deskBounds.center + Vector3.up * .45f, new Vector3(.65f, .5f, .15f));
        GameObject workstationTarget = CreateProxyTarget("TyWorkstationInteraction", targets.transform,
            monitorBounds.center, ClampSize(monitorBounds.size, new Vector3(.5f, .38f, .12f), new Vector3(1.1f, .85f, .28f)));
        OfficeWorkstationInteractable workstation = workstationTarget.AddComponent<OfficeWorkstationInteractable>();
        SetObject(workstation, "office", office);

        Bounds coffeeBounds = coffee != null ? coffee.bounds : new Bounds(deskBounds.center + Vector3.up * .55f, Vector3.one * .18f);
        GameObject coffeeTarget = CreateProxyTarget("ColdCoffeeInteraction", targets.transform,
            coffeeBounds.center, ClampSize(coffeeBounds.size * 1.4f, Vector3.one * .16f, Vector3.one * .32f));
        ConfigureMicro(coffeeTarget, office, OfficeMicroInteractionKind.ColdCoffee, false, 2.5f);

        UniversalAdditionalCameraData cameraData = playerCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        EditorUtility.SetDirty(cameraData);

        UpdateBuildSettings();
        EditorUtility.SetDirty(intro);
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(gameplayUi);
        EditorUtility.SetDirty(systems);
        EditorSceneManager.MarkSceneDirty(targetScene);
        EditorSceneManager.SaveScene(targetScene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = systems;
        Debug.Log("[KuTy] Act1 Office Blockout gameplay installed. Existing map, imported assets and lighting were preserved.");
    }

    [MenuItem("KuTy/Validation/Validate Act1 Office Blockout gameplay")]
    public static void Validate()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        List<string> missing = new List<string>();
        Require(FindInScene<OfficeIntroController>(scene), "OfficeIntroController", missing);
        Require(FindInScene<OfficeBlockoutGameplayBridge>(scene), "OfficeBlockoutGameplayBridge", missing);
        Require(FindInScene<OfficeSceneController>(scene), "OfficeSceneController", missing);
        Require(FindInScene<PlayerInteraction>(scene), "PlayerInteraction", missing);
        Require(FindInScene<HUDController>(scene), "HUDController", missing);
        Require(FindInScene<ExcelDataAuditorGame>(scene), "ExcelDataAuditorGame", missing);
        OfficeReportMiniGame legacyReport = FindInScene<OfficeReportMiniGame>(scene);
        if (legacyReport != null && legacyReport.enabled) missing.Add("Legacy OfficeReportMiniGame must stay disabled");
        Require(FindInScene<OfficeExhaustionSequence>(scene), "OfficeExhaustionSequence", missing);
        Require(FindByPath(scene, "OFFICE_BLOCKOUT/03_Workstations/TyDesk_SLOT"), "TyDesk_SLOT", missing);
        Require(FindByPath(scene, "OFFICE_BLOCKOUT/03_Workstations/TyChair_SLOT"), "TyChair_SLOT", missing);
        Require(scene.GetRootGameObjects().FirstOrDefault(root => root.name == SystemsName), SystemsName, missing);
        Require(scene.GetRootGameObjects().FirstOrDefault(root => root.name == UiName), UiName, missing);
        Require(FindInScene<EventSystem>(scene), "EventSystem", missing);
        if (missing.Count > 0) Debug.LogError("[KuTy] Office Blockout gameplay validation failed: " + string.Join(", ", missing));
        else Debug.Log("[KuTy] Office Blockout gameplay validation passed: Excel-only flow, UI, interactions and ending are wired.");
    }

    private static void PrepareGameplayUi(GameObject ui)
    {
        DestroyComponent<DialogueUI>(ui);
        DestroyComponent<StoryPrologueController>(ui);
        DestroyComponent<ProgressBarUI>(ui);

        foreach (string child in new[] { "Memory0", "Memory1", "Memory2", "DialoguePanel", "PrologueOverlay", "OfficeOpeningFade" })
        {
            Transform item = ui.transform.Find(child);
            if (item != null) item.gameObject.SetActive(false);
        }

        HUDController hud = ui.GetComponent<HUDController>();
        if (hud != null)
        {
            SerializedObject serialized = new SerializedObject(hud);
            HideReferencedGraphic(serialized.FindProperty("moneyText"));
            HideReferencedGraphic(serialized.FindProperty("objectiveText"));
            SerializedProperty icons = serialized.FindProperty("memoryIcons");
            if (icons != null && icons.isArray)
                for (int i = 0; i < icons.arraySize; i++) HideReferencedGraphic(icons.GetArrayElementAtIndex(i));
        }

        Canvas canvas = ui.GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 20;
        Transform legacyReportOverlay = ui.transform.Find("ReportMiniGameOverlay");
        if (legacyReportOverlay != null) UnityEngine.Object.DestroyImmediate(legacyReportOverlay.gameObject);
        DestroyComponent<OfficeReportMiniGame>(ui);
        ui.transform.Find("ExcelMiniGamePanel")?.gameObject.SetActive(false);
        ui.transform.Find("BossChatPopup")?.gameObject.SetActive(false);
        ui.transform.Find("Pause")?.gameObject.SetActive(false);
    }

    private static void ConfigureGameplayHud(TextMeshProUGUI objective, TextMeshProUGUI subtitle,
        Transform subtitlePanel, TextMeshProUGUI controlsHint)
    {
        objective.richText = true;
        objective.enableAutoSizing = true;
        objective.fontSizeMin = 17f;
        objective.fontSizeMax = 23f;
        CanvasGroup objectiveGroup = objective.GetComponentInParent<CanvasGroup>();
        if (objectiveGroup != null)
        {
            objectiveGroup.alpha = 1f;
            objectiveGroup.interactable = false;
            objectiveGroup.blocksRaycasts = false;
        }

        subtitle.richText = true;
        subtitle.enableAutoSizing = true;
        subtitle.fontSizeMin = 20f;
        subtitle.fontSizeMax = 28f;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.lineSpacing = 6f;

        RectTransform panelRect = subtitlePanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(.5f, 0f);
            panelRect.anchorMax = new Vector2(.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 154f);
            panelRect.sizeDelta = new Vector2(1120f, 126f);
        }
        Image panelImage = subtitlePanel.GetComponent<Image>();
        if (panelImage != null) panelImage.color = new Color(.018f, .028f, .055f, .9f);

        controlsHint.richText = true;
        controlsHint.enableWordWrapping = false;
        controlsHint.enableAutoSizing = true;
        controlsHint.fontSizeMin = 14f;
        controlsHint.fontSizeMax = 18f;
        controlsHint.alignment = TextAlignmentOptions.Center;
        controlsHint.text =
            "<color=#F4C66A><b>WASD</b></color> <color=#DCE5EC>Di chuyển</color>" +
            "  <color=#617387>•</color>  <color=#F4C66A><b>CHUỘT</b></color> <color=#DCE5EC>Quan sát</color>" +
            "  <color=#617387>•</color>  <color=#F4C66A><b>E</b></color> <color=#DCE5EC>Tương tác</color>" +
            "  <color=#617387>•</color>  <color=#F4C66A><b>ESC</b></color> <color=#DCE5EC>Tạm dừng</color>";
        RectTransform controlsRect = controlsHint.rectTransform;
        controlsRect.anchorMin = new Vector2(.5f, 0f);
        controlsRect.anchorMax = new Vector2(.5f, 0f);
        controlsRect.anchoredPosition = new Vector2(0f, 30f);
        controlsRect.sizeDelta = new Vector2(1180f, 40f);
        Shadow shadow = controlsHint.GetComponent<Shadow>();
        if (shadow == null) shadow = controlsHint.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, .8f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private static void EnsureVietnameseGlyphs(TMP_FontAsset font)
    {
        if (font == null) throw new MissingReferenceException("Vietnamese TMP font is missing from the office HUD.");
        const string sample = "ÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬĐÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴ" +
            "áàảãạăắằẳẵặâấầẩẫậđéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵ";
        if (!font.HasCharacters(sample, out uint[] missing, true, true))
            throw new System.InvalidOperationException("Vietnamese TMP glyphs are missing: " + string.Join(",", missing));
        EditorUtility.SetDirty(font);
    }

    private static void ConfigureAtmosphere(Transform parent, OfficeAtmosphere atmosphere)
    {
        GameObject audioRoot = new GameObject("GameplayAudio");
        audioRoot.transform.SetParent(parent, false);
        AudioSource keyboard = CreateAudio("KeyboardFeedback", audioRoot.transform, .2f);
        AudioSource boss = CreateAudio("BossReminder", audioRoot.transform, .5f);
        AudioSource props = CreateAudio("PropFeedback", audioRoot.transform, .55f);
        AudioSource memory = CreateAudio("MemoryBridge", audioRoot.transform, .5f);
        SetObject(atmosphere, "streetSource", null);
        SetObject(atmosphere, "fluorescentSource", null);
        SetObject(atmosphere, "keyboardSource", keyboard);
        SetObject(atmosphere, "bossSource", boss);
        SetObject(atmosphere, "propSource", props);
        SetObject(atmosphere, "memorySource", memory);
        SetObjectArray(atmosphere, "ceilingLights", new Light[0]);
        SetBool(atmosphere, "deferAmbienceToOpening", false);
    }

    private static AudioSource CreateAudio(string name, Transform parent, float volume)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.volume = volume;
        source.spatialBlend = 0f;
        return source;
    }

    private static Volume CreateMoodVolume(Transform parent)
    {
        EnsureGeneratedFolder();
        string profilePath = GeneratedPath + "/OfficeBlockoutGameplayMood.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(.2f);
        vignette.smoothness.Override(.32f);
        if (!profile.TryGet(out ColorAdjustments color)) color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.saturation.Override(-12f);
        color.postExposure.Override(-.12f);
        color.colorFilter.Override(new Color(.88f, .93f, 1f));
        if (!profile.TryGet(out DepthOfField depth)) depth = profile.Add<DepthOfField>(false);
        depth.active = false;
        depth.mode.Override(DepthOfFieldMode.Gaussian);
        EditorUtility.SetDirty(profile);

        GameObject go = new GameObject("OfficeGameplayMoodVolume");
        go.transform.SetParent(parent, false);
        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 30f;
        volume.sharedProfile = profile;
        return volume;
    }

    private static Transform CreateCollapseAnchor(Transform parent, GameObject desk, Transform seat)
    {
        Bounds bounds = BoundsOf(desk, new Bounds(desk.transform.position + Vector3.up, new Vector3(1.8f, 1.2f, 1f)));
        Vector3 towardDesk = Vector3.ProjectOnPlane(bounds.center - seat.position, Vector3.up).normalized;
        if (towardDesk.sqrMagnitude < .01f) towardDesk = desk.transform.forward;
        float horizontalExtent = Mathf.Max(.25f, Vector3.Dot(bounds.extents, Abs(towardDesk)));
        Vector3 position = bounds.center - towardDesk * horizontalExtent * .35f;
        position.y = bounds.max.y + .12f;
        Vector3 look = (towardDesk * .35f + Vector3.down).normalized;
        GameObject anchor = new GameObject("DeskCollapseAnchor");
        anchor.transform.SetParent(parent, false);
        anchor.transform.SetPositionAndRotation(position, Quaternion.LookRotation(look, Vector3.up) * Quaternion.Euler(0f, 0f, 8f));
        return anchor.transform;
    }

    private static void ConfigureDoor(GameObject door, OfficeIntroController intro)
    {
        OfficeBlockoutDoorInteractable adapter = door.AddComponent<OfficeBlockoutDoorInteractable>();
        SetObject(adapter, "intro", intro);
        SetColliderLayers(door, LayerMask.NameToLayer("Interactable"));
    }

    private static void ConfigureChair(GameObject chair, OfficeIntroController intro, OfficeSceneController office)
    {
        OfficeBlockoutChairInteractable adapter = chair.AddComponent<OfficeBlockoutChairInteractable>();
        SetObject(adapter, "intro", intro);
        SetObject(adapter, "office", office);
        SetColliderLayers(chair, LayerMask.NameToLayer("Interactable"));
    }

    private static void ConfigureOfficeInteraction(GameObject target, OfficeSceneController office, OfficeInteractionKind kind)
    {
        OfficeInteractable interaction = target.AddComponent<OfficeInteractable>();
        SetObject(interaction, "office", office);
        SetEnum(interaction, "kind", (int)kind);
        SetColliderLayers(target, LayerMask.NameToLayer("Interactable"));
    }

    private static void ConfigureMicro(GameObject target, OfficeSceneController office, OfficeMicroInteractionKind kind,
        bool oneTime, float cooldown)
    {
        OfficeMicroInteractable interaction = target.AddComponent<OfficeMicroInteractable>();
        SetObject(interaction, "office", office);
        SetEnum(interaction, "kind", (int)kind);
        SetBool(interaction, "isOneTimeUse", oneTime);
        SetFloat(interaction, "cooldownSeconds", cooldown);
        SetColliderLayers(target, LayerMask.NameToLayer("Interactable"));
    }

    private static GameObject CreateProxyTarget(string name, Transform parent, Vector3 position, Vector3 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.layer = LayerMask.NameToLayer("Interactable");
        BoxCollider collider = go.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;
        return go;
    }

    private static void RemoveGeneratedRoots(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == SystemsName || root.name == UiName || root.name == EventSystemName)
                UnityEngine.Object.DestroyImmediate(root);
    }

    private static void RemoveGeneratedComponents(params GameObject[] objects)
    {
        foreach (GameObject go in objects)
        {
            if (go == null) continue;
            foreach (MonoBehaviour component in go.GetComponents<MonoBehaviour>())
                if (component is PlayerInteraction || component is OfficeBlockoutDoorInteractable ||
                    component is OfficeBlockoutChairInteractable || component is OfficeInteractable ||
                    component is OfficeMicroInteractable)
                    UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static void SetColliderLayers(GameObject root, int layer)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true)) collider.gameObject.layer = layer;
    }

    private static Renderer FindNearestRenderer(Scene scene, string rootNamePrefix, Vector3 point)
    {
        Renderer best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (!root.name.StartsWith(rootNamePrefix)) continue;
            Renderer renderer = root.GetComponentInChildren<Renderer>(true);
            if (renderer == null) continue;
            float distance = (renderer.bounds.center - point).sqrMagnitude;
            if (distance >= bestDistance) continue;
            best = renderer;
            bestDistance = distance;
        }
        return best;
    }

    private static Bounds BoundsOf(GameObject root, Bounds fallback)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return fallback;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Vector3 ClampSize(Vector3 value, Vector3 min, Vector3 max) => new Vector3(
        Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y), Mathf.Clamp(value.z, min.z, max.z));

    private static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

    private static Scene GetOrOpenSourceScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loaded = SceneManager.GetSceneAt(i);
            if (loaded.path == SourceScenePath) return loaded;
        }
        return EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
    }

    private static void CloseReferenceScene(Scene source)
    {
        if (source.IsValid() && source.isLoaded && source.path == SourceScenePath)
            EditorSceneManager.CloseScene(source, true);
    }

    private static GameObject FindByPath(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        GameObject current = scene.GetRootGameObjects().FirstOrDefault(root => root.name == parts[0]);
        if (current == null) return null;
        for (int i = 1; i < parts.Length; i++)
        {
            Transform child = current.transform.Find(parts[i]);
            if (child == null) return null;
            current = child.gameObject;
        }
        return current;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    private static void UpdateBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        int oldIndex = scenes.FindIndex(item => item.path == "Assets/_Project/Scenes/Act1_RealWorld.unity");
        int targetIndex = scenes.FindIndex(item => item.path == TargetScenePath);
        if (targetIndex >= 0) scenes[targetIndex] = new EditorBuildSettingsScene(TargetScenePath, true);
        else if (oldIndex >= 0) scenes[oldIndex] = new EditorBuildSettingsScene(TargetScenePath, true);
        else scenes.Insert(Mathf.Min(1, scenes.Count), new EditorBuildSettingsScene(TargetScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void WriteSafetyBackup()
    {
        string directory = Path.Combine("Temp", "KuTyBackups");
        Directory.CreateDirectory(directory);
        string destination = Path.Combine(directory, "Act1_Office_Blockout_before_gameplay_" +
            System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
        File.Copy(TargetScenePath, destination, true);
    }

    private static void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedPath)) AssetDatabase.CreateFolder("Assets/_Project", "Generated");
    }

    private static void HideReferencedGraphic(SerializedProperty property)
    {
        Component component = property != null ? property.objectReferenceValue as Component : null;
        if (component != null) component.gameObject.SetActive(false);
    }

    private static void DestroyComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component != null) UnityEngine.Object.DestroyImmediate(component);
    }

    private static void Require(UnityEngine.Object value, string label, List<string> missing)
    {
        if (value == null) missing.Add(label);
    }

    private static void SetObject(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null) throw new MissingReferenceException(target.GetType().Name + "." + property);
        field.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectArray<T>(UnityEngine.Object target, string property, T[] values) where T : UnityEngine.Object
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty field = serialized.FindProperty(property);
        field.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(UnityEngine.Object target, string property, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(UnityEngine.Object target, string property, float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(UnityEngine.Object target, string property, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(UnityEngine.Object target, string property, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).enumValueIndex = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
