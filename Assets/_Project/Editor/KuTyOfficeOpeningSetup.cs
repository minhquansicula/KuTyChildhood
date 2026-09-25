using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Connects the dedicated Scene 1 opening without rebuilding the existing task system.</summary>
public static class KuTyOfficeOpeningSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_RealWorld.unity";
    private const string KeyboardPath = "Assets/_Project/Audio/SFX/Office/Keyboard_Distant.wav";
    private const string TrafficPath = "Assets/_Project/Audio/SFX/Office/Traffic_Outside.mp3";
    private const string DeskSlamPath = "Assets/_Project/Audio/SFX/Office/Boss_Desk_Slam.mp3";
    private const string BossVoicePath = "Assets/_Project/Audio/Voice/Boss_Opening_Report.wav";
    private const string PlayerVoicePath = "Assets/_Project/Audio/Voice/Player_Opening_Response.wav";

    [MenuItem("KuTy/Setup/Build office opening sequence")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty) { Debug.LogWarning("[KuTy] Save the current scene before building the office opening."); return; }
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        BuildInActiveScene(true);
    }

    public static bool BuildInActiveScene(bool saveScene)
    {
        GameObject officeRoot = GameObject.Find("OfficeRoot");
        GameObject player = GameObject.Find("Player");
        GameObject ui = GameObject.Find("UI");
        OfficeSceneController controller = Object.FindObjectOfType<OfficeSceneController>();
        OfficeAtmosphere atmosphere = Object.FindObjectOfType<OfficeAtmosphere>();
        FirstPersonController fps = Object.FindObjectOfType<FirstPersonController>();
        PlayerInteraction interaction = Object.FindObjectOfType<PlayerInteraction>();
        Camera camera = Camera.main;
        if (officeRoot == null || player == null || ui == null || controller == null || atmosphere == null ||
            fps == null || interaction == null || camera == null)
        {
            Debug.LogError("[KuTy] Cannot build opening: a Scene 1 core reference is missing.");
            return false;
        }

        AudioClip keyboard = AssetDatabase.LoadAssetAtPath<AudioClip>(KeyboardPath);
        AudioClip traffic = AssetDatabase.LoadAssetAtPath<AudioClip>(TrafficPath);
        AudioClip slam = AssetDatabase.LoadAssetAtPath<AudioClip>(DeskSlamPath);
        AudioClip bossClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BossVoicePath);
        AudioClip playerClip = AssetDatabase.LoadAssetAtPath<AudioClip>(PlayerVoicePath);
        if (keyboard == null || traffic == null || slam == null || bossClip == null)
        {
            Debug.LogError("[KuTy] Opening audio has not finished importing. Refresh assets and run setup again.");
            return false;
        }

        ConfigureImporter(KeyboardPath, true);
        ConfigureImporter(TrafficPath, true);
        ConfigureImporter(DeskSlamPath, true);

        Transform old = officeRoot.transform.Find("OfficeOpening");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject openingObject = new GameObject("OfficeOpening");
        openingObject.transform.SetParent(officeRoot.transform, false);
        OfficeOpeningSequence opening = openingObject.AddComponent<OfficeOpeningSequence>();

        SerializedObject atmosphereSerialized = new SerializedObject(atmosphere);
        AudioSource trafficSource = atmosphereSerialized.FindProperty("streetSource").objectReferenceValue as AudioSource;
        AudioSource airSource = atmosphereSerialized.FindProperty("fluorescentSource").objectReferenceValue as AudioSource;
        AudioSource bossSource = atmosphereSerialized.FindProperty("bossSource").objectReferenceValue as AudioSource;
        if (trafficSource == null) trafficSource = CreateAudioSource("TrafficOutside", openingObject.transform, new Vector3(-3.35f, 1.45f, .15f));
        if (airSource == null) airSource = CreateAudioSource("AirConditioner", openingObject.transform, new Vector3(0f, 2.7f, 0f));
        if (bossSource == null) bossSource = CreateAudioSource("BossVoice", openingObject.transform, new Vector3(0f, 1.35f, -3.72f));

        AudioSource keyboardSource = CreateAudioSource("KeyboardDistant", openingObject.transform, new Vector3(2.7f, 1.05f, -.65f));
        AudioSource deskSlamSource = CreateAudioSource("BossDeskSlam", openingObject.transform, bossSource.transform.position);
        AudioSource playerSource = CreateAudioSource("PlayerOpeningVoice", player.transform, Vector3.zero, true);

        ConfigureSpatial(trafficSource, .88f, 3f, 24f);
        ConfigureSpatial(airSource, .28f, 3f, 16f);
        ConfigureSpatial(keyboardSource, .72f, 2f, 13f);
        ConfigureSpatial(bossSource, 1f, 2.5f, 12f);
        ConfigureSpatial(deskSlamSource, 1f, 2.5f, 12f);
        ConfigureSpatial(playerSource, 0f, 1f, 500f);

        AudioLowPassFilter bossFilter = bossSource.GetComponent<AudioLowPassFilter>();
        if (bossFilter == null) bossFilter = bossSource.gameObject.AddComponent<AudioLowPassFilter>();
        bossFilter.cutoffFrequency = 2600f;
        bossFilter.lowpassResonanceQ = 1.2f;

        CanvasGroup blackFade = BuildBlackFade(ui.transform);
        GameObject objectivePanel = GameObject.Find("OfficeObjectivePanel");
        CanvasGroup objectiveGroup = objectivePanel != null ? objectivePanel.GetComponent<CanvasGroup>() : null;
        if (objectivePanel != null && objectiveGroup == null) objectiveGroup = objectivePanel.AddComponent<CanvasGroup>();
        if (objectiveGroup != null)
        {
            objectiveGroup.alpha = 0f;
            objectiveGroup.interactable = false;
            objectiveGroup.blocksRaycasts = false;
        }

        List<GameObject> hiddenHud = new List<GameObject>();
        GameObject crosshair = GameObject.Find("Crosshair");
        if (crosshair != null) hiddenHud.Add(crosshair);
        foreach (TextMeshProUGUI text in ui.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (text.text.Contains("WASD:") && !hiddenHud.Contains(text.gameObject)) hiddenHud.Add(text.gameObject);

        Set(opening, "playerController", fps);
        Set(opening, "playerInteraction", interaction);
        Set(opening, "playerCamera", camera.transform);
        Set(opening, "officeController", controller);
        Set(opening, "blackFade", blackFade);
        Set(opening, "objectiveCanvasGroup", objectiveGroup);
        SetArray(opening, "hideDuringOpening", hiddenHud.ToArray());
        Set(opening, "airConditioner", airSource);
        Set(opening, "trafficOutside", trafficSource);
        Set(opening, "keyboardDistant", keyboardSource);
        Set(opening, "bossVoice", bossSource);
        Set(opening, "playerVoice", playerSource);
        Set(opening, "deskSlamSource", deskSlamSource);
        Set(opening, "trafficClip", traffic);
        Set(opening, "keyboardClip", keyboard);
        Set(opening, "bossDialogueClip", bossClip);
        Set(opening, "playerResponseClip", playerClip);
        Set(opening, "deskSlamClip", slam);

        Set(controller, "openingSequence", opening);
        Set(atmosphere, "streetTraffic", traffic);
        Set(atmosphere, "deskSlam", slam);
        SetBool(atmosphere, "deferAmbienceToOpening", true);

        EditorUtility.SetDirty(opening);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(atmosphere);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if (saveScene) EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = openingObject;

        string playerVoiceStatus = playerClip != null
            ? " Player response voice is connected."
            : " Player response uses subtitles until Player_Opening_Response.wav is supplied.";
        Debug.Log("[KuTy] Office opening sequence built with real keyboard, traffic and desk-slam audio." + playerVoiceStatus);
        return true;
    }

    private static CanvasGroup BuildBlackFade(Transform ui)
    {
        Transform old = ui.Find("OfficeOpeningFade");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject go = new GameObject("OfficeOpeningFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(ui, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        CanvasGroup group = go.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = true;
        group.interactable = false;
        go.transform.SetAsLastSibling();
        return group;
    }

    private static AudioSource CreateAudioSource(string name, Transform parent, Vector3 position, bool local = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (local) go.transform.localPosition = position;
        else go.transform.position = position;
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    private static void ConfigureSpatial(AudioSource source, float spatialBlend, float minDistance, float maxDistance)
    {
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    private static void ConfigureImporter(string path, bool forceMono)
    {
        AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) return;
        importer.forceToMono = forceMono;
        importer.loadInBackground = true;
        importer.SaveAndReimport();
    }

    private static void Set(Object target, string property, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null) { Debug.LogError($"[KuTy] Missing serialized field {property} on {target}."); return; }
        field.objectReferenceValue = value;
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

    private static void SetBool(Object target, string property, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
