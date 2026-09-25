using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class KuTySmokeTests
{
    private const string Pending = "KuTy.StorySmoke.Pending";
    static KuTySmokeTests()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                new GameObject("KuTy Story Checks").AddComponent<KuTyStoryRunner>();
            }
        };
    }
    [MenuItem("KuTy/Tests/Run story smoke test")]
    public static void Run()
    {
        KuTyProjectSetup.Validate();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
}

public class KuTyStoryRunner : MonoBehaviour
{
    private int assertions;
    private bool finished;
    private float started;
    private bool previousRunInBackground;
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        Application.logMessageReceived += Log;
        StartCoroutine(Execute());
    }
    private void Update() { if (!finished && Time.realtimeSinceStartup - started > 100f) Complete(false, "Timeout"); }
    private void Log(string condition, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            Complete(false, condition + "\n" + stack);
    }
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        assertions++;
    }
    private static T Get<T>(object instance, string field)
        => (T)instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
    private static void Call(object instance, string method)
        => instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);
    private IEnumerator SceneReady(string name)
    {
        float start = Time.realtimeSinceStartup;
        while (SceneManager.GetActiveScene().name != name || SceneLoader.Instance.IsLoading)
        {
            if (Time.realtimeSinceStartup - start > 15) throw new TimeoutException("Waiting for " + name);
            yield return null;
        }
        yield return null;
    }
    private IEnumerator Execute()
    {
        yield return null;
        Check(GameManager.Instance != null && SceneLoader.Instance != null, "Bootstrap missing");
        FindObjectOfType<MainMenuUI>().OnPlayButtonClicked();
        yield return SceneReady(SceneNames.Act1);
        var office = FindObjectOfType<OfficeSceneController>();
        Check(office != null && office.Phase == OfficePhase.Opening, "Office scene did not start at the meeting room opening");
        Check(GameManager.Instance.InputBlocked && FindObjectOfType<FirstPersonController>() != null,
            "The opening reprimand should briefly lock movement");
        foreach (string objectName in new[] { "OfficeDesk", "OfficeDesk_New", "OfficeInterior_New",
                     "OfficeChair", "StreetBackdrop2D", "BossSilhouette", "FluorescentFixture",
                     "PlayerDeskAfternoonLight", "BossGlassInteriorGlow", "Scene1NarrativeProps",
                     "OfficeClockText", "PrinterPaperInteraction", "WaterCupInteraction",
                     "ColdCoffeeInteraction", "OfficeMoodVolume",
                     "OfficeExhaustionFade", "LaptopStatusText", "OfficeOpening", "OfficeOpeningFade",
                     "KeyboardDistant", "BossDeskSlam", "PlayerOpeningVoice" })
            Check(GameObject.Find(objectName) != null, "Missing office asset slot: " + objectName);
        Check(GameObject.Find("ChildhoodMarbleInteraction") == null,
            "The childhood marble should not be present in Scene 1");
        Check(GameObject.Find("OfficeDesk").GetComponent<BoxCollider>() == null,
            "The old invisible desk blocker was not removed");
        var playerDesk = GameObject.Find("OfficeDesk_New");
        var workDesk = playerDesk.GetComponent<OfficeWorkstationInteractable>();
        Check(workDesk != null && playerDesk.GetComponent<MeshCollider>() != null &&
              playerDesk.layer == LayerMask.NameToLayer("Interactable"),
            "The selected desk is not the active interaction target");
        foreach (string demoVisual in new[] { "DeskTopPlaceholder", "DeskLegPlaceholder0", "DeskLegPlaceholder1",
                     "DeskLegPlaceholder2", "DeskLegPlaceholder3", "SeatPlaceholder", "BackPlaceholder",
                     "ChairPostPlaceholder", "ChairBasePlaceholder", "BossBodyPlaceholder", "BossHeadPlaceholder",
                     "BossArmPlaceholder" })
            Check(GameObject.Find(demoVisual) == null, "Demo visual was not removed: " + demoVisual);
        Check(Get<Transform>(office, "playerTransform") == FindObjectOfType<FirstPersonController>().transform &&
              Get<Transform>(office, "chairAnchor") == GameObject.Find("office-chair-main-character").transform,
            "The sitting anchor is not wired to office-chair-main-character");
        Check(Get<TMPro.TextMeshPro>(office, "screenWarning") != null &&
              Get<TMPro.TextMeshPro>(office, "clockText") != null &&
              Get<OfficeExhaustionSequence>(office, "exhaustionSequence") != null &&
              Get<OfficeOpeningSequence>(office, "openingSequence") != null,
            "Scene 1 narrative references are incomplete");
        Check(!GameObject.Find("UI").GetComponent<StoryPrologueController>().enabled,
            "Old black-screen prologue remains active");
        var ambience = FindObjectOfType<OfficeAtmosphere>();
        Check(Get<AudioSource>(ambience, "streetSource").clip != null &&
              Get<AudioSource>(ambience, "fluorescentSource").clip != null, "Office ambient placeholders missing");
        Check(Get<AudioClip>(ambience, "bossFirstLine") != null, "Boss opening voice is not assigned");
        var opening = FindObjectOfType<OfficeOpeningSequence>();
        Check(opening != null && Get<AudioClip>(opening, "keyboardClip") != null &&
              Get<AudioClip>(opening, "trafficClip") != null && Get<AudioClip>(opening, "deskSlamClip") != null,
            "Opening audio from Dumb Assets is not connected");
        var interactions = FindObjectsOfType<OfficeInteractable>();
        Func<OfficeInteractionKind, OfficeInteractable> find = kind =>
            interactions.First(o => Get<OfficeInteractionKind>(o, "kind") == kind);
        yield return new WaitForSecondsRealtime(1.3f);
        Check(GameManager.Instance.InputBlocked && Get<CanvasGroup>(opening, "blackFade").alpha > .99f &&
              !Get<GameObject>(office, "subtitlePanel").activeSelf,
            "Opening did not begin on a locked, UI-free black screen");
        float openingWaitStarted = Time.realtimeSinceStartup;
        while (!opening.IsComplete)
        {
            if (Time.realtimeSinceStartup - openingWaitStarted > 30f)
                throw new TimeoutException("Office opening did not complete");
            yield return null;
        }
        Check(office.Phase == OfficePhase.ReturnToDesk && !GameManager.Instance.InputBlocked &&
              !FindObjectOfType<FirstPersonController>().IsMovementLocked &&
              !FindObjectOfType<PlayerInteraction>().IsInteractionLocked &&
              Get<CanvasGroup>(opening, "objectiveCanvasGroup").alpha > .99f,
            "Opening did not restore control and fade in the objective");
        Capture("office-view.png");
        var player = FindObjectOfType<FirstPersonController>().transform;
        var eye = Camera.main.transform;
        Vector3 originalPlayerPosition = player.position;
        Quaternion originalEyeRotation = eye.localRotation;
        player.position = new Vector3(1.4f, .45f, 1.7f);
        eye.LookAt(playerDesk.GetComponent<Renderer>().bounds.center);
        Physics.SyncTransforms();
        Check(Physics.Raycast(eye.position, eye.forward, out RaycastHit laptopHit, 3f) &&
              laptopHit.collider.GetComponentInParent<OfficeWorkstationInteractable>() == workDesk,
              "Selected desk is not reachable by the player's gaze raycast");
        player.position = new Vector3(0, .45f, -2.5f);
        eye.LookAt(GameObject.Find("BossSilhouette").transform.position + Vector3.up * 1.35f);
        Physics.SyncTransforms();
        Check(Physics.Raycast(eye.position, eye.forward, out RaycastHit bossHit, 3f) &&
              bossHit.collider.GetComponentInParent<OfficeInteractable>() == find(OfficeInteractionKind.Boss),
              "Boss behind the glass is not reachable by the player's gaze raycast");
        player.position = originalPlayerPosition;
        eye.localRotation = originalEyeRotation;
        Physics.SyncTransforms();
        find(OfficeInteractionKind.Boss).Interact();
        Check(!GameManager.Instance.InputBlocked, "Boss inspection should keep exploration free");
        workDesk.Interact();
        yield return new WaitForSecondsRealtime(.5f);
        workDesk.Interact();
        Check(office.Phase == OfficePhase.ProcessEmails && office.IsWorking && GameManager.Instance.InputBlocked,
            "Email task did not start");
        var reportGame = FindObjectOfType<OfficeReportMiniGame>();
        Check(reportGame != null && reportGame.IsOpen, "Report mini-game UI did not open");
        reportGame.ChooseOption(0);
        Check(reportGame.CurrentTask == OfficeWorkTask.Emails && reportGame.ItemIndex == 0,
            "Wrong email choice advanced the work task");
        office.CancelWork();
        Check(!office.IsWorking && !GameManager.Instance.InputBlocked && !reportGame.IsOpen,
            "Report mini-game cancellation failed");

        workDesk.Interact();
        reportGame.ChooseOption(1);
        yield return new WaitForSecondsRealtime(.7f);
        reportGame.ChooseOption(2);
        yield return new WaitForSecondsRealtime(.7f);
        reportGame.ChooseOption(0);
        yield return new WaitForSecondsRealtime(.9f);
        Check(office.Phase == OfficePhase.SortDocuments && !office.IsWorking,
            "Email processing did not advance to document sorting");

        workDesk.Interact();
        Check(reportGame.CurrentTask == OfficeWorkTask.Documents && reportGame.IsOpen,
            "Document sorting did not open");
        reportGame.ChooseOption(0);
        yield return new WaitForSecondsRealtime(.7f);
        reportGame.ChooseOption(1);
        yield return new WaitForSecondsRealtime(.7f);
        reportGame.ChooseOption(2);
        yield return new WaitForSecondsRealtime(.7f);
        reportGame.ChooseOption(2);
        yield return new WaitForSecondsRealtime(.9f);
        Check(office.Phase == OfficePhase.FixReport && !office.IsWorking,
            "Document sorting did not advance to report correction");

        workDesk.Interact();
        Check(reportGame.CurrentTask == OfficeWorkTask.Report && reportGame.IsOpen,
            "Report correction did not open");
        reportGame.ChooseOption(2);
        yield return new WaitForSecondsRealtime(.7f);
        reportGame.ChooseOption(1);
        yield return new WaitForSecondsRealtime(.7f);
        reportGame.ChooseOption(3);
        yield return new WaitForSecondsRealtime(.7f);
        reportGame.ChooseOption(0);
        reportGame.ChooseOption(1);
        reportGame.ChooseOption(2);
        reportGame.ChooseOption(3);
        yield return new WaitForSecondsRealtime(1.1f);
        Check(office.Phase == OfficePhase.AfterWork && !GameManager.Instance.InputBlocked,
            "Report completion did not start the free-roam aftermath");
        office.MakeRestAvailableForTests();
        Check(office.Phase == OfficePhase.Rest, "Exhaustion objective did not become available");
        find(OfficeInteractionKind.Chair).Interact();
        Check(office.Phase == OfficePhase.Leaving && GameManager.Instance.InputBlocked,
            "Chair did not start exhausted transition");
        yield return SceneReady(SceneNames.Act2);

        var quests = QuestManager.Instance;
        var memories = MemoryCollectionManager.Instance;
        var currency = CurrencyManager.Instance;
        var inventory = InventoryManager.Instance;
        var dishes = FindObjectOfType<DishWashingGame>();
        var marbles = FindObjectOfType<MarbleShootingGame>();
        var journal = FindObjectOfType<JournalInteractable>();
        var shop = ShopManager.Instance;
        Check(quests != null && quests.CurrentStep == QuestStep.ReadJournal, "Quest did not start at journal");
        Check(!quests.CanShop && !quests.CanWashDishes && !quests.CanPlayMarbles, "Sequence gates missing");
        dishes.StartGame();
        Check(currency.CurrentMoney == 0, "Dishes started before journal");
        journal.Interact();
        DialogueUI.Instance.HideDialogue();
        Check(JournalUI.Instance.IsOpen && GameManager.Instance.InputBlocked, "Journal did not open modal");
        JournalUI.Instance.Close();
        Check(quests.CurrentStep == QuestStep.WashDishes && !GameManager.Instance.InputBlocked, "Journal progression failed");
        dishes.StartGame();
        dishes.WashFor(1);
        dishes.CancelGame();
        Check(currency.CurrentMoney == 0, "Dish cancel paid reward");
        dishes.StartGame();
        for (int i = 0; i < 5; i++) dishes.WashFor(3);
        Check(dishes.IsCompleted && currency.CurrentMoney == 2000, "Dish reward must be 2000đ");
        Check(quests.CurrentStep == QuestStep.BuyCandyAndMarbles && memories.HasCollected(MemoryType.FilialLove), "Memory #1 missing");
        DialogueUI.Instance.HideDialogue();
        dishes.StartGame(); dishes.WashFor(100);
        Check(currency.CurrentMoney == 2000, "Dish reward duplicated");
        Check(shop.ShopItems.Count == 2 && shop.ShopItems.Sum(i => i.price) == 2000, "Shop economy incorrect");
        var invalid = ScriptableObject.CreateInstance<ItemData>(); invalid.price = 1;
        Check(!shop.PurchaseItem(invalid) && currency.CurrentMoney == 2000, "Non-stock purchase succeeded");
        Destroy(invalid);
        ShopUI.Instance.OpenShop();
        yield return null;
        Check(ShopUI.Instance.IsOpen && GameManager.Instance.InputBlocked, "Shop did not lock input");
        Check(FindObjectsOfType<ShopItemSlotUI>().Length == 2, "Expected two shop slots");
        Check(shop.PurchaseItem(quests.Candy) && currency.CurrentMoney == 1500, "Candy purchase failed");
        Check(memories.CollectedCount == 1, "Memory #2 arrived before both purchases");
        Check(!shop.PurchaseItem(quests.Candy) && currency.CurrentMoney == 1500, "Duplicate candy charged money");
        Check(shop.PurchaseItem(quests.MarbleJar), "Marble jar purchase failed");
        Check(currency.CurrentMoney == 0 && inventory.OwnedItems.Count == 2, "Shop accounting failed");
        Check(quests.CurrentStep == QuestStep.PlayMarbles && memories.HasCollected(MemoryType.SimpleJoy), "Memory #2 missing");
        DialogueUI.Instance.HideDialogue();
        Check(!GameManager.Instance.InputBlocked, "Shop/dialogue left input blocked");

        marbles.StartGame();
        Check(marbles.State == MarbleShootingGame.PlayState.Aiming, "Marble game did not start");
        var targets = Get<System.Collections.Generic.List<Rigidbody>>(marbles, "targetMarbles");
        for (int i = 0; i < 3; i++) targets[i].position = new Vector3(5 + i, 1, 5);
        Call(marbles, "EvaluateShot");
        Check(marbles.IsCompleted && quests.CurrentStep == QuestStep.ReturnToJournal, "Marble win did not advance quest");
        Check(memories.HasCollected(MemoryType.Freedom) && memories.CollectedCount == 3, "Memory #3 missing");
        Check(SceneManager.GetActiveScene().name == SceneNames.Act2, "Three memories skipped key crafting");
        DialogueUI.Instance.HideDialogue();
        journal.Interact();
        DialogueUI.Instance.HideDialogue();
        Check(quests.CurrentStep == QuestStep.TakeDreamKey, "Journal did not craft key");
        JournalUI.Instance.Close();
        var key = FindObjectOfType<DreamKeyInteractable>();
        Check(key != null && key.gameObject.activeInHierarchy, "Dream key visual absent");
        key.Interact();
        DialogueUI.Instance.HideDialogue();
        Check(quests.HasDreamKey && quests.CurrentStep == QuestStep.UnlockFrontDoor, "Could not take key");
        var chained = FindObjectOfType<ChainedDoorInteractable>();
        chained.Interact();
        Check(quests.CurrentStep == QuestStep.Ending, "Chained door did not unlock");
        yield return SceneReady(SceneNames.Act3);
        Check(FindObjectOfType<EndingUI>() != null, "Ending scene missing");
        FindObjectOfType<EndingUI>().OnPlayAgainClicked();
        yield return SceneReady(SceneNames.MainMenu);
        FindObjectOfType<MainMenuUI>().OnPlayButtonClicked();
        yield return SceneReady(SceneNames.Act1);
        GameManager.Instance.EnterMemoryWorld();
        yield return SceneReady(SceneNames.Act2);
        Check(CurrencyManager.Instance.CurrentMoney == 0 && InventoryManager.Instance.OwnedItems.Count == 0 &&
            MemoryCollectionManager.Instance.CollectedCount == 0 && QuestManager.Instance.CurrentStep == QuestStep.ReadJournal,
            "Replay retained old progress");
        Complete(true, "Complete story flow passed");
    }
    private void Complete(bool success, string reason)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= Log;
        Application.runInBackground = previousRunInBackground;
        File.WriteAllText("smoke-results.txt", (success ? "PASS" : "FAIL") + " (" + assertions + " assertions)\n" + reason);
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
        else { Debug.Log(reason); EditorApplication.isPlaying = false; }
    }
    private static void Capture(string path)
    {
        var camera = Camera.main;
        var canvases = FindObjectsOfType<Canvas>().Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = new RenderTexture(1280, 720, 24);
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            foreach (var item in canvases)
            {
                item.renderMode = RenderMode.ScreenSpaceCamera;
                item.worldCamera = camera;
                item.planeDistance = .5f;
            }
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            foreach (var item in canvases) { item.renderMode = RenderMode.ScreenSpaceOverlay; item.worldCamera = null; }
            target.Release();
            Destroy(target);
            Destroy(pixels);
        }
    }
}
