using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Idempotent scene authoring for the interactions and mood described in the Scene 1 document.</summary>
public static class KuTyScene1CompletionSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_RealWorld.unity";
    private const string GeneratedPath = "Assets/_Project/Generated";

    [MenuItem("KuTy/Setup/Complete Scene 1 from design document")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty) { Debug.LogWarning("[KuTy] Save the current scene before completing Scene 1."); return; }
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        KuTyOfficeReportMiniGameSetup.Build();
        GameObject officeRoot = GameObject.Find("OfficeRoot");
        GameObject ui = GameObject.Find("UI");
        GameObject player = GameObject.Find("Player");
        Camera playerCamera = Camera.main;
        OfficeSceneController controller = Object.FindObjectOfType<OfficeSceneController>();
        OfficeAtmosphere atmosphere = Object.FindObjectOfType<OfficeAtmosphere>();
        if (officeRoot == null || ui == null || player == null || playerCamera == null || controller == null || atmosphere == null)
        {
            Debug.LogError("[KuTy] Scene 1 core objects are missing.");
            return;
        }

        RemoveLegacyOfficeUI(ui.transform);
        Transform oldProps = officeRoot.transform.Find("Scene1NarrativeProps");
        if (oldProps != null) Object.DestroyImmediate(oldProps.gameObject);
        GameObject props = new GameObject("Scene1NarrativeProps");
        props.transform.SetParent(officeRoot.transform, false);

        Material paper = MaterialAsset("OfficeNarrativePaper", new Color(.89f, .91f, .87f));
        Material coffee = MaterialAsset("OfficeColdCoffee", new Color(.19f, .09f, .045f));
        Material blue = MaterialAsset("OfficeWaterGlass", new Color(.19f, .52f, .72f, .7f));
        Material marble = MaterialAsset("OfficeMemoryMarble", new Color(.95f, .63f, .12f));
        Material silhouette = MaterialAsset("OfficeStreetSilhouette", new Color(.08f, .11f, .16f));
        TMP_FontAsset font = GameObject.Find("OfficeObjective")?.GetComponent<TextMeshProUGUI>()?.font ?? TMP_Settings.defaultFontAsset;

        TextMeshPro clock = CreateWorldText("OfficeClockText", props.transform, "17:42", font,
            new Vector3(-.82f, 1.73f, -3.43f), Quaternion.Euler(0f, 180f, 0f), .065f, 8f, Color.white);
        BoxCollider clockCollider = clock.gameObject.AddComponent<BoxCollider>();
        clockCollider.size = new Vector3(3.1f, 1.25f, .25f);
        AddMicro(clock.gameObject, controller, OfficeMicroInteractionKind.Clock, false);
        TextMeshPro deadline = CreateWorldText("DeadlineNote", props.transform, "DEADLINE  18:00", font,
            new Vector3(-.82f, 1.49f, -3.42f), Quaternion.Euler(0f, 180f, 0f), .03f, 7f, new Color(1f, .42f, .35f));
        deadline.alignment = TextAlignmentOptions.Center;

        GameObject paperProp = Primitive("PrinterPaperInteraction", PrimitiveType.Cube, props.transform,
            new Vector3(-2.92f, 1.05f, -.78f), new Vector3(.35f, .018f, .25f), paper);
        AddMicro(paperProp, controller, OfficeMicroInteractionKind.PrinterPaper, true);
        GameObject waterProp = Primitive("WaterCupInteraction", PrimitiveType.Cylinder, props.transform,
            new Vector3(-1.02f, 1.02f, -2.82f), new Vector3(.07f, .12f, .07f), blue);
        AddMicro(waterProp, controller, OfficeMicroInteractionKind.WaterCooler, false);
        GameObject coffeeProp = Primitive("ColdCoffeeInteraction", PrimitiveType.Cylinder, props.transform,
            new Vector3(2.13f, 1.70f, .40f), new Vector3(.075f, .11f, .075f), coffee);
        AddMicro(coffeeProp, controller, OfficeMicroInteractionKind.ColdCoffee, false);
        GameObject marbleProp = Primitive("ChildhoodMarbleInteraction", PrimitiveType.Sphere, props.transform,
            new Vector3(.45f, 1.68f, .36f), Vector3.one * .055f, marble);
        AddMicro(marbleProp, controller, OfficeMicroInteractionKind.ChildhoodMarble, true);

        TextMeshPro laptopScreen = BuildLaptopScreen(controller, font);
        Light[] ceilingLights = BuildMoodLights(props.transform);
        BuildMovingStreet(officeRoot.transform, silhouette);

        AudioSource propAudio = CreateAudioSource("OfficePropAudio", props.transform, new Vector3(1.4f, 1.2f, .4f));
        AudioSource memoryAudio = CreateAudioSource("OfficeMemoryAudio", props.transform, playerCamera.transform.position);
        Volume moodVolume = BuildMoodVolume(props.transform);
        Image blackout = BuildBlackout(ui.transform);
        OfficeExhaustionSequence exhaustion = props.gameObject.AddComponent<OfficeExhaustionSequence>();
        Set(exhaustion, "cameraTransform", playerCamera.transform);
        Set(exhaustion, "blackout", blackout);
        Set(exhaustion, "moodVolume", moodVolume);
        Set(exhaustion, "atmosphere", atmosphere);

        UniversalAdditionalCameraData cameraData = playerCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        EditorUtility.SetDirty(cameraData);

        Light afternoon = GameObject.Find("PlayerDeskAfternoonLight")?.GetComponent<Light>();
        if (afternoon != null)
        {
            afternoon.intensity = 6.5f;
            afternoon.color = new Color(1f, .62f, .30f);
            afternoon.shadows = LightShadows.Soft;
            EditorUtility.SetDirty(afternoon);
        }

        Set(atmosphere, "propSource", propAudio);
        Set(atmosphere, "memorySource", memoryAudio);
        SetArray(atmosphere, "ceilingLights", ceilingLights);
        Set(controller, "screenWarning", laptopScreen);
        Set(controller, "clockText", clock);
        Set(controller, "exhaustionSequence", exhaustion);
        Set(controller, "afternoonLight", afternoon);

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.SetPositionAndRotation(new Vector3(0f, .45f, -2.15f), Quaternion.Euler(0f, 180f, 0f));
        if (cc != null) cc.enabled = true;

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(atmosphere);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = props;
        Debug.Log("[KuTy] Scene 1 completed: opening position, interactions, work UI, moving street, mood lighting and exhaustion transition.");
    }

    private static void RemoveLegacyOfficeUI(Transform ui)
    {
        Transform legacy = ui.Find("OfficeWorkPanel");
        if (legacy != null) Object.DestroyImmediate(legacy.gameObject);
        foreach (TextMeshProUGUI text in ui.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string value = text.text.Trim();
            if (value == "Mẹ" || value == "Niềm vui" || value == "Bạn bè" || value == "0 đồng")
                text.gameObject.SetActive(false);
            if (text.text.Contains("Giữ chuột trái: làm việc"))
                text.text = "WASD: di chuyển  ·  Chuột: nhìn  ·  E: tương tác  ·  Click: chọn công việc  ·  Esc: tạm dừng";
        }
        for (int i = 0; i < 3; i++)
        {
            Transform memory = ui.Find("Memory" + i);
            if (memory != null) memory.gameObject.SetActive(false);
        }
    }

    private static TextMeshPro BuildLaptopScreen(OfficeSceneController controller, TMP_FontAsset font)
    {
        GameObject anchor = GameObject.Find("OfficeScreenTextV3");
        if (anchor == null)
        {
            anchor = new GameObject("OfficeScreenTextV3");
            anchor.transform.SetParent(GameObject.Find("OfficeDesk_New")?.transform, true);
        }
        for (int i = anchor.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(anchor.transform.GetChild(i).gameObject);
        TextMeshPro text = CreateWorldText("LaptopStatusText", anchor.transform, "3 EMAIL KHẨN\nDEADLINE 18:00", font,
            new Vector3(1.40f, 1.58f, -.045f), Quaternion.Euler(0f, 180f, 0f), .014f, 7f, new Color(1f, .18f, .14f));
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        return text;
    }

    private static Light[] BuildMoodLights(Transform parent)
    {
        Vector3[] positions = { new Vector3(1.3f, 2.86f, -1.65f), new Vector3(-1.35f, 2.86f, 1.38f) };
        Light[] lights = new Light[positions.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            GameObject go = new GameObject("OfficeFluorescentLight_" + (char)('A' + i));
            go.transform.SetParent(parent, false);
            go.transform.position = positions[i];
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(.72f, .82f, 1f);
            light.intensity = .7f;
            light.range = 4.2f;
            light.shadows = LightShadows.None;
            lights[i] = light;
        }
        return lights;
    }

    private static void BuildMovingStreet(Transform officeRoot, Material material)
    {
        OfficeStreetMotion motion = officeRoot.GetComponentInChildren<OfficeStreetMotion>(true);
        if (motion == null) return;
        Transform old = motion.transform.Find("StreetMotionProps");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject root = new GameObject("StreetMotionProps");
        root.transform.SetParent(motion.transform, false);
        Transform[] people = new Transform[4];
        for (int i = 0; i < people.Length; i++)
        {
            GameObject person = Primitive("Pedestrian_" + (i + 1), PrimitiveType.Capsule, root.transform,
                new Vector3(-1.8f + i * 1.05f, .36f, .08f), new Vector3(.075f, .19f, .075f), material);
            person.transform.localPosition = new Vector3(-1.8f + i * 1.05f, -.58f + (i % 2) * .12f, .04f);
            Object.DestroyImmediate(person.GetComponent<Collider>());
            people[i] = person.transform;
        }
        Transform[] cars = new Transform[2];
        for (int i = 0; i < cars.Length; i++)
        {
            GameObject car = Primitive("Car_" + (i + 1), PrimitiveType.Cube, root.transform,
                Vector3.zero, new Vector3(.48f, .12f, .13f), material);
            car.transform.localPosition = new Vector3(-1.5f + i * 2.7f, -.82f - i * .15f, .02f);
            Object.DestroyImmediate(car.GetComponent<Collider>());
            cars[i] = car.transform;
        }
        SetArray(motion, "pedestrians", people);
        SetArray(motion, "cars", cars);
    }

    private static Volume BuildMoodVolume(Transform parent)
    {
        string path = GeneratedPath + "/OfficeMoodProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        foreach (VolumeComponent component in profile.components.ToArray()) profile.Remove(component.GetType());
        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(.2f);
        vignette.smoothness.Override(.32f);
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.saturation.Override(-12f);
        color.postExposure.Override(-.12f);
        color.colorFilter.Override(new Color(.88f, .93f, 1f));
        DepthOfField depth = profile.Add<DepthOfField>(false);
        depth.mode.Override(DepthOfFieldMode.Gaussian);
        EditorUtility.SetDirty(profile);

        GameObject go = new GameObject("OfficeMoodVolume");
        go.transform.SetParent(parent, false);
        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
        return volume;
    }

    private static Image BuildBlackout(Transform ui)
    {
        Transform old = ui.Find("OfficeExhaustionFade");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject go = new GameObject("OfficeExhaustionFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(ui, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;
        go.transform.SetAsLastSibling();
        return image;
    }

    private static TextMeshPro CreateWorldText(string name, Transform parent, string value, TMP_FontAsset font,
        Vector3 worldPosition, Quaternion worldRotation, float scale, float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(TextMeshPro));
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(worldPosition, worldRotation);
        go.transform.localScale = Vector3.one * scale;
        TextMeshPro text = go.GetComponent<TextMeshPro>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        return text;
    }

    private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return go;
    }

    private static void AddMicro(GameObject go, OfficeSceneController controller, OfficeMicroInteractionKind kind, bool oneTime)
    {
        go.layer = LayerMask.NameToLayer("Interactable");
        OfficeMicroInteractable interaction = go.AddComponent<OfficeMicroInteractable>();
        Set(interaction, "office", controller);
        SetEnum(interaction, "kind", (int)kind);
        SetBool(interaction, "isOneTimeUse", oneTime);
    }

    private static AudioSource CreateAudioSource(string name, Transform parent, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        return go.AddComponent<AudioSource>();
    }

    private static Material MaterialAsset(string name, Color color)
    {
        string path = GeneratedPath + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void Set(Object target, string property, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray<T>(Object target, string property, T[] values) where T : Object
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty array = serialized.FindProperty(property);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string property, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).enumValueIndex = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string property, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
