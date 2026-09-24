using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Only modifies Act1. All props are replaceable placeholders under OfficeRoot.
public static class KuTyOfficeSetup
{
    private const string Root = "Assets/_Project";
    private const string ScenePath = Root + "/Scenes/Act1_RealWorld.unity";
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Generated/Vietnamese.asset");
    [MenuItem("KuTy/Setup/Build interactive office scene (Act 1)")]
    public static void UpgradeMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        UpgradeScene();
    }
    public static void UpgradeBatch()
    {
        try { UpgradeScene(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    public static void UpgradeScene()
    {
        EditorSceneManager.OpenScene(ScenePath);
        if (GameObject.Find("OfficeRoot") != null)
        {
            EnsureOfficeBindings();
            PolishGeneratedScene();
            EnlargeGeneratedScreenText();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("OfficeRoot already exists; keeping user assets and gameplay objects.");
            return;
        }
        DisableOldIntro();
        var office = new GameObject("OfficeRoot");
        var grey = Material("OfficeWall", new Color(.34f, .39f, .45f));
        var dark = Material("OfficeDark", new Color(.08f, .11f, .15f));
        var floor = Material("OfficeFloor", new Color(.27f, .31f, .34f));
        var deskMat = Material("OfficeDesk", new Color(.33f, .34f, .32f));
        var screenMat = ScreenMaterial();
        var paper = Material("OfficePaper", new Color(.78f, .79f, .75f));
        var lightMat = Material("OfficeLight", new Color(.86f, .94f, 1f));
        var city = Material("OfficeCity", new Color(.16f, .21f, .27f));
        var car = Material("OfficeCar", new Color(.42f, .19f, .18f));
        BuildRoom(office.transform, grey, floor, dark, lightMat);
        BuildCubicle(office.transform, grey, dark);
        var controller = office.AddComponent<OfficeSceneController>();
        var ambience = office.AddComponent<OfficeAtmosphere>();
        Ref(controller, "atmosphere", ambience);
        BuildDesk(office.transform, controller, deskMat, dark, paper, screenMat);
        BuildChair(office.transform, controller);
        BuildBoss(office.transform, controller, ambience);
        BuildWindow(office.transform, controller, ambience, grey, city, car, dark);
        BuildLight(office.transform, ambience, lightMat, dark);
        BuildUI(controller);
        var player = GameObject.Find("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(0, .05f, -3.6f);
            player.transform.rotation = Quaternion.identity;
            Ref(controller, "playerTransform", player.transform);
        }
        RenderSettings.ambientLight = new Color(.38f, .43f, .5f);
        var sun = GameObject.Find("Sun")?.GetComponent<Light>();
        if (sun != null) { sun.intensity = .25f; sun.color = new Color(.74f, .84f, 1f); }
        PolishGeneratedScene();
        EnlargeGeneratedScreenText();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("KUTY_OFFICE_UPGRADE_PASSED");
    }
    private static void EnsureOfficeBindings()
    {
        var controller = GameObject.Find("OfficeRoot")?.GetComponent<OfficeSceneController>();
        if (controller == null) return;
        var data = new SerializedObject(controller);
        var chair = data.FindProperty("chairAnchor");
        var player = data.FindProperty("playerTransform");
        if (chair != null && chair.objectReferenceValue == null)
            chair.objectReferenceValue = GameObject.Find("OfficeChair")?.transform;
        if (player != null && player.objectReferenceValue == null)
            player.objectReferenceValue = GameObject.Find("Player")?.transform;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void PolishGeneratedScene()
    {
        if (GameObject.Find("OfficeVisualPolishV2") != null) return;
        var office = GameObject.Find("OfficeRoot");
        if (office == null) return;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.62f, .67f, .74f);
        var colors = new[] {
            ("OfficeWall", new Color(.48f, .52f, .57f)),
            ("OfficeDark", new Color(.16f, .20f, .25f)),
            ("OfficeFloor", new Color(.38f, .41f, .45f)),
            ("OfficeDesk", new Color(.48f, .50f, .52f)),
            ("OfficeCity", new Color(.32f, .36f, .41f)),
            ("OfficePaper", new Color(.91f, .91f, .88f))
        };
        foreach (var entry in colors)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Generated/" + entry.Item1 + ".mat");
            if (material == null) continue;
            material.SetColor("_BaseColor", entry.Item2);
            EditorUtility.SetDirty(material);
        }
        var chair = GameObject.Find("OfficeChair");
        if (chair != null) chair.transform.localPosition = new Vector3(1.02f, 0, -1.48f);
        var lamp = GameObject.Find("FluorescentFixture")?.GetComponent<Light>();
        if (lamp != null) { lamp.intensity = 3.4f; lamp.range = 10f; }
        var marker = new GameObject("OfficeVisualPolishV2");
        marker.transform.SetParent(office.transform, false);
    }
    private static void EnlargeGeneratedScreenText()
    {
        if (GameObject.Find("OfficeScreenTextV3") != null) return;
        var office = GameObject.Find("OfficeRoot");
        var warning = GameObject.Find("RedReportWarning")?.GetComponent<TextMeshPro>();
        if (office == null || warning == null) return;
        warning.fontSize = 7.2f;
        var marker = new GameObject("OfficeScreenTextV3");
        marker.transform.SetParent(office.transform, false);
    }
    private static void DisableOldIntro()
    {
        var oldDoor = GameObject.Find("ChildhoodDoor");
        if (oldDoor != null) oldDoor.SetActive(false);
        var oldTable = GameObject.Find("OldTable");
        if (oldTable != null) oldTable.SetActive(false);
        var oldSign = GameObject.Find("Sign");
        if (oldSign != null) oldSign.SetActive(false);
        var oldOverlay = GameObject.Find("PrologueOverlay");
        if (oldOverlay != null) oldOverlay.SetActive(false);
        var oldPrologue = GameObject.Find("UI")?.GetComponent<StoryPrologueController>();
        if (oldPrologue != null) oldPrologue.enabled = false;
        var canvas = GameObject.Find("UI");
        if (canvas == null) return;
        foreach (var icon in canvas.GetComponentsInChildren<Image>(true))
            if (icon.name.StartsWith("Memory")) icon.gameObject.SetActive(false);
        foreach (var label in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label.text == "Mẹ" || label.text == "Bạn bè" || label.text == "Niềm vui" || label.text == "Tự do" || label.text == "0 đồng")
                label.gameObject.SetActive(false);
            else if (label.text.StartsWith("WASD:"))
                label.text = "WASD: đi · Chuột: nhìn · E: tương tác · Giữ chuột trái: làm việc · Esc: dừng/tạm nghỉ";
        }
    }
    private static Material Material(string name, Color color)
    {
        string path = Root + "/Generated/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", .12f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
    private static Material ScreenMaterial()
    {
        string path = Root + "/Generated/OfficeScreenBlack.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        var material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", new Color(.004f, .006f, .01f));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
    private static void Ref(UnityEngine.Object owner, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(owner);
        var property = data.FindProperty(field);
        if (property == null) throw new MissingFieldException(owner.GetType().Name, field);
        property.objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Enum(UnityEngine.Object owner, string field, int value)
    {
        var data = new SerializedObject(owner);
        data.FindProperty(field).enumValueIndex = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Array(UnityEngine.Object owner, string field, Transform[] values)
    {
        var data = new SerializedObject(owner);
        var property = data.FindProperty(field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool solid = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    private static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    private static void BuildRoom(Transform root, Material wall, Material ground, Material dark, Material light)
    {
        var room = new GameObject("OfficeRoom").transform;
        room.SetParent(root, false);
        var groundObject = Box("OfficeFloor", room, new Vector3(0, -.08f, 0), new Vector3(10, .16f, 12), ground);
        groundObject.layer = LayerMask.NameToLayer("Ground");
        Box("BackWall", room, new Vector3(0, 1.62f, -6), new Vector3(10, 3.24f, .2f), wall);
        Box("LeftWall", room, new Vector3(-5, 1.62f, 0), new Vector3(.2f, 3.24f, 12), wall);
        Box("RightWall", room, new Vector3(5, 1.62f, 0), new Vector3(.2f, 3.24f, 12), wall);
        Box("Ceiling", room, new Vector3(0, 3.35f, 0), new Vector3(10, .16f, 12), dark);
        Box("WindowWallLeft", room, new Vector3(-3.6f, 1.62f, 5.8f), new Vector3(2.8f, 3.24f, .2f), wall);
        Box("WindowWallRight", room, new Vector3(3.6f, 1.62f, 5.8f), new Vector3(2.8f, 3.24f, .2f), wall);
        Box("WindowWallBottom", room, new Vector3(0, .36f, 5.8f), new Vector3(4.4f, .72f, .2f), wall);
        Box("WindowWallTop", room, new Vector3(0, 3.03f, 5.8f), new Vector3(4.4f, .59f, .2f), wall);
        // Cold light strips guide the eye toward the window.
        Box("CeilingStripLeft", room, new Vector3(-3.5f, 3.24f, 0), new Vector3(.04f, .03f, 11), light, false);
        Box("CeilingStripRight", room, new Vector3(3.5f, 3.24f, 0), new Vector3(.04f, .03f, 11), light, false);
    }
    private static void BuildCubicle(Transform root, Material wall, Material trim)
    {
        var cubicle = new GameObject("CubicleWalls").transform;
        cubicle.SetParent(root, false);
        Box("LeftPartition", cubicle, new Vector3(-1.92f, .73f, -.85f), new Vector3(.13f, 1.46f, 3.8f), wall);
        Box("RightPartition", cubicle, new Vector3(1.92f, .73f, -.85f), new Vector3(.13f, 1.46f, 3.8f), wall);
        Box("LeftTrim", cubicle, new Vector3(-1.92f, 1.48f, -.85f), new Vector3(.16f, .05f, 3.8f), trim, false);
        Box("RightTrim", cubicle, new Vector3(1.92f, 1.48f, -.85f), new Vector3(.16f, .05f, 3.8f), trim, false);
    }
    private static void Interactable(GameObject target, OfficeSceneController controller, OfficeInteractionKind kind)
    {
        target.layer = LayerMask.NameToLayer("Interactable");
        var component = target.AddComponent<OfficeInteractable>();
        Ref(component, "office", controller);
        Enum(component, "kind", (int)kind);
    }
    private static void BuildDesk(Transform root, OfficeSceneController controller, Material desk, Material dark, Material paper, Material screen)
    {
        var work = new GameObject("Workstation").transform;
        work.SetParent(root, false);
        var deskRoot = new GameObject("OfficeDesk");
        deskRoot.transform.SetParent(work, false);
        deskRoot.transform.localPosition = new Vector3(0, .7f, .45f);
        var deskCollider = deskRoot.AddComponent<BoxCollider>();
        deskCollider.size = new Vector3(2.85f, .17f, 1.35f);
        // Visuals are intentionally empty; the player's desk model belongs here.

        var laptop = new GameObject("OfficeLaptop");
        laptop.transform.SetParent(work, false);
        laptop.transform.localPosition = new Vector3(0, 1.17f, .4f);
        var laptopCollider = laptop.AddComponent<BoxCollider>();
        laptopCollider.size = new Vector3(1.24f, .82f, .14f);
        Shape("ScreenFramePlaceholder", PrimitiveType.Cube, laptop.transform, Vector3.zero, new Vector3(1.24f, .82f, .10f), dark);
        Shape("ScreenGlassPlaceholder", PrimitiveType.Cube, laptop.transform, new Vector3(0, 0, -.058f),
            new Vector3(1.12f, .70f, .012f), screen);
        Shape("KeyboardPlaceholder", PrimitiveType.Cube, laptop.transform, new Vector3(0, -.38f, -.35f),
            new Vector3(1.2f, .035f, .7f), dark);
        var warning = new GameObject("RedReportWarning", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        warning.transform.SetParent(laptop.transform, false);
        warning.transform.localPosition = new Vector3(0, .02f, -.07f);
        warning.transform.localScale = Vector3.one * .15f;
        warning.rectTransform.sizeDelta = new Vector2(7, 3);
        warning.font = Font;
        warning.fontSize = 7.2f;
        warning.alignment = TextAlignmentOptions.Center;
        warning.color = new Color(1f, .16f, .15f);
        warning.text = "BÁO CÁO BỊ TRẢ VỀ\nLÀM LẠI NGAY";
        Ref(controller, "screenWarning", warning);
        Interactable(laptop, controller, OfficeInteractionKind.Laptop);

        var files = Box("DocumentStack", work, new Vector3(-.98f, .81f, .30f), new Vector3(.64f, .11f, .56f), paper);
        for (int i = 0; i < 5; i++)
            Shape("PagePlaceholder" + i, PrimitiveType.Cube, files.transform,
                new Vector3(i * .01f, .5f + i * .18f, i * -.01f), new Vector3(1.02f, .2f, 1.02f), i % 2 == 0 ? paper : desk);
        Interactable(files, controller, OfficeInteractionKind.Documents);
        var typing = laptop.AddComponent<AudioSource>();
        typing.playOnAwake = false;
        Ref(controller.GetComponent<OfficeAtmosphere>(), "keyboardSource", typing);
    }
    private static void BuildChair(Transform root, OfficeSceneController controller)
    {
        var chair = new GameObject("OfficeChair");
        chair.transform.SetParent(root, false);
        chair.transform.localPosition = new Vector3(0, 0, -1.48f);
        var collider = chair.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, .65f, 0);
        collider.size = new Vector3(.85f, 1.3f, .82f);
        Interactable(chair, controller, OfficeInteractionKind.Chair);
        Ref(controller, "chairAnchor", chair.transform);
    }
    private static void BuildBoss(Transform root, OfficeSceneController controller, OfficeAtmosphere ambience)
    {
        var boss = new GameObject("BossSilhouette");
        boss.transform.SetParent(root, false);
        boss.transform.localPosition = new Vector3(3.15f, 0, .85f);
        var collider = boss.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 1.33f, 0);
        collider.size = new Vector3(.85f, 2.45f, .65f);
        var voice = boss.AddComponent<AudioSource>(); voice.playOnAwake = false;
        Ref(ambience, "bossSource", voice);
        Interactable(boss, controller, OfficeInteractionKind.Boss);
    }
    private static void BuildWindow(Transform root, OfficeSceneController controller, OfficeAtmosphere ambience,
        Material frame, Material city, Material car, Material dark)
    {
        var window = new GameObject("OfficeWindow");
        window.transform.SetParent(root, false);
        window.transform.localPosition = new Vector3(0, 1.83f, 5.62f);
        var glass = window.AddComponent<BoxCollider>();
        glass.size = new Vector3(4.25f, 2.26f, .06f);
        Interactable(window, controller, OfficeInteractionKind.Window);
        Box("WindowFrameTopPlaceholder", window.transform, new Vector3(0, 1.16f, -.08f), new Vector3(4.5f, .1f, .13f), frame, false);
        Box("WindowFrameBottomPlaceholder", window.transform, new Vector3(0, -1.16f, -.08f), new Vector3(4.5f, .1f, .13f), frame, false);
        Box("WindowFrameLeftPlaceholder", window.transform, new Vector3(-2.2f, 0, -.08f), new Vector3(.1f, 2.35f, .13f), frame, false);
        Box("WindowFrameRightPlaceholder", window.transform, new Vector3(2.2f, 0, -.08f), new Vector3(.1f, 2.35f, .13f), frame, false);
        Box("WindowMullionPlaceholder", window.transform, new Vector3(0, 0, -.08f), new Vector3(.07f, 2.3f, .13f), frame, false);
        var street = new GameObject("StreetView");
        street.transform.SetParent(root, false);
        street.transform.localPosition = new Vector3(0, 0, 6.18f);
        var motion = street.AddComponent<OfficeStreetMotion>();
        var sprite = new GameObject("StreetBackdrop2D", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        sprite.transform.SetParent(street.transform, false);
        sprite.transform.localPosition = new Vector3(0, 1.78f, .1f);
        Ref(motion, "backdropImage", sprite);
        var placeholders = new GameObject("CityPlaceholder");
        placeholders.transform.SetParent(street.transform, false);
        Box("CitySky", placeholders.transform, new Vector3(0, 1.8f, .37f), new Vector3(4.4f, 2.42f, .04f), frame, false);
        Box("Road", placeholders.transform, new Vector3(0, .84f, .28f), new Vector3(4.4f, .42f, .05f), dark, false);
        for (int i = 0; i < 7; i++)
            Box("BuildingPlaceholder" + i, placeholders.transform,
                new Vector3(-1.9f + i * .63f, 1.55f + (i % 3) * .2f, .22f),
                new Vector3(.54f, .9f + (i % 3) * .4f, .05f), city, false);
        Ref(motion, "placeholderCity", placeholders);
        var walkers = new Transform[5];
        for (int i = 0; i < walkers.Length; i++)
        {
            walkers[i] = Shape("PedestrianPlaceholder" + i, PrimitiveType.Capsule, street.transform,
                new Vector3(-1.8f + i * .85f, 1.10f, -.04f), new Vector3(.16f, .32f, .1f), dark).transform;
        }
        Array(motion, "pedestrians", walkers);
        var cars = new Transform[2];
        for (int i = 0; i < cars.Length; i++)
            cars[i] = Box("CarPlaceholder" + i, street.transform, new Vector3(i == 0 ? -1.3f : 1.2f, .83f, -.07f),
                new Vector3(.68f, .2f, .14f), car, false).transform;
        Array(motion, "cars", cars);
        var traffic = window.AddComponent<AudioSource>(); traffic.playOnAwake = false;
        Ref(ambience, "streetSource", traffic);
    }
    private static void BuildLight(Transform root, OfficeAtmosphere ambience, Material light, Material dark)
    {
        var fixture = new GameObject("FluorescentFixture");
        fixture.transform.SetParent(root, false);
        fixture.transform.localPosition = new Vector3(0, 3.13f, -1.2f);
        Box("HousingPlaceholder", fixture.transform, Vector3.zero, new Vector3(2.6f, .12f, .42f), dark, false);
        Box("TubePlaceholder", fixture.transform, new Vector3(0, -.08f, 0), new Vector3(2.42f, .045f, .2f), light, false);
        var lamp = fixture.AddComponent<Light>();
        lamp.type = LightType.Point; lamp.range = 9f; lamp.intensity = 1.65f;
        lamp.color = new Color(.79f, .89f, 1f);
        Ref(ambience, "ceilingLight", lamp);
        var hum = fixture.AddComponent<AudioSource>(); hum.playOnAwake = false;
        Ref(ambience, "fluorescentSource", hum);
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }
    private static Image Panel(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }
    private static TextMeshProUGUI Label(string name, Transform parent, Vector2 size, Vector2 position, int sizeText)
    {
        var label = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font; label.fontSize = sizeText; label.color = new Color(.93f, .95f, 1f);
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }
    private static void BuildUI(OfficeSceneController controller)
    {
        var canvas = GameObject.Find("UI")?.GetComponent<Canvas>();
        if (canvas == null) throw new InvalidOperationException("Gameplay UI missing in Act1");
        var objectivePanel = Panel("OfficeObjectivePanel", canvas.transform, new Vector2(850, 54), new Vector2(0, 315), new Color(.07f, .10f, .14f, .83f));
        objectivePanel.raycastTarget = false;
        var objective = Label("OfficeObjective", objectivePanel.transform, new Vector2(820, 49), Vector2.zero, 21);
        Ref(controller, "objectiveText", objective);
        var subtitlePanel = Panel("OfficeSubtitlePanel", canvas.transform, new Vector2(1040, 102), new Vector2(0, -230), new Color(.08f, .10f, .14f, .88f));
        subtitlePanel.raycastTarget = false;
        var subtitle = Label("OfficeSubtitle", subtitlePanel.transform, new Vector2(990, 88), Vector2.zero, 26);
        subtitle.color = new Color(1f, .87f, .8f);
        Ref(controller, "subtitlePanel", subtitlePanel.gameObject);
        Ref(controller, "subtitleText", subtitle);
        var progress = Panel("OfficeWorkPanel", canvas.transform, new Vector2(760, 132), new Vector2(0, -195), new Color(.08f, .10f, .14f, .95f));
        progress.raycastTarget = false;
        var label = Label("WorkLabel", progress.transform, new Vector2(710, 46), new Vector2(0, 31), 23);
        var barBackground = Panel("WorkBar", progress.transform, new Vector2(660, 26), new Vector2(0, -24), new Color(.22f, .26f, .3f));
        barBackground.raycastTarget = false;
        var fill = Rect("Fill", barBackground.transform, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        fill.color = new Color(.9f, .2f, .16f); fill.raycastTarget = false;
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        var slider = barBackground.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform; slider.targetGraphic = fill; slider.interactable = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        var ui = canvas.gameObject.AddComponent<ProgressBarUI>();
        Ref(ui, "panel", progress.gameObject);
        Ref(ui, "progressSlider", slider);
        Ref(ui, "labelText", label);
        Ref(ui, "fillImage", fill);
    }
}
