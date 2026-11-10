using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Post-merge chapter handoff checks. Physical activities have their own ray-driven suites.</summary>
[InitializeOnLoad]
public static class Scene2MergedJourneyChecks
{
    private const string Pending = "KuTy.MergedJourney.Pending";
    public const string Report = "Builds/Integration-20261109/merged-journey.txt";
    static Scene2MergedJourneyChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.EraseBool(Pending);
            EditorApplication.delayCall += () => new GameObject("Merged chapter handoff checks").AddComponent<Scene2MergedJourneyRunner>();
        };
    }
    [MenuItem("KuTy/Tests/Check merged Scene 2 chapter handoff")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != Scene2MotherHomeSetup.Home)
            throw new InvalidOperationException("Open saved Home in Edit Mode first.");
        KuTyCheckpointTestScope.Capture();
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class Scene2MergedJourneyRunner : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly StringBuilder report = new StringBuilder();
    private bool finished, background;
    private int assertions;
    private float started;
    private void Check(bool ok, string text)
    {
        if (!ok) throw new InvalidOperationException(text);
        assertions++; report.AppendLine("PASS " + text);
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        background = Application.runInBackground; Application.runInBackground = true;
        started = Time.realtimeSinceStartup; Application.logMessageReceived += Log;
        StartCoroutine(Guarded());
    }
    private void Update() { if (!finished && Time.realtimeSinceStartup - started > 100) Finish(false, "Timeout"); }
    private void Log(string message, string stack, LogType type)
    {
        if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish(false, message + "\n" + stack);
    }
    private IEnumerator Guarded()
    {
        var run = Run();
        while (!finished)
        {
            object next;
            try { if (!run.MoveNext()) { Finish(true, "Chapter handoff and incoming ending summary passed; physical activity suites run separately."); yield break; } next = run.Current; }
            catch (Exception error) { Finish(false, error.ToString()); yield break; }
            yield return next;
        }
    }
    private IEnumerator Run()
    {
        yield return null;
        EndingSummary.BeginSession();
        var flow = HouseholdChoreFlow.Instance;
        var quests = QuestManager.Instance;
        Check(flow && quests && !EndingSummary.HasData, "Home systems and incoming ending summary initialize cleanly");
        Check(quests.SkipScene3 && quests.HouseholdDestination == SceneNames.Act_Ending, "Scene 2 goes directly to Thuan ending cutscene; Scene 3 is not in the active route");
        var bed = FindObjectOfType<BedWakeUpController>();
        Check(GameManager.Instance.InputBlocked && flow.Pieces == 0, "Initial wake cinematic owns input without granting chore progress");
        yield return new WaitForSeconds(6.5f);
        Check(bed.CanPauseWhileWaiting && GameManager.Instance.InputBlocked, "Full wake cinematic reaches the get-out-of-bed prompt without unlocking movement early");
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", Private).Invoke(bed, null);
        yield return new WaitForSeconds(1.5f);
        FindObjectOfType<FirstPersonController>().enabled = false;
        flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue();
        Check(flow.ChecklistVisible && !GameManager.Instance.InputBlocked, "Wake-up starts the chapter and releases controls");
        var voices = Resources.Load<DialogueSequence>("HomeGameplayVoices");
        Check(voices && voices.Count == 15 && voices.Lines.All(l => l.voiceClip && !string.IsNullOrEmpty(l.text)), "All 15 home narration entries retain audio and subtitles");
        Check(Resources.Load<DialogueSequence>("EndingVoices"), "Incoming ending voice resource imports successfully");
        var video = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/_Project/Resources/Cutscenes/OfficeToHome_Final.mp4");
        Check(video && video.width > 0 && video.height > 0 && video.length > 0, "Incoming Office-to-Home video imports with valid dimensions and duration (not a playback certification)");
        var room = GameObject.Find("BoBanGhe_TiepKhach");
        Check(room && room.GetComponentsInChildren<MeshRenderer>().Length == 4, "All four committed living room models exist in runtime");
        foreach (var mesh in room.GetComponentsInChildren<MeshFilter>())
        {
            var solid = mesh.GetComponent<MeshCollider>();
            Check(solid && solid.enabled && !solid.isTrigger && solid.sharedMesh == mesh.sharedMesh, "New furniture blocks movement: " + mesh.name);
        }
        foreach (var chore in HouseholdChoreFlow.Order)
            Check(FindObjectsOfType<HouseholdChoreInteractable>(true).Any(t => t.Chore == chore && t.isActiveAndEnabled), "Chapter target remains bound: " + chore);
        Check(!flow.Complete(HouseholdChore.SortTools) && !flow.Complete(HouseholdChore.GrindCorn), "Removed optional tasks grant no progress or rewards");
        Check(!flow.Complete(HouseholdChore.ReadLetter), "Out-of-order final letter remains rejected");
        foreach (var chore in HouseholdChoreFlow.Order.Take(HouseholdChoreFlow.ChoreCount - 1))
        {
            if (Scene2HouseholdActivityPolicy.IsSimple(chore)) yield return ConfirmSimple(chore, flow);
            else Check(flow.Complete(chore, false), "Physical task advances in handoff fixture (tested physically in separate suites): " + chore);
            Check(!flow.Complete(chore, false), "No duplicate task reward: " + chore);
            flow.Voice.Stop();
        }
        Check(flow.Pieces == 40 && Scene2Checkpoint.HomeChores == 4 && Scene2Checkpoint.HomeOptional == 0, "Checkpoint before letter retains exactly four physical tasks and 40 pieces");
        var letter = FindObjectsOfType<HouseholdChoreInteractable>().Single(t => t.Chore == HouseholdChore.ReadLetter && t.isActiveAndEnabled);
        letter.Interact();
        Check(flow.UI.SimpleActionVisible && GameManager.Instance.InputBlocked, "Final letter opens deliberate reading UI, not retired puzzle");
        yield return null;
        flow.UI.ConfirmSimpleAction();
        Check(!flow.IsCompleted(HouseholdChore.ReadLetter), "Reading cannot be instantly confirmed");
        yield return new WaitForSeconds(4.2f);
        flow.UI.ConfirmSimpleAction();
        Check(flow.IsCompleted(HouseholdChore.ReadLetter), "Actual reading confirmation completes the final story task");
        Check(flow.ActiveIndex == 5 && flow.Pieces == 50 && quests.IsLeavingHome, "Final task completes five required tasks and 50 pieces");
        Check(CurrencyManager.Instance.CurrentMoney == 2000 && !quests.CompleteHouseholdChores(), "Allowance is exactly 2000 and cannot duplicate");
        Check(MemoryCollectionManager.Instance.HasCollected(MemoryType.FilialLove), "Household completion grants filial-love memory");
        Check(!Scene2Checkpoint.HasOutsideProgress && quests.CurrentStep == QuestStep.Ending, "Chapter completion marks ending, never creates an outdoor quest/checkpoint");
        // Handoff fixture closes presentation instead of waiting out the entire narrated letter.
        DialogueUI.Instance?.HideDialogue();
        quests.ContinueOutside();
        Check(EndingSummary.HasData && EndingSummary.Pieces == 50 && EndingSummary.IsCompleted(HouseholdChore.ReadLetter), "Ending summary captures the five-task chapter results before unload");
        while (SceneManager.GetActiveScene().name != SceneNames.Act_Ending || SceneLoader.Instance && SceneLoader.Instance.IsLoading) yield return null;
        yield return null;
        var director=FindObjectOfType<EndingDirector>();
        Check(director && director.isActiveAndEnabled && FindObjectOfType<EndingCameraRig>(), "Thuan authored ending director and cinematic camera initialize after actual Home unload");
        Check(EndingSummary.HasData && EndingSummary.Pieces == 50, "Personal ending summary survives Home scene destruction");
        Check(EndingMemoryFrames.EarnedSpriteNames().Count()==5, "Ending keepsakes contain exactly the five retained tasks, not removed chores");
        yield return new WaitForSeconds(30f);
        Check(director.FinalStarted, "Actual ending film plays through to the final card without loading Scene 3");
        var finalCard=FindObjectOfType<EndingUI>();float revealDeadline=Time.realtimeSinceStartup+15f;
        while(finalCard && !(bool)typeof(EndingUI).GetField("revealFinished",Private).GetValue(finalCard) && Time.realtimeSinceStartup<revealDeadline)yield return null;
        Check(finalCard && (bool)typeof(EndingUI).GetField("revealFinished",Private).GetValue(finalCard),"Ending final-card reveal finishes naturally, without an injected skip");
        var finalStats=(TMPro.TextMeshProUGUI)typeof(EndingUI).GetField("statsLabel",Private).GetValue(finalCard);
        Check(finalStats.text.Contains("50</color></b> / 50"),"Visible ending card finishes counting all 50 of 50 earned pieces");
        ScreenCapture.CaptureScreenshot(Scene2FiveTaskSetup.Output+"/ending-final-card.png");yield return new WaitForEndOfFrame();yield return null;
    }
    private IEnumerator ConfirmSimple(HouseholdChore chore, HouseholdChoreFlow flow)
    {
        var target = FindObjectsOfType<HouseholdChoreInteractable>().First(t => t.Chore == chore && t.isActiveAndEnabled && !string.IsNullOrEmpty(t.GetPromptText()));
        int before = flow.Pieces;
        target.Interact();
        Check(flow.UI.SimpleActionVisible, "Simple task routes through E interaction: " + chore);
        yield return null;
        flow.UI.ConfirmSimpleAction();
        Check(flow.IsCompleted(chore), "Actual UI confirmation completes task: " + chore);
        Check(flow.Pieces == before + (HouseholdChoreFlow.IsOptional(chore) ? 5 : 10), "Normal reward preserved: " + chore);
    }
    private void Finish(bool passed, string detail)
    {
        if (finished) return; finished = true;
        Application.logMessageReceived -= Log; Application.runInBackground = background;
        File.WriteAllText(Scene2MergedJourneyChecks.Report, (passed ? "PASS" : "FAIL") + " " + assertions + " assertions\n" + report + "\n" + detail);
        Debug.Log("[MergedJourney] " + (passed ? "PASS " : "FAIL ") + assertions + " " + detail);
        EditorApplication.isPlaying = false;
    }
}
