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
        Check(!quests.SkipScene3 && quests.HouseholdDestination == SceneNames.Act3_OutSide, "Full story remains default; merge does not silently skip Scene 3");
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
        foreach (var chore in HouseholdChoreFlow.Order.Concat(new[] { HouseholdChore.SortTools, HouseholdChore.GrindCorn }))
            Check(FindObjectsOfType<HouseholdChoreInteractable>(true).Any(t => t.Chore == chore && t.isActiveAndEnabled), "Chapter target remains bound: " + chore);
        foreach (var optional in new[] { HouseholdChore.SortTools, HouseholdChore.GrindCorn })
        {
            yield return ConfirmSimple(optional, flow);
            Check(flow.ActiveIndex == 0 && !flow.Complete(optional), "Optional task rewards once without advancing required order: " + optional);
        }
        Check(!flow.Complete(HouseholdChore.ReadLetter), "Out-of-order final letter remains rejected");
        foreach (var chore in HouseholdChoreFlow.Order.Take(HouseholdChoreFlow.ChoreCount - 1))
        {
            if (Scene2HouseholdActivityPolicy.IsSimple(chore)) yield return ConfirmSimple(chore, flow);
            else Check(flow.Complete(chore, false), "Physical task advances in handoff fixture (tested physically in separate suites): " + chore);
            Check(!flow.Complete(chore, false), "No duplicate task reward: " + chore);
            flow.Voice.Stop();
        }
        Check(flow.Pieces == 120 && Scene2Checkpoint.HomeChores == 11 && Scene2Checkpoint.HomeOptional == 3, "Checkpoint before letter retains 11 required tasks, two optional tasks, 120 pieces");
        var letter = FindObjectsOfType<HouseholdChoreInteractable>().Single(t => t.Chore == HouseholdChore.ReadLetter && t.isActiveAndEnabled);
        letter.Interact();
        Check(flow.UI.SimpleActionVisible && GameManager.Instance.InputBlocked, "Final letter opens deliberate reading UI, not retired puzzle");
        yield return null;
        flow.UI.ConfirmSimpleAction();
        Check(!flow.IsCompleted(HouseholdChore.ReadLetter), "Reading cannot be instantly confirmed");
        yield return new WaitForSeconds(4.2f);
        flow.UI.ConfirmSimpleAction();
        Check(flow.IsCompleted(HouseholdChore.ReadLetter), "Actual reading confirmation completes the final story task");
        Check(flow.ActiveIndex == 12 && flow.Pieces == 130 && quests.IsLeavingHome, "Final task completes 12 required tasks and 130 pieces");
        Check(CurrencyManager.Instance.CurrentMoney == 2000 && !quests.CompleteHouseholdChores(), "Allowance is exactly 2000 and cannot duplicate");
        Check(MemoryCollectionManager.Instance.HasCollected(MemoryType.FilialLove), "Household completion grants filial-love memory");
        Check(Scene2Checkpoint.HasOutsideProgress && Scene2Checkpoint.ResumeScene == SceneNames.Act3_OutSide, "Chapter completion saves the outdoor checkpoint");
        // Handoff fixture closes presentation instead of waiting out the entire narrated letter.
        DialogueUI.Instance?.HideDialogue();
        quests.ContinueOutside();
        Check(EndingSummary.HasData && EndingSummary.Pieces == 130 && EndingSummary.IsCompleted(HouseholdChore.ReadLetter), "Thuan ending summary captures local chapter results before unload");
        while (SceneManager.GetActiveScene().name != SceneNames.Act3_OutSide || SceneLoader.Instance && SceneLoader.Instance.IsLoading) yield return null;
        yield return null;
        Check(CurrencyManager.Instance.CurrentMoney == 2000 && QuestManager.Instance.CurrentStep == QuestStep.BuyCandyAndMarbles, "Outdoor scene restores allowance and expected quest");
        Check(EndingSummary.HasData && EndingSummary.Pieces == 130, "Personal ending summary survives Home scene destruction");
        Check(!GameManager.Instance.InputBlocked, "Chapter handoff does not leak modal input ownership");
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
