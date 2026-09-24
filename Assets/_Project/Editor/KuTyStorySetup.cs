using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Non-destructive upgrade for the generated greybox: only named prototype objects are changed.
public static class KuTyStorySetup
{
    private const string Root = "Assets/_Project";
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Generated/Vietnamese.asset");
    private static Material Wood => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Generated/Wood.mat");
    private static Material Clean => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Generated/Clean.mat");
    private static Material Blue => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Generated/Blue.mat");
    private static string Path(string name) => Root + "/Scenes/" + name + ".unity";

    [MenuItem("KuTy/Setup/Upgrade existing scenes to story flow")]
    public static void UpgradeMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        UpgradeScenes();
        EditorSceneManager.OpenScene(Path(SceneNames.MainMenu));
    }
    public static void UpgradeBatch()
    {
        try { UpgradeScenes(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }
    public static void UpgradeScenes()
    {
        UpgradeAct1();
        UpgradeAct2();
        UpgradeAct3();
        AssetDatabase.SaveAssets();
        Debug.Log("KUTY_STORY_UPGRADE_PASSED");
    }
    private static T Require<T>(string name) where T : Component
    {
        var go = GameObject.Find(name);
        if (go == null || !go.TryGetComponent(out T component)) throw new InvalidOperationException("Missing generated object: " + name);
        return component;
    }
    private static void Ref(UnityEngine.Object owner, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(owner);
        var property = data.FindProperty(field);
        if (property == null) throw new MissingFieldException(owner.GetType().Name, field);
        property.objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Int(UnityEngine.Object owner, string field, int value)
    {
        var data = new SerializedObject(owner);
        data.FindProperty(field).intValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void String(UnityEngine.Object owner, string field, string value)
    {
        var data = new SerializedObject(owner);
        data.FindProperty(field).stringValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Array(UnityEngine.Object owner, string field, params UnityEngine.Object[] values)
    {
        var data = new SerializedObject(owner);
        var property = data.FindProperty(field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
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
    private static TextMeshProUGUI Label(string name, Transform parent, string value, Vector2 size, Vector2 position, int fontSize)
    {
        var label = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font;
        label.fontSize = fontSize;
        label.text = value;
        label.color = new Color(1f, 0.96f, 0.84f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }
    private static Image Panel(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }
    private static Button CloseButton(Transform parent, Vector2 position)
    {
        var image = Panel("CloseJournal", parent, new Vector2(190, 50), position, new Color(.34f, .25f, .17f));
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Label("Text", image.transform, "Đóng (Esc)", new Vector2(180, 45), Vector2.zero, 21);
        return button;
    }
    private static void UpgradeAct1()
    {
        EditorSceneManager.OpenScene(Path(SceneNames.Act1));
        ReplaceWorldSign("Chạm vào kỷ vật trên bàn", "Mở cánh cửa tuổi thơ");
        if (GameObject.Find("ChildhoodDoor") != null) { EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); return; }
        var door = Require<MemoryTrigger>("Keepsake");
        door.gameObject.name = "ChildhoodDoor";
        door.transform.position = new Vector3(0, 1.25f, 0);
        door.transform.localScale = new Vector3(2.1f, 2.5f, .28f);
        door.GetComponent<Renderer>().sharedMaterial = Wood;
        String(door, "promptText", "[E] Mở cánh cửa tuổi thơ");
        var table = GameObject.Find("OldTable");
        if (table != null) table.SetActive(false);
        var canvas = Require<Canvas>("UI");
        var prologue = canvas.GetComponent<StoryPrologueController>() ?? canvas.gameObject.AddComponent<StoryPrologueController>();
        var overlay = Panel("PrologueOverlay", canvas.transform, new Vector2(1280, 720), Vector2.zero, Color.black);
        overlay.rectTransform.anchorMin = Vector2.zero;
        overlay.rectTransform.anchorMax = Vector2.one;
        overlay.rectTransform.offsetMin = overlay.rectTransform.offsetMax = Vector2.zero;
        var line = Label("Subtitle", overlay.transform, "", new Vector2(1100, 240), new Vector2(0, -130), 35);
        Ref(prologue, "overlay", overlay.gameObject);
        Ref(prologue, "background", overlay);
        Ref(prologue, "subtitle", line);
        Ref(prologue, "audioSource", canvas.GetComponent<AudioSource>() ?? canvas.gameObject.AddComponent<AudioSource>());
        Ref(door, "prologue", prologue);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
    private static void UpgradeAct2()
    {
        EditorSceneManager.OpenScene(Path(SceneNames.Act2));
        ReplaceWorldSign("Con diều tuổi thơ", "Kẹo mút và hũ bi ve");
        if (GameObject.Find("Journal") != null) { EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); return; }
        var systems = GameObject.Find("Act2 Systems");
        if (systems == null) throw new InvalidOperationException("Act2 Systems missing");
        var quests = systems.GetComponent<QuestManager>() ?? systems.AddComponent<QuestManager>();
        var candy = AssetDatabase.LoadAssetAtPath<ItemData>(Root + "/ScriptableObjects/Items/Item0.asset");
        var jar = AssetDatabase.LoadAssetAtPath<ItemData>(Root + "/ScriptableObjects/Items/Item2.asset");
        if (candy == null || jar == null) throw new InvalidOperationException("Shop item assets missing");
        candy.itemName = "Kẹo mút"; candy.price = 500; candy.itemType = ItemType.Candy;
        candy.description = "Viên kẹo ngọt của buổi chiều tuổi thơ."; candy.isSpecialItem = false;
        jar.itemName = "Hũ bi ve"; jar.price = 1500; jar.itemType = ItemType.Toy;
        jar.description = "Mang ra sân để chơi bắn bi."; jar.isSpecialItem = false;
        EditorUtility.SetDirty(candy); EditorUtility.SetDirty(jar);
        var shop = systems.GetComponent<ShopManager>();
        Array(shop, "shopItems", candy, jar);
        var dishes = Require<DishWashingGame>("DishWashingGame");
        var marbles = Require<MarbleShootingGame>("MarbleShootingGame");
        Int(dishes, "moneyPerDish", 400);
        Ref(quests, "candy", candy); Ref(quests, "marbleJar", jar);
        Ref(quests, "dishGame", dishes); Ref(quests, "marbleGame", marbles);
        Ref(quests, "shop", shop); Ref(quests, "memories", systems.GetComponent<MemoryCollectionManager>());
        Ref(quests, "inventory", systems.GetComponent<InventoryManager>());
        var player = GameObject.Find("Player");
        var playerCamera = player != null ? player.transform.Find("PlayerCamera") : null;
        if (playerCamera != null) playerCamera.localPosition = new Vector3(0, 1.3f, 0);

        var canvas = Require<Canvas>("UI");
        var hud = canvas.GetComponent<HUDController>();
        var objective = Label("Objective", canvas.transform, "", new Vector2(920, 50), new Vector2(0, 309), 20);
        Ref(hud, "objectiveText", objective);
        var icons = new[] { "Mẹ", "Niềm vui", "Tự do" };
        var labels = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var label in labels)
        {
            if (label.text == "Bạn bè") label.text = icons[1];
            else if (label.text == "Niềm vui") label.text = icons[2];
        }

        Cube("JournalTable", new Vector3(-3.5f, .48f, -.7f), new Vector3(2.2f, .96f, 1.3f), Wood);
        var journal = Cube("Journal", new Vector3(-3.5f, 1.02f, -.7f), new Vector3(.9f, .08f, .65f), Clean);
        journal.layer = LayerMask.NameToLayer("Interactable");
        journal.AddComponent<JournalInteractable>();
        var journalPanel = Panel("JournalPanel", canvas.transform, new Vector2(930, 610), Vector2.zero, new Color(.25f, .17f, .1f, .98f));
        var body = Label("JournalBody", journalPanel.transform, "", new Vector2(850, 490), new Vector2(0, 38), 22);
        body.alignment = TextAlignmentOptions.TopLeft;
        var journalUI = canvas.gameObject.AddComponent<JournalUI>();
        Ref(journalUI, "panel", journalPanel.gameObject);
        Ref(journalUI, "bodyText", body);
        Ref(journalUI, "closeButton", CloseButton(journalPanel.transform, new Vector2(0, -270)));

        var key = new GameObject("DreamKey");
        key.transform.position = new Vector3(-3.5f, 1.95f, -.7f);
        var keyVisual = Cube("DreamKeyVisual", key.transform.position, new Vector3(.12f, .75f, .12f), Clean, key.transform);
        var teeth = Cube("KeyTeeth", key.transform.position + new Vector3(.22f, -.2f, 0), new Vector3(.4f, .12f, .12f), Clean, keyVisual.transform);
        teeth.GetComponent<Collider>().enabled = false;
        var keyCollider = keyVisual.GetComponent<Collider>();
        keyVisual.layer = LayerMask.NameToLayer("Interactable");
        keyVisual.AddComponent<DreamKeyInteractable>();
        var keyController = key.AddComponent<DreamKeyController>();
        Ref(keyController, "keyVisual", keyVisual);
        var chainedDoor = Cube("ChainedDoor", new Vector3(8, 1.25f, -8.65f), new Vector3(2.2f, 2.5f, .3f), Wood);
        chainedDoor.layer = LayerMask.NameToLayer("Interactable");
        var chainRoot = new GameObject("Chains");
        chainRoot.transform.SetParent(chainedDoor.transform, false);
        Cube("ChainA", new Vector3(8, 1.25f, -8.42f), new Vector3(2.4f, .13f, .13f), Blue, chainRoot.transform).GetComponent<Collider>().enabled = false;
        Cube("ChainB", new Vector3(8, 1.65f, -8.42f), new Vector3(2.4f, .13f, .13f), Blue, chainRoot.transform).GetComponent<Collider>().enabled = false;
        var exit = chainedDoor.AddComponent<ChainedDoorInteractable>();
        Ref(exit, "chainsVisual", chainRoot);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
    private static void UpgradeAct3()
    {
        EditorSceneManager.OpenScene(Path(SceneNames.Act3));
        var ending = Require<EndingUI>("UI");
        String(ending, "endingMessage", "Thế giới của người lớn luôn đầy những cơn bão.\nNhưng đứa trẻ bên trong bạn, cùng những ký ức tươi đẹp này,\nsẽ luôn là nơi trú ẩn an toàn nhất.\nNgày mai trời lại sáng.");
        var labels = ending.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var label in labels)
            if (label.text.StartsWith("Gia đình, bạn bè")) label.text = "Trước hiên nhà hiện tại, Quân thở phào nhẹ nhõm.";
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
    private static void ReplaceWorldSign(string oldText, string newText)
    {
        foreach (var label in UnityEngine.Object.FindObjectsOfType<TextMeshPro>())
            if (label.text.Contains(oldText)) label.text = label.text.Replace(oldText, newText);
    }
}
