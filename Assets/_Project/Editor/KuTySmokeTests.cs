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
    [MenuItem("KuTy/Tests/Run merged memory chapter smoke test")]
    public static void RunMemoryChapter()
    {
        KuTyCheckpointTestScope.Capture();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity");
        SessionState.SetBool("KuTy.StorySmoke.ChapterOnly", true);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("KuTy/Tests/Run story smoke test")]
    public static void Run()
    {
        KuTyProjectSetup.Validate();
        KuTyCheckpointTestScope.Capture();
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
    private void Update() { if (!finished && Time.realtimeSinceStartup - started > 180f) Complete(false, "Timeout"); }
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
            if (Time.realtimeSinceStartup - start > 45) throw new TimeoutException("Waiting for " + name);
            yield return null;
        }
        yield return null;
    }
    private IEnumerator Execute()
    {
        yield return null;
        if (!SessionState.GetBool("KuTy.StorySmoke.ChapterOnly", false))
        {
            Check(GameManager.Instance != null && SceneLoader.Instance != null, "Bootstrap missing");
            FindObjectOfType<MainMenuUI>().OnPlayButtonClicked();
            yield return SceneReady(SceneNames.Act1);
            // Reuse the current office checks instead of asserting the retired black-screen opening.
            var officeHost = new GameObject("Office chapter checks");
            var officeRunner = officeHost.AddComponent<OfficeBlockoutGameplaySmokeRunner>();
            officeRunner.StopPlayOnCompletion = false;
            bool officeFinished = false;
            bool officePassed = false;
            string officeMessage = "";
            officeRunner.Completed += (passed, message) =>
            {
                officeFinished = true;
                officePassed = passed;
                officeMessage = message;
            };
            while (!officeFinished) yield return null;
            Check(officePassed, officeMessage);
            Destroy(officeHost);
            yield return SceneReady(SceneNames.Act2_Home);

        }
        else
        {
            SessionState.SetBool("KuTy.StorySmoke.ChapterOnly", false);
            yield return SceneReady(SceneNames.Act2_Home);
        }

        var chapterPipeline = QualitySettings.renderPipeline;
        var villagePipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
            "Assets/_Project/Art/Models/Scene3/VillageDetails/MemoryLandscape/OrganicVillage/Village_URP.asset");
        float villageRenderScale = villagePipeline != null ? villagePipeline.renderScale : 1f;
        var quests = QuestManager.Instance;
        var flow = HouseholdChoreFlow.Instance;
        var currency = CurrencyManager.Instance;
        Check(quests != null && flow != null && quests.CurrentStep == QuestStep.WashDishes, "Household chapter did not start");
        Check(!quests.CanShop && !quests.CanWashDishes && !quests.CanPlayMarbles, "Sequence gates missing");
        var wake = FindObjectOfType<BedWakeUpController>();
        if (wake != null) Call(wake, "GetOutOfBed");
        flow.ShowChecklist();
        var first = FindObjectsOfType<HouseholdChoreInteractable>().First(t => t.Chore == HouseholdChoreFlow.Order[0]);
        first.Interact();
        first.enabled = false;
        first.enabled = true;
        Check(flow.Pieces == 0 && currency.CurrentMoney == 0, "Cancelled chore granted a reward");
        Check(!flow.Complete(HouseholdChore.ReadLetter), "Out-of-order chore completed");
        foreach (var chore in HouseholdChoreFlow.Order)
        {
            Check(flow.Complete(chore) && !flow.Complete(chore), "Chore did not complete exactly once: " + chore);
        }
        Check(flow.Pieces == 140 && currency.CurrentMoney == 2000, "14 chores must award 140 pieces and 2000đ");
        Check(!quests.CompleteHouseholdChores() && currency.CurrentMoney == 2000, "Allowance duplicated");
        DialogueUI.Instance?.HideDialogue();
        quests.ContinueOutside();
        yield return SceneReady(SceneNames.Act3_OutSide);
        quests = QuestManager.Instance;
        currency = CurrencyManager.Instance;
        var memories = MemoryCollectionManager.Instance;
        var inventory = InventoryManager.Instance;
        var marbles = FindObjectOfType<MarbleShootingGame>();
        var journal = FindObjectOfType<JournalInteractable>();
        var shop = ShopManager.Instance;
        Check(villagePipeline != null && QualitySettings.renderPipeline != villagePipeline &&
            QualitySettings.renderPipeline != null, "Village must use a runtime pipeline instance");
        Check(Mathf.Approximately(villagePipeline.renderScale, villageRenderScale), "Village modified its shared pipeline asset");
        Check(quests.CurrentStep == QuestStep.BuyCandyAndMarbles && currency.CurrentMoney == 2000 &&
            memories.HasCollected(MemoryType.FilialLove), "Outdoor scene did not restore household reward");
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

        Check(marbles != null && Camera.main != null, "Outdoor marble game or camera missing");
        var originalCamera = Camera.main;
        var originalCameraParent = originalCamera.transform.parent;
        Vector3 originalCameraPosition = originalCamera.transform.localPosition;
        Quaternion originalCameraRotation = originalCamera.transform.localRotation;
        float originalFov = originalCamera.fieldOfView;
        float originalNear = originalCamera.nearClipPlane;
        marbles.StartGame();
        Check(marbles.State == MarbleShootingGame.PlayState.Aiming, "Marble game did not open for cancellation test");
        Call(marbles, "ClearAim");
        marbles.CancelGame();
        Check(marbles.State == MarbleShootingGame.PlayState.Inactive && !GameManager.Instance.InputBlocked,
            "Cancelling marble game leaked modal input");
        Check(originalCamera.transform.parent == originalCameraParent &&
            Vector3.Distance(originalCamera.transform.localPosition, originalCameraPosition) < .001f &&
            Quaternion.Angle(originalCamera.transform.localRotation, originalCameraRotation) < .01f &&
            Mathf.Approximately(originalCamera.fieldOfView, originalFov) && Mathf.Approximately(originalCamera.nearClipPlane, originalNear),
            "Marble cancellation failed to restore the player camera and lens");
        Check(quests.CurrentStep == QuestStep.PlayMarbles && currency.CurrentMoney == 0, "Marble cancellation advanced or paid");
        marbles.StartGame();
        Check(marbles.State == MarbleShootingGame.PlayState.Aiming, "Marble game did not start");
        var targets = Get<System.Collections.Generic.List<Rigidbody>>(marbles, "targetMarbles");
        var ring = Get<Collider>(marbles, "ringCollider");
        float outsideRadius = Mathf.Max(ring.bounds.extents.x, ring.bounds.extents.z) + 1f;
        for (int i = 0; i < 3; i++) targets[i].position = ring.bounds.center + Vector3.right * (outsideRadius + i);
        Call(marbles, "EvaluateShot");
        Check(marbles.IsCompleted && quests.CurrentStep == QuestStep.ReturnToJournal, "Marble win did not advance quest");
        Check(memories.HasCollected(MemoryType.Freedom) && memories.CollectedCount == 3, "Memory #3 missing");
        Check(SceneManager.GetActiveScene().name == SceneNames.Act3_OutSide, "Three memories skipped key crafting");
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
        yield return SceneReady(SceneNames.Act_Ending);
        Check(QualitySettings.renderPipeline == chapterPipeline, "Village pipeline leaked into the ending scene");
        Check(FindObjectOfType<EndingUI>() != null, "Ending scene missing");
        FindObjectOfType<EndingUI>().OnPlayAgainClicked();
        yield return SceneReady(SceneNames.MainMenu);
        FindObjectOfType<MainMenuUI>().OnPlayButtonClicked();
        yield return SceneReady(SceneNames.Act1);
        GameManager.Instance.EnterMemoryWorld();
        yield return SceneReady(SceneNames.Act2_Home);
        Check(CurrencyManager.Instance.CurrentMoney == 0 && InventoryManager.Instance.OwnedItems.Count == 0 &&
            MemoryCollectionManager.Instance.CollectedCount == 0 && QuestManager.Instance.CurrentStep == QuestStep.WashDishes,
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
}
