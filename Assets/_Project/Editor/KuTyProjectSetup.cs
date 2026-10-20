using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Creates an editable greybox, real prefab references and a complete playable loop.</summary>
public static class KuTyProjectSetup
{
    private const string Root = "Assets/_Project";
    private const string Generated = Root + "/Generated";
    private static TMP_FontAsset font;
    private static Material ground, wood, blue, clean, dirty, red;
    private static GameObject slotPrefab, playerPrefab;
    private static bool batchSetup;
    public static void GenerateBatch()
    {
        batchSetup = true;
        try { Generate(); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }
    private static void ResourcesReady(string packageName)
    {
        AssetDatabase.importPackageCompleted -= ResourcesReady;
        AssetDatabase.importPackageFailed -= ResourcesFailed;
        EditorApplication.delayCall += () => { if (batchSetup) GenerateBatch(); else Generate(); };
    }
    private static void ResourcesFailed(string packageName, string error)
    {
        AssetDatabase.importPackageCompleted -= ResourcesReady;
        AssetDatabase.importPackageFailed -= ResourcesFailed;
        Debug.LogError(error);
        if (batchSetup) EditorApplication.Exit(1);
    }
    private static readonly string[] Scenes = { SceneNames.MainMenu, SceneNames.Act1, SceneNames.Act2_Home, SceneNames.Act3_OutSide, SceneNames.Act_Ending };

    [MenuItem("KuTy/Setup/Create playable prototype")]
    public static void Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (Scenes.Any(s => File.Exists(ScenePath(s))))
        {
            Debug.Log("KuTy scenes already exist. Setup preserves existing scenes; use KuTy/Validate prototype.");
            if (batchSetup) { Validate(); EditorApplication.Exit(0); }
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Generated);
        Directory.CreateDirectory(Root + "/Scenes");
        Directory.CreateDirectory(Root + "/Resources");
        Directory.CreateDirectory(Root + "/Prefabs/Player");
        Directory.CreateDirectory(Root + "/Prefabs/UI");
        Directory.CreateDirectory(Root + "/ScriptableObjects/Items");
        AssetDatabase.Refresh();
        if (AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf") == null)
        {
            AssetDatabase.importPackageCompleted += ResourcesReady;
            AssetDatabase.importPackageFailed += ResourcesFailed;
            string package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_FontAsset).Assembly).resolvedPath;
            AssetDatabase.ImportPackage(Path.Combine(package, "Package Resources/TMP Essential Resources.unitypackage"), false);
            return;
        }
        ConfigureProject();
        CreateFont();
        ground = Material("Ground", new Color(0.47f, 0.48f, 0.30f));
        wood = Material("Wood", new Color(0.48f, 0.27f, 0.13f));
        blue = Material("Blue", new Color(0.13f, 0.55f, 0.80f));
        clean = Material("Clean", new Color(0.95f, 0.92f, 0.79f));
        dirty = Material("Dirty", new Color(0.35f, 0.29f, 0.15f));
        red = Material("Red", new Color(0.83f, 0.26f, 0.18f));
        CreatePlayerPrefab();
        CreateSlotPrefab();
        CreateAudioPrefab();
        CreateMenu(false);
        CreateAct1();
        CreateAct2();
        CreateMenu(true);
        KuTyStorySetup.UpgradeScenes();
        KuTyOfficeSetup.UpgradeScene();
        var existing = EditorBuildSettings.scenes.Where(s => !Scenes.Any(n => s.path == ScenePath(n)));
        EditorBuildSettings.scenes = Scenes.Select(n => new EditorBuildSettingsScene(ScenePath(n), true)).Concat(existing).ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePath(SceneNames.MainMenu));
        Validate();
        Debug.Log("KuTy setup complete. Open MainMenu and press Play.");
        if (batchSetup) EditorApplication.Exit(0);
    }

    private static string ScenePath(string name) => Root + "/Scenes/" + name + ".unity";
    private static void ConfigureProject()
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        foreach (string name in new[] { "Interactable", "Ground" })
        {
            if (LayerMask.NameToLayer(name) >= 0) continue;
            bool added = false;
            for (int i = 6; i < layers.arraySize; i++)
            {
                if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;
                layers.GetArrayElementAtIndex(i).stringValue = name;
                added = true;
                break;
            }
            if (!added) throw new InvalidOperationException("No free layer for " + name);
        }
        tags.ApplyModifiedPropertiesWithoutUndo();
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input = settings.FindProperty("activeInputHandler");
        if (input != null) input.intValue = 0; // Current scripts use legacy Input Manager.
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSettings.serializationMode = SerializationMode.ForceText;
        PlayerSettings.companyName = "KuTy";
        PlayerSettings.productName = "Chia Khoa Ky Uc";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        var renderer = Asset<UniversalRendererData>("Renderer.asset");
        var pipeline = Asset<UniversalRenderPipelineAsset>("URP.asset");
        var serialized = new SerializedObject(pipeline);
        var renderers = serialized.FindProperty("m_RendererDataList");
        renderers.arraySize = 1;
        renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        QualitySettings.vSyncCount = 1;
    }
    private static T Asset<T>(string name) where T : ScriptableObject
    {
        string path = Generated + "/" + name;
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
    private static void CreateFont()
    {
        string fontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Generated + "/Vietnamese.asset");
        if (font != null) return;
        font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(fontPath), 48, 5,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        font.name = "Vietnamese";
        font.isMultiAtlasTexturesEnabled = true;
        AssetDatabase.CreateAsset(font, Generated + "/Vietnamese.asset");
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
        font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 []:/.,!?—đĐăĂâÂêÊôÔơƠưƯ" +
            string.Concat(Enumerable.Range(0x1EA0, 0x1EF9 - 0x1EA0 + 1).Select(c => (char)c)) +
            "ÀÁÃÈÉÌÍÒÓÕÙÚÝàáãèéìíòóõùúý");
        EditorUtility.SetDirty(font);
    }
    private static Material Material(string name, Color color)
    {
        string path = Generated + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.15f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
    private static void Set(UnityEngine.Object owner, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(owner);
        var property = serialized.FindProperty(field);
        if (property == null) throw new MissingFieldException(owner.GetType().Name, field);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetInt(UnityEngine.Object owner, string field, int value)
    {
        var serialized = new SerializedObject(owner);
        serialized.FindProperty(field).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetArray(UnityEngine.Object owner, string field, UnityEngine.Object[] values)
    {
        var serialized = new SerializedObject(owner);
        var array = serialized.FindProperty(field);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }
    private static Transform Point(string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        return go.transform;
    }
    private static void CreatePlayerPrefab()
    {
        var player = new GameObject("Player");
        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.75f;
        controller.radius = 0.28f;
        controller.center = new Vector3(0, 0.875f, 0);
        var camera = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.SetParent(player.transform);
        camera.transform.localPosition = new Vector3(0, 1.55f, 0);
        camera.GetComponent<Camera>().nearClipPlane = 0.05f;
        camera.AddComponent<UniversalAdditionalCameraData>();
        var movement = player.AddComponent<FirstPersonController>();
        Set(movement, "playerCamera", camera.transform);
        SetInt(movement, "groundMask", 1 << LayerMask.NameToLayer("Ground"));
        var interaction = player.AddComponent<PlayerInteraction>();
        Set(interaction, "raycastOrigin", camera.transform);
        SetInt(interaction, "interactableLayer", 1 << LayerMask.NameToLayer("Interactable"));
        playerPrefab = PrefabUtility.SaveAsPrefabAsset(player, Root + "/Prefabs/Player/Player.prefab");
        UnityEngine.Object.DestroyImmediate(player);
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }
    private static TextMeshProUGUI Text(Transform parent, string value, Vector2 size, Vector2 position, float fontSize = 24)
    {
        var text = Rect("Text", parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = new Color(1, 0.96f, 0.85f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }
    private static Image Panel(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var panel = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
        panel.color = new Color(0.10f, 0.15f, 0.18f, 0.96f);
        return panel;
    }
    private static Button Button(Transform parent, string label, Vector2 position, Vector2? size = null)
    {
        var btnSize = size ?? new Vector2(240, 52);
        var image = Panel(parent, label, btnSize, position);
        image.color = new Color(0.14f, 0.25f, 0.28f, 0.98f);

        // Frame border around button
        var borderObj = Rect("Border", image.transform, btnSize + new Vector2(4, 4), Vector2.zero);
        var border = borderObj.gameObject.AddComponent<Image>();
        border.color = new Color(0.85f, 0.68f, 0.38f, 0.55f);
        border.raycastTarget = false;
        borderObj.SetAsFirstSibling();

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var labelText = Text(image.transform, label, btnSize, Vector2.zero, 22);
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = new Color(0.98f, 0.96f, 0.90f);

        var juice = image.gameObject.AddComponent<UIButtonJuice>();
        juice.SetBorder(border, new Color(0.85f, 0.68f, 0.38f, 0.55f), new Color(1f, 0.90f, 0.45f, 1f));

        return button;
    }
    private static Slider Bar(Transform parent, Vector2 position)
    {
        var background = Panel(parent, "Progress", new Vector2(470, 24), position);
        background.raycastTarget = false;
        var fill = Rect("Fill", background.transform, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        fill.color = new Color(0.9f, 0.68f, 0.28f);
        fill.raycastTarget = false;
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        var slider = background.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = fill;
        slider.interactable = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }
    private static Canvas Canvas()
    {
        var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return canvas;
    }
    private static void CreateSlotPrefab()
    {
        var panel = Panel(null, "ShopItemSlot", new Vector2(760, 78), Vector2.zero);
        panel.color = new Color(0.12f, 0.17f, 0.20f, 0.98f);
        var borderObj = Rect("SlotBorder", panel.transform, new Vector2(764, 82), Vector2.zero);
        var bImg = borderObj.gameObject.AddComponent<Image>();
        bImg.color = new Color(0.85f, 0.68f, 0.38f, 0.35f);
        bImg.raycastTarget = false;
        borderObj.SetAsFirstSibling();

        var slot = panel.gameObject.AddComponent<ShopItemSlotUI>();
        var nameTxt = Text(panel.transform, "", new Vector2(190, 65), new Vector2(-265, 0), 22);
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.color = new Color(1f, 0.88f, 0.45f);
        Set(slot, "itemNameText", nameTxt);
        Set(slot, "descriptionText", Text(panel.transform, "", new Vector2(250, 68), new Vector2(-35, 0), 17));
        var priceTxt = Text(panel.transform, "", new Vector2(110, 65), new Vector2(155, 0), 21);
        priceTxt.color = new Color(0.95f, 0.80f, 0.35f);
        Set(slot, "priceText", priceTxt);
        var buy = Button(panel.transform, "Mua", new Vector2(295, 0), new Vector2(120, 46));
        Set(slot, "buyButton", buy);
        Set(slot, "buttonText", buy.GetComponentInChildren<TextMeshProUGUI>());
        slotPrefab = PrefabUtility.SaveAsPrefabAsset(panel.gameObject, Root + "/Prefabs/UI/ShopItemSlot.prefab");
        UnityEngine.Object.DestroyImmediate(panel.gameObject);
    }
    private static void CreateAudioPrefab()
    {
        var library = Asset<SoundLibrary>("SoundLibrary.asset");
        // Original generated tones are placeholders, with no downloaded media.
        library.sfxDishClean = Tone("DishClean", 660);
        library.sfxMarbleShoot = Tone("MarbleShoot", 220);
        library.sfxPurchase = Tone("Purchase", 880);
        library.sfxPurchaseFail = Tone("PurchaseFail", 140);
        library.sfxMemoryCollected = Tone("Memory", 1046);
        library.sfxAllMemoriesComplete = Tone("Complete", 1320);
        var go = new GameObject("KuTyAudio");
        var audio = go.AddComponent<AudioManager>();
        var bgm = go.AddComponent<AudioSource>();
        var sfx = go.AddComponent<AudioSource>();
        var ambient = go.AddComponent<AudioSource>();
        var ui = go.AddComponent<AudioSource>();
        bgm.playOnAwake = sfx.playOnAwake = ambient.playOnAwake = ui.playOnAwake = false;
        ui.ignoreListenerPause = true;
        Set(audio, "bgmSource", bgm); Set(audio, "sfxSource", sfx); Set(audio, "ambientSource", ambient);
        Set(audio, "uiSource", ui);
        Set(audio, "soundLibrary", library);
        Set(audio, "sfxCatalog", AssetDatabase.LoadAssetAtPath<SfxCatalog>("Assets/_Project/Data/Audio/SfxCatalog.asset"));
        PrefabUtility.SaveAsPrefabAsset(go, Root + "/Resources/KuTyAudio.prefab");
        EditorUtility.SetDirty(library);
        UnityEngine.Object.DestroyImmediate(go);
    }
    private static AudioClip Tone(string name, float frequency)
    {
        string path = Generated + "/" + name + ".wav";
        if (!File.Exists(path))
        {
            const int rate = 22050, samples = 5512;
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                for (int i = 0; i < samples; i++)
                    writer.Write((short)(Mathf.Sin(2 * Mathf.PI * frequency * i / rate) * 5000 * (1f - (float)i / samples)));
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    private static void NewScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.65f, 0.60f, 0.48f);
        var light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(48, -30, 0);
    }
    private static void CreateMenu(bool ending)
    {
        NewScene();
        new GameObject("Camera", typeof(Camera), typeof(AudioListener)).tag = "MainCamera";
        Camera.main.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        var canvas = Canvas();

        // Warm sunset atmosphere glow
        var bgGlow = Rect("WarmAtmosphere", canvas.transform, new Vector2(900, 500), new Vector2(0, 40));
        var glowImg = bgGlow.gameObject.AddComponent<Image>();
        glowImg.color = new Color(0.24f, 0.15f, 0.08f, 0.35f);
        glowImg.raycastTarget = false;

        // Title Shadow & Main Text
        var titleShadow = Text(canvas.transform, ending ? "CHÌA KHÓA ƯỚC MƠ" : "CHÌA KHÓA KÝ ỨC", new Vector2(1100, 90), new Vector2(2, 173), 54);
        titleShadow.fontStyle = FontStyles.Bold;
        titleShadow.color = new Color(0f, 0f, 0f, 0.55f);

        var title = Text(canvas.transform, ending ? "CHÌA KHÓA ƯỚC MƠ" : "CHÌA KHÓA KÝ ỨC", new Vector2(1100, 90), new Vector2(0, 175), 54);
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.88f, 0.45f);

        // Golden divider
        var divider = Rect("TitleDivider", canvas.transform, new Vector2(460, 3), new Vector2(0, 128));
        var divImg = divider.gameObject.AddComponent<Image>();
        divImg.color = new Color(0.92f, 0.76f, 0.38f, 0.85f);
        divImg.raycastTarget = false;

        // Slogan / Subtitle
        var subtitle = Text(canvas.transform, ending ? "Gia đình, bạn bè và những niềm vui nhỏ bé.\nBạn đã tìm lại điều mình từng bỏ quên." :
            "Một buổi chiều cũ. Ba mảnh ký ức. Một lần trở về.", new Vector2(950, 70), new Vector2(0, 85), 22);
        subtitle.color = new Color(0.88f, 0.85f, 0.78f);

        var play = Button(canvas.transform, ending ? "⟲  Chơi lại" : "▶  Bắt đầu", new Vector2(0, -45), new Vector2(260, 54));
        var quit = Button(canvas.transform, "✕  Thoát", new Vector2(0, -118), new Vector2(260, 54));
        if (ending)
        {
            var ui = canvas.gameObject.AddComponent<EndingUI>();
            UnityEventTools.AddPersistentListener(play.onClick, ui.OnPlayAgainClicked);
            UnityEventTools.AddPersistentListener(quit.onClick, ui.OnQuitClicked);
        }
        else
        {
            var ui = canvas.gameObject.AddComponent<MainMenuUI>();
            UnityEventTools.AddPersistentListener(play.onClick, ui.OnPlayButtonClicked);
            UnityEventTools.AddPersistentListener(quit.onClick, ui.OnQuitButtonClicked);
        }

        // Pill guide footer
        var guidePill = Panel(canvas.transform, "ControlGuidePill", new Vector2(780, 38), new Vector2(0, -295));
        guidePill.color = new Color(0.08f, 0.12f, 0.15f, 0.85f);
        guidePill.raycastTarget = false;
        var guideBorder = Rect("PillBorder", guidePill.transform, new Vector2(784, 42), Vector2.zero);
        var pillBorderImg = guideBorder.gameObject.AddComponent<Image>();
        pillBorderImg.color = new Color(0.85f, 0.68f, 0.38f, 0.4f);
        pillBorderImg.raycastTarget = false;
        guideBorder.SetAsFirstSibling();

        Text(guidePill.transform, "Bản prototype — WASD: di chuyển · Chuột: nhìn · E: tương tác · Esc: menu", new Vector2(760, 35), Vector2.zero, 16);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath(ending ? SceneNames.Act_Ending : SceneNames.MainMenu));
    }
    private static void World()
    {
        var floor = Box("Ground", new Vector3(0, -0.15f, 2), new Vector3(26, 0.3f, 22), ground);
        floor.layer = LayerMask.NameToLayer("Ground");
        Box("BackWall", new Vector3(0, 1.5f, 12), new Vector3(26, 3, 0.3f), wood);
        Box("LeftWall", new Vector3(-13, 1.5f, 2), new Vector3(0.3f, 3, 22), wood);
        Box("RightWall", new Vector3(13, 1.5f, 2), new Vector3(0.3f, 3, 22), wood);
        Box("FrontWall", new Vector3(0, 1.5f, -9), new Vector3(26, 3, 0.3f), wood);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(0, 0.05f, -5);
    }
    private static void Sign(string text, Vector3 position)
    {
        var go = new GameObject("Sign", typeof(TextMeshPro));
        go.transform.position = position;
        var label = go.GetComponent<TextMeshPro>();
        label.font = font; label.text = text; label.fontSize = 4; label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(5, 1.5f);
        label.color = Color.white;
    }
    private static Canvas GameplayUI()
    {
        var canvas = Canvas();
        var hud = canvas.gameObject.AddComponent<HUDController>();
        var crosshair = Panel(canvas.transform, "Crosshair", new Vector2(5, 5), Vector2.zero);
        crosshair.color = Color.white; crosshair.raycastTarget = false;
        Set(hud, "crosshairImage", crosshair);
        var prompt = Text(canvas.transform, "", new Vector2(900, 45), new Vector2(0, -55));
        Set(hud, "promptPanel", prompt.gameObject); Set(hud, "promptText", prompt);
        Set(hud, "moneyText", Text(canvas.transform, "0 đồng", new Vector2(250, 45), new Vector2(485, 310)));
        var icons = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            icons[i] = Panel(canvas.transform, "Memory" + i, new Vector2(24, 24), new Vector2(-590 + i * 145, 310));
            icons[i].raycastTarget = false;
            Text(canvas.transform, new[] { "Mẹ", "Bạn bè", "Niềm vui" }[i], new Vector2(110, 35), new Vector2(-525 + i * 145, 310), 18);
        }
        SetArray(hud, "memoryIcons", icons);
        Text(canvas.transform, "WASD: đi · Chuột: nhìn · E: tương tác · Esc: tạm dừng / thoát hoạt động", new Vector2(1200, 35), new Vector2(0, -330), 18);
        return canvas;
    }
    private static void DialogueAndPause(Canvas canvas)
    {
        var panel = Panel(canvas.transform, "DialoguePanel", new Vector2(1060, 175), new Vector2(0, -215));
        panel.color = new Color(0.09f, 0.13f, 0.17f, 0.96f);

        // Outer border for gamified RPG feel
        var outerBorder = Rect("DialogueBorder", panel.transform, new Vector2(1066, 181), Vector2.zero);
        var borderImg = outerBorder.gameObject.AddComponent<Image>();
        borderImg.color = new Color(0.85f, 0.68f, 0.38f, 0.90f);
        borderImg.raycastTarget = false;
        outerBorder.SetAsFirstSibling();

        // Top gold accent line
        var topLine = Rect("TopAccent", panel.transform, new Vector2(1060, 3), new Vector2(0, 86));
        var topImg = topLine.gameObject.AddComponent<Image>();
        topImg.color = new Color(1f, 0.88f, 0.45f, 1f);
        topImg.raycastTarget = false;

        // Bottom gold accent line
        var bottomLine = Rect("BottomAccent", panel.transform, new Vector2(1060, 3), new Vector2(0, -86));
        var bottomImg = bottomLine.gameObject.AddComponent<Image>();
        bottomImg.color = new Color(0.85f, 0.68f, 0.38f, 0.8f);
        bottomImg.raycastTarget = false;

        // Speaker Name Badge (khung tên người nói ở góc trên bên trái)
        var speakerBadge = Rect("SpeakerBadge", panel.transform, new Vector2(240, 38), new Vector2(-380, 88));
        var badgeBg = speakerBadge.gameObject.AddComponent<Image>();
        badgeBg.color = new Color(0.20f, 0.14f, 0.10f, 0.98f);
        var badgeBorder = Rect("BadgeBorder", speakerBadge, new Vector2(244, 42), Vector2.zero);
        var bBorderImg = badgeBorder.gameObject.AddComponent<Image>();
        bBorderImg.color = new Color(0.95f, 0.78f, 0.38f, 1f);
        bBorderImg.raycastTarget = false;
        badgeBorder.SetAsFirstSibling();

        var titleText = Text(speakerBadge, "", new Vector2(220, 34), Vector2.zero, 22);
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.90f, 0.50f);

        // Body dialogue text
        var bodyText = Text(panel.transform, "", new Vector2(990, 80), new Vector2(0, -8), 24);
        bodyText.color = new Color(0.96f, 0.95f, 0.92f);
        bodyText.alignment = TextAlignmentOptions.TopLeft;

        // Continue prompt indicator [Space Tiếp tục]
        var continuePrompt = Rect("ContinuePrompt", panel.transform, new Vector2(180, 28), new Vector2(410, -58));
        var promptBg = continuePrompt.gameObject.AddComponent<Image>();
        promptBg.color = new Color(0.12f, 0.18f, 0.22f, 0.92f);
        var promptBorder = Rect("PromptBorder", continuePrompt, new Vector2(184, 32), Vector2.zero);
        var pBorderImg = promptBorder.gameObject.AddComponent<Image>();
        pBorderImg.color = new Color(0.85f, 0.68f, 0.38f, 0.7f);
        pBorderImg.raycastTarget = false;
        promptBorder.SetAsFirstSibling();

        var promptText = Text(continuePrompt, "[ Space ▶ ]", new Vector2(170, 26), Vector2.zero, 15);
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = new Color(1f, 0.88f, 0.40f);

        var dialogue = canvas.gameObject.AddComponent<DialogueUI>();
        Set(dialogue, "panel", panel.gameObject);
        Set(dialogue, "canvasGroup", panel.gameObject.AddComponent<CanvasGroup>());
        Set(dialogue, "titleText", titleText);
        Set(dialogue, "bodyText", bodyText);
        Set(dialogue, "speakerBadge", speakerBadge.gameObject);
        Set(dialogue, "continuePrompt", continuePrompt.gameObject);
        Set(dialogue, "containerRect", panel.rectTransform);

        // Pause Menu with Dim Backdrop & Styled Card
        var pausePanel = Panel(canvas.transform, "Pause", new Vector2(1280, 720), Vector2.zero);
        pausePanel.color = new Color(0f, 0f, 0f, 0.70f); // Screen-dimmer backdrop

        var pauseCard = Panel(pausePanel.transform, "PauseCard", new Vector2(460, 280), Vector2.zero);
        pauseCard.color = new Color(0.09f, 0.13f, 0.17f, 0.98f);
        var pauseBorder = Rect("CardBorder", pauseCard.transform, new Vector2(466, 286), Vector2.zero);
        var pCardBorderImg = pauseBorder.gameObject.AddComponent<Image>();
        pCardBorderImg.color = new Color(0.85f, 0.68f, 0.38f, 0.85f);
        pCardBorderImg.raycastTarget = false;
        pauseBorder.SetAsFirstSibling();

        var pauseTitle = Text(pauseCard.transform, "TẠM DỪNG", new Vector2(400, 45), new Vector2(0, 85), 32);
        pauseTitle.fontStyle = FontStyles.Bold;
        pauseTitle.color = new Color(1f, 0.88f, 0.45f);

        var pauseDivider = Rect("PauseDivider", pauseCard.transform, new Vector2(240, 2), new Vector2(0, 58));
        var divImg = pauseDivider.gameObject.AddComponent<Image>();
        divImg.color = new Color(0.85f, 0.68f, 0.38f, 0.7f);
        divImg.raycastTarget = false;

        var pause = canvas.gameObject.AddComponent<PauseUI>();
        Set(pause, "panel", pausePanel.gameObject);
        Set(pause, "resumeButton", Button(pauseCard.transform, "Tiếp tục", new Vector2(0, 10), new Vector2(240, 48)));
        Set(pause, "menuButton", Button(pauseCard.transform, "Về menu", new Vector2(0, -60), new Vector2(240, 48)));
    }
    private static void CreateAct1()
    {
        NewScene(); World();
        var canvas = GameplayUI();
        Box("OldTable", new Vector3(0, 0.55f, 0), new Vector3(3, 1.1f, 1.5f), wood);
        var keepsake = Box("Keepsake", new Vector3(0, 1.3f, -0.3f), new Vector3(0.45f, 0.4f, 0.45f), blue);
        keepsake.layer = LayerMask.NameToLayer("Interactable");
        keepsake.AddComponent<MemoryTrigger>();
        Sign("Căn nhà cũ\nChạm vào kỷ vật trên bàn", new Vector3(0, 2.8f, 0.7f));
        DialogueAndPause(canvas);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath(SceneNames.Act1));
    }
    private static void CreateAct2()
    {
        NewScene(); World();
        var systems = new GameObject("Act2Systems");
        systems.AddComponent<CurrencyManager>(); systems.AddComponent<InventoryManager>(); systems.AddComponent<MemoryCollectionManager>();
        var shopManager = systems.AddComponent<ShopManager>();
        var canvas = GameplayUI();
        CreateDishes(canvas);
        CreateMarbles(canvas);
        CreateShop(canvas, shopManager);
        DialogueAndPause(canvas);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath(SceneNames.Act2_Home));
    }
    private static void CreateDishes(Canvas canvas)
    {
        var sink = Box("Sink", new Vector3(-7, 0.5f, 4), new Vector3(3, 1, 1.6f), blue);
        sink.layer = LayerMask.NameToLayer("Interactable");
        var game = new GameObject("DishWashingGame").AddComponent<DishWashingGame>();
        var dishes = new GameObject[5];
        for (int i = 0; i < dishes.Length; i++)
        {
            dishes[i] = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dishes[i].name = "Dish" + i;
            dishes[i].transform.position = new Vector3(-8 + i * 0.5f, 1.1f, 4);
            dishes[i].transform.localScale = new Vector3(0.4f, 0.04f, 0.4f);
            dishes[i].GetComponent<Renderer>().sharedMaterial = dirty;
        }
        SetArray(game, "dishes", dishes); Set(game, "cleanDishMaterial", clean);
        Set(sink.AddComponent<DishInteractable>(), "dishWashingGame", game);
        Sign("GIAN BẾP\nGiúp mẹ rửa 5 chiếc chén", new Vector3(-7, 2.8f, 4.8f));
        var panel = Panel(canvas.transform, "DishProgress", new Vector2(900, 130), new Vector2(0, -200));
        panel.raycastTarget = false;
        var ui = canvas.gameObject.AddComponent<ProgressBarUI>();
        Set(ui, "panel", panel.gameObject);
        Set(ui, "labelText", Text(panel.transform, "", new Vector2(860, 60), new Vector2(0, 28), 23));
        var bar = Bar(panel.transform, new Vector2(0, -28));
        Set(ui, "progressSlider", bar); Set(ui, "fillImage", bar.fillRect.GetComponent<Image>());
    }
    private static Rigidbody Marble(string name, Vector3 position, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name; go.transform.position = position; go.transform.localScale = Vector3.one * 0.35f;
        go.GetComponent<Renderer>().sharedMaterial = material;
        var body = go.AddComponent<Rigidbody>();
        body.mass = 0.25f; body.drag = 0.8f; body.angularDrag = 1f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        return body;
    }
    private static void CreateMarbles(Canvas canvas)
    {
        Box("MarbleTable", new Vector3(0, 0.4f, 5), new Vector3(6, 0.8f, 6), wood);
        var ring = Box("ScoringRing", new Vector3(0, 0.82f, 5), new Vector3(4, 0.03f, 4), ground);
        ring.GetComponent<Collider>().isTrigger = true;
        var ringLine = ring.AddComponent<LineRenderer>();
        ringLine.useWorldSpace = true; ringLine.loop = true; ringLine.positionCount = 64;
        ringLine.startWidth = ringLine.endWidth = 0.025f;
        ringLine.sharedMaterial = clean;
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2 / 64;
            ringLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * 2, 0.855f, 5 + Mathf.Sin(angle) * 2));
        }
        var game = new GameObject("MarbleShootingGame").AddComponent<MarbleShootingGame>();
        var shooter = Marble("PlayerMarble", new Vector3(0, 1.0f, 2.5f), blue);
        var targets = new Rigidbody[5];
        for (int i = 0; i < 5; i++)
        {
            float angle = (30 + i * 30) * Mathf.Deg2Rad;
            targets[i] = Marble("Target" + i, new Vector3(Mathf.Cos(angle) * 1.45f, 1.0f, 5 + Mathf.Sin(angle) * 1.45f), red);
        }
        Set(game, "playerMarble", shooter); SetArray(game, "targetMarbles", targets);
        Set(game, "shootPosition", Point("ShootPosition", shooter.position));
        Set(game, "ringCollider", ring.GetComponent<Collider>());
        var cameraPoint = Point("MarbleCameraPosition", new Vector3(0, 9.5f, 3));
        cameraPoint.rotation = Quaternion.Euler(75, 0, 0);
        Set(game, "gameCameraPosition", cameraPoint);
        Set(game, "gameCameraLookAt", Point("MarbleLookAt", new Vector3(0, 0.8f, 5)));
        var line = game.gameObject.AddComponent<LineRenderer>();
        line.sharedMaterial = blue; line.startWidth = line.endWidth = 0.04f; line.positionCount = 2;
        Set(game, "aimLine", line);
        var entry = Box("PlayMarbles", new Vector3(0, 0.65f, 1.4f), new Vector3(1.4f, 1.3f, 0.5f), blue);
        entry.layer = LayerMask.NameToLayer("Interactable");
        Set(entry.AddComponent<MarbleInteractable>(), "marbleGame", game);
        Sign("SÂN NHÀ\nChơi bi cùng bạn", new Vector3(0, 2.9f, 8.1f));
        var panel = Panel(canvas.transform, "MarbleHUD", new Vector2(1000, 130), new Vector2(0, 225));
        panel.raycastTarget = false;
        var ui = canvas.gameObject.AddComponent<MarbleAimUI>();
        Set(ui, "panel", panel.gameObject);
        Text(panel.transform, "Kéo từ bi xanh rồi thả · Đẩy 3 bi đỏ ra vòng · Esc: thoát", new Vector2(970, 35), new Vector2(0, 42), 21);
        Set(ui, "shotsText", Text(panel.transform, "", new Vector2(220, 40), new Vector2(-310, 0), 22));
        Set(ui, "knockedText", Text(panel.transform, "", new Vector2(220, 40), new Vector2(310, 0), 22));
        var bar = Bar(panel.transform, new Vector2(0, -30));
        Set(ui, "forceSlider", bar); Set(ui, "forceSliderFill", bar.fillRect.GetComponent<Image>());
    }
    private static void CreateShop(Canvas canvas, ShopManager manager)
    {
        string[] names = { "Kẹo dừa", "Kẹo kéo", "Bi ve", "Con diều" };
        int[] prices = { 2, 3, 2, 8 };
        var items = new ItemData[4];
        for (int i = 0; i < 4; i++)
        {
            string path = Root + "/ScriptableObjects/Items/Item" + i + ".asset";
            items[i] = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (items[i] != null) continue;
            items[i] = ScriptableObject.CreateInstance<ItemData>();
            items[i].itemName = names[i]; items[i].price = prices[i]; items[i].isSpecialItem = i == 3;
            items[i].itemType = i == 3 ? ItemType.Special : i == 2 ? ItemType.Toy : ItemType.Candy;
            items[i].description = i == 3 ? "Món đồ chơi chứa một mảnh ký ức." : "Một niềm vui nho nhỏ của tuổi thơ.";
            AssetDatabase.CreateAsset(items[i], path);
        }
        SetArray(manager, "shopItems", items);
        var counter = Box("ShopCounter", new Vector3(7, 0.6f, 4), new Vector3(3.5f, 1.2f, 1.5f), wood);
        counter.layer = LayerMask.NameToLayer("Interactable"); counter.AddComponent<ShopInteractable>();
        Sign("TIỆM TẠP HÓA\nCon diều tuổi thơ", new Vector3(7, 2.8f, 4.8f));
        var panel = Panel(canvas.transform, "ShopPanel", new Vector2(900, 570), Vector2.zero);
        var ui = canvas.gameObject.AddComponent<ShopUI>();
        Set(ui, "shopPanel", panel.gameObject);
        Text(panel.transform, "TIỆM TẠP HÓA", new Vector2(800, 45), new Vector2(0, 238), 33);
        Set(ui, "playerMoneyText", Text(panel.transform, "", new Vector2(800, 35), new Vector2(0, 193), 24));
        var list = Rect("Items", panel.transform, new Vector2(760, 340), new Vector2(0, -3));
        var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8; layout.childControlHeight = false; layout.childControlWidth = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        Set(ui, "itemListParent", list); Set(ui, "itemSlotPrefab", slotPrefab);
        Set(ui, "messageText", Text(panel.transform, "", new Vector2(840, 35), new Vector2(0, -197), 20));
        Set(ui, "closeButton", Button(panel.transform, "Đóng (Esc)", new Vector2(0, -243)));
    }

    private static void EnsureAssetsLoaded()
    {
        CreateFont();
        if (ground == null) ground = Material("Ground", new Color(0.47f, 0.48f, 0.30f));
        if (wood == null) wood = Material("Wood", new Color(0.48f, 0.27f, 0.13f));
        if (blue == null) blue = Material("Blue", new Color(0.13f, 0.55f, 0.80f));
        if (clean == null) clean = Material("Clean", new Color(0.95f, 0.92f, 0.79f));
        if (dirty == null) dirty = Material("Dirty", new Color(0.35f, 0.29f, 0.15f));
        if (red == null) red = Material("Red", new Color(0.83f, 0.26f, 0.18f));
        if (playerPrefab == null) playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Player/Player.prefab");
        if (slotPrefab == null) slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/UI/ShopItemSlot.prefab");
    }

    [MenuItem("KuTy/UI/Rebuild Beautiful UI in Scenes")]
    public static void RebuildUI()
    {
        EnsureAssetsLoaded();
        CreateSlotPrefab();
        CreateMenu(false);
        CreateAct1();
        CreateAct2();
        CreateMenu(true);
        KuTyStorySetup.UpgradeScenes();
        KuTyOfficeSetup.UpgradeScene();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePath(SceneNames.MainMenu));
        Debug.Log("KuTy Beautiful UI rebuild completed successfully!");
    }

    [MenuItem("KuTy/Validate prototype")]
    public static void Validate()
    {
        foreach (string scene in Scenes)
            if (!File.Exists(ScenePath(scene))) throw new InvalidOperationException("Missing scene: " + scene);
        var items = AssetDatabase.FindAssets("t:ItemData", new[] { Root + "/ScriptableObjects/Items" })
            .Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        var candy = items.FirstOrDefault(i => i != null && i.itemName == "Kẹo mút");
        var marbles = items.FirstOrDefault(i => i != null && i.itemName == "Hũ bi ve");
        if (candy == null || marbles == null || candy.price + marbles.price != 2000)
            throw new InvalidOperationException("Story economy is invalid.");
        if (GraphicsSettings.defaultRenderPipeline == null) throw new InvalidOperationException("URP is missing.");
        foreach (string name in Scenes)
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath(name)))
                throw new InvalidOperationException("Scene not enabled: " + name);
        Debug.Log("KUTY_VALIDATION_PASSED");
    }
    [MenuItem("KuTy/Build Windows prototype")]
    public static void BuildWindows()
    {
        Validate();
        string output = "Builds/Windows/KuTy.exe";
        Directory.CreateDirectory("Builds/Windows");
        var result = UnityEditor.BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            output, BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException("Build failed: " + result.summary.result);
        Debug.Log("KUTY_BUILD_PASSED: " + output);
    }
}
