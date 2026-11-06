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
using TMPro;

/// <summary>Real home-scene regression checks, with automatic checkpoint and scene restoration.</summary>
[InitializeOnLoad]
public static class Scene2CompletionChecks
{
    private const string Pending = "KuTy.Scene2Completion.Pending";
    private const string PreviousScenes = "KuTy.Scene2Completion.Scenes";
    [Serializable] private sealed class SceneRecord { public string path; public bool loaded, active; }
    [Serializable] private sealed class SceneRecords { public SceneRecord[] scenes; }

    static Scene2CompletionChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                EditorApplication.delayCall += () => new GameObject("Scene 2 completion checks").AddComponent<Scene2CompletionCheckRunner>();
            }
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += RestoreScenes;
        };
    }

    [MenuItem("KuTy/Tests/Check Scene 2 complete household flow")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isDirty || (string.IsNullOrEmpty(scene.path) && scene.rootCount > 0))
                throw new InvalidOperationException("Save the open scenes before running Scene 2 checks; no unsaved work will be discarded.");
        }
        var previous = new SceneRecords { scenes = EditorSceneManager.GetSceneManagerSetup().Select(s =>
            new SceneRecord { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray() };
        KuTyCheckpointTestScope.Capture();
        SessionState.SetString(PreviousScenes, JsonUtility.ToJson(previous));
        try
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity");
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
        catch
        {
            SessionState.SetBool(Pending, false);
            KuTyCheckpointTestScope.Restore();
            RestoreScenes();
            throw;
        }
    }

    private static void RestoreScenes()
    {
        string json = SessionState.GetString(PreviousScenes, "");
        if (string.IsNullOrEmpty(json)) return;
        SessionState.EraseString(PreviousScenes);
        var previous = JsonUtility.FromJson<SceneRecords>(json);
        if (previous.scenes.Length == 0 || previous.scenes.All(s => string.IsNullOrEmpty(s.path)))
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        else EditorSceneManager.RestoreSceneManagerSetup(previous.scenes.Where(s => !string.IsNullOrEmpty(s.path)).Select(s =>
            new SceneSetup { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
    }
}

public sealed class Scene2CompletionCheckRunner : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private bool finished, background;
    private int assertions;
    private float started;
    private readonly System.Text.StringBuilder report = new System.Text.StringBuilder();
    private static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Private).Invoke(obj, args);
    private static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private).GetValue(obj);
    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        assertions++;
        report.AppendLine("PASS " + message);
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        background = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        Application.logMessageReceived += OnLog;
        StartCoroutine(GuardedRun());
    }
    private IEnumerator GuardedRun()
    {
        var run = Run();
        while (!finished)
        {
            object current;
            try
            {
                if (!run.MoveNext()) { Finish(true, "All checks passed. Mouse/visual playtesting is still required."); yield break; }
                current = run.Current;
            }
            catch (Exception error) { Finish(false, error.ToString()); yield break; }
            yield return current;
        }
    }
    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup - started > 90f) Finish(false, "Timed out.");
    }
    private IEnumerator Run()
    {
        yield return null;
        var flow = HouseholdChoreFlow.Instance;
        Check(flow != null && QuestManager.Instance != null, "Home story systems exist");
        var quests = QuestManager.Instance;
        Check(!quests.SkipScene3 && quests.HouseholdDestination == SceneNames.Act3_OutSide,
            "Full outdoor chapter remains the default route");
        var routeField = typeof(QuestManager).GetField("skipScene3", Private);
        routeField.SetValue(quests, true);
        try
        {
            Check(quests.HouseholdDestination == SceneNames.Act_Ending && quests.CanFinishHouseholdChores,
                "Optional skip route validates the ending scene");
        }
        finally { routeField.SetValue(quests, false); }
        var bed = FindObjectOfType<BedWakeUpController>();
        Check(bed != null, "Wake-up controller exists");
        Call(bed, "GetOutOfBed");
        yield return new WaitForSeconds(1.3f);
        Check(flow.ChecklistVisible && !GameManager.Instance.InputBlocked, "Getting out of bed starts chores and releases input");
        flow.Voice.Stop();
        Check(HouseholdChoreFlow.Order.Length == HouseholdChoreFlow.ChoreCount && HouseholdChoreFlow.Order.Distinct().Count() == 12,
            "Twelve unique required chores");
        Check(!flow.IsCurrent((HouseholdChore)(-1)) && !flow.IsCompleted((HouseholdChore)100), "Invalid task IDs are safely rejected");
        Check(!flow.Complete(HouseholdChore.CookRice), "Out-of-order completion is rejected");

        var hud = flow.GetComponent<Scene2QuestHUD>();
        Check(hud != null, "Shared household HUD exists");
        var targets = FindObjectsOfType<HouseholdChoreInteractable>(true).Where(t => t.enabled).ToArray();
        foreach (var chore in HouseholdChoreFlow.Order.Concat(new[] { HouseholdChore.SortTools, HouseholdChore.GrindCorn }))
            Check(targets.Any(t => t.Chore == chore), "Runtime target bound: " + chore);

        var first = targets.First(t => t.Chore == HouseholdChore.ArrangeKeepsakes && t.gameObject.activeInHierarchy);
        first.Interact();
        Check(GameManager.Instance.InputBlocked, "Mini-game acquires input");
        Check(Field<bool>(flow.UI.Actions, "active"), "Mini-game opens");
        Call(first, "RestartInteraction");
        Check(flow.Pieces == 0, "Restarting the current mini-game gives no reward");
        Call(first, "EndInteraction");
        Check(!GameManager.Instance.InputBlocked && flow.ActiveIndex == 0, "Cancelling preserves chapter progress and releases input");

        foreach (var optional in new[] { HouseholdChore.SortTools, HouseholdChore.GrindCorn })
        {
            Check(flow.Complete(optional), "Optional completion: " + optional);
            Check(!flow.Complete(optional), "Optional reward cannot duplicate: " + optional);
        }
        Check(flow.ActiveIndex == 0 && flow.Pieces == 10 && flow.OptionalPieces == 10, "Optional tasks do not advance required story");
        Check(Scene2Checkpoint.HomeOptional == 3 && Scene2Checkpoint.HasCheckpoint, "Optional-only progress creates a resume checkpoint");
        Check(Field<TextMeshProUGUI>(flow.UI, "rewardText").text == "+5 MẢNH KÝ ỨC", "Optional reward displays five pieces");
        string board = (string)Call(hud, "BuildBoardText");
        Check(board.Contains("Xếp dụng cụ") && board.Contains("Xay ngô") && !board.Contains("Làm lại từ đầu"), "Board shows optional tasks without destructive reset shortcut");

        Check(flow.Complete(HouseholdChore.ArrangeKeepsakes), "Chest advances to radio");
        flow.Voice.Stop();
        var radio = flow.GetComponent<Scene2RadioKitchenSequence>();
        Check(radio && radio.Handles(HouseholdChore.TuneRadio) && radio.Handles(HouseholdChore.CarryWood), "Physical radio and wood bindings are ready");
        var camera = Camera.main;
        var player = FindObjectOfType<FirstPersonController>();
        player.enabled = false; // The suite drives poses explicitly, never changes authored scene transforms.
        Vector3 home = camera.transform.localPosition;
        Quaternion rotation = camera.transform.localRotation;
        float fov = camera.fieldOfView;
        radio.InteractWith(HouseholdChore.TuneRadio);
        Check(radio.IsTuning && GameManager.Instance.InputBlocked, "Physical radio acquires input");
        GameManager.Instance.SetPaused(true);
        yield return new WaitForSecondsRealtime(.15f);
        GameManager.Instance.SetPaused(false);
        radio.EndTuning();
        Check(!GameManager.Instance.InputBlocked && Vector3.Distance(home, camera.transform.localPosition) < .001f &&
            Quaternion.Angle(rotation, camera.transform.localRotation) < .01f && Mathf.Abs(fov - camera.fieldOfView) < .001f,
            "Radio cancel after pause restores camera and input");

        for (int i = 1; i < HouseholdChoreFlow.Order.Length - 1; i++)
        {
            var chore = HouseholdChoreFlow.Order[i];
            Check(flow.CurrentChore == chore && !string.IsNullOrEmpty(flow.CurrentGuidance), "Guidance exists for " + chore);
            var physicalKitchen = flow.GetComponent<Scene2KitchenHandsOn>();
            if (chore == HouseholdChore.TuneRadio)
            {
                radio.InteractWith(chore);
                Call(radio, "AdvanceSignalLock", 2f);
                Check(!flow.IsCompleted(chore), "Noise cannot complete radio tuning");
                Call(radio, "AdjustFrequency", .56f);
                Call(radio, "AdvanceSignalLock", 1.4f);
                Check(radio.RadioOn && !radio.IsTuning && !GameManager.Instance.InputBlocked, "Stable radio signal completes and releases input");
            }
            else if (chore == HouseholdChore.CarryWood)
            {
                radio.InteractWith(chore);
                Check(radio.CarryingWood && !flow.IsCompleted(chore), "Picking up wood does not prematurely award delivery");
                radio.ReturnWood();
                Check(!radio.CarryingWood && !flow.IsCompleted(chore), "Returning wood leaves the delivery retryable");
                radio.InteractWith(chore);
                Check(ApproachWoodDestination(player, camera, radio), "Wood destination has a clear reachable approach");
                radio.PlaceWood();
                Check(!radio.CarryingWood && radio.woodBundle.parent == radio.woodRest, "Physical wood delivery places the bundle");
            }
            else if (physicalKitchen.Handles(chore))
            {
                home = camera.transform.localPosition;
                rotation = camera.transform.localRotation;
                fov = camera.fieldOfView;
                physicalKitchen.Begin(chore);
                Check(physicalKitchen.IsWorking && GameManager.Instance.InputBlocked, "Physical kitchen acquires input: " + chore);
                yield return new WaitForSeconds(.5f);
                physicalKitchen.End();
                Check(!GameManager.Instance.InputBlocked && Vector3.Distance(home, camera.transform.localPosition) < .001f &&
                    Quaternion.Angle(rotation, camera.transform.localRotation) < .01f && Mathf.Abs(fov - camera.fieldOfView) < .001f,
                    "Kitchen cancellation restores camera: " + chore);
                physicalKitchen.Begin(chore);
                if (chore == HouseholdChore.WashRice)
                {
                    Call(physicalKitchen, "Drain", 1f);
                    Check(physicalKitchen.RinseRound == 0, "Cannot drain unwashed rice to finish");
                    for (int round = 0; round < 2; round++)
                    {
                        for (int step = 0; step < 60 && !physicalKitchen.NeedsDrain; step++)
                        {
                            float angle = step * 10f * Mathf.Deg2Rad;
                            Call(physicalKitchen, "WashAt", physicalKitchen.water.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * .09f);
                        }
                        Check(physicalKitchen.NeedsDrain, "Circular washing enables draining: round " + round);
                        Call(physicalKitchen, "Drain", 1f);
                    }
                }
                else if (chore == HouseholdChore.LightFire)
                {
                    Call(physicalKitchen, "Blow", .5f);
                    Check(!physicalKitchen.FireLit, "Cannot light the fire before placing wood");
                    for (int log = 0; log < 3; log++) Call(physicalKitchen, "PlaceKindling", log);
                    Call(physicalKitchen, "PlaceKindling", 0);
                    Check(physicalKitchen.KindlingCount == 3, "Placed wood cannot duplicate");
                    for (int breath = 0; breath < 3; breath++) Call(physicalKitchen, "Blow", .5f);
                }
                else
                {
                    Call(physicalKitchen, "LiftLid", 1f);
                    Check(!flow.IsCompleted(chore), "Checking the lid early cannot finish cooking");
                    Call(physicalKitchen, "AdjustFuel", -.38f);
                    for (int tick = 0; tick < 130; tick++) Call(physicalKitchen, "Simmer", .25f);
                    Check(physicalKitchen.Cooked >= 12f, "Gentle heat cooks rice");
                    GameManager.Instance.SetPaused(true);
                    float cooked = physicalKitchen.Cooked;
                    yield return new WaitForSecondsRealtime(.2f);
                    Check(physicalKitchen.Cooked == cooked, "Pause freezes cooking");
                    GameManager.Instance.SetPaused(false);
                    Call(physicalKitchen, "LiftLid", 1f);
                }
                Check(!physicalKitchen.IsWorking && !GameManager.Instance.InputBlocked, "Finishing physical kitchen releases input: " + chore);
            }
            else Check(flow.Complete(chore, false), "Required completion: " + chore);
            Check(flow.IsCompleted(chore), "Required task completed: " + chore);
            Check(!flow.Complete(chore, false), "Required reward cannot duplicate: " + chore);
            Check(Scene2Checkpoint.HomeChores == i + 1 && Scene2Checkpoint.HomeOptional == 3, "Checkpoint preserves required and optional progress after " + chore);
            flow.Voice.Stop();
            yield return null;
        }
        Check(flow.RequiredPieces == 110 && flow.OptionalPieces == 10 && flow.Pieces == 120, "Required and optional totals remain separate");
        Call(radio, "RestoreWorld");
        Check(radio.RadioOn && radio.woodBundle.parent == radio.woodRest, "Checkpoint reconstructs radio and delivered wood");
        var kitchen = flow.GetComponent<Scene2KitchenHandsOn>();
        Call(kitchen, "RestoreWorld");
        Check(kitchen.FireLit && !kitchen.washRoot.gameObject.activeSelf, "Checkpoint reconstructs completed kitchen");
        var station = Field<AudioSource>(radio, "stationAudio");
        Check(station.isPlaying, "Restored radio is playing");
        GameManager.Instance.SetPaused(true);
        float audioTime = station.time;
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(station.time - audioTime) < .05f, "Radio audio does not advance while paused");
        GameManager.Instance.SetPaused(false);
        Check(station.isPlaying, "Radio resumes after pause");

        flow.UI.ShowGame("Mẹ", flow.CurrentGuidance, false);
        flow.UI.ShowChoices(new[] { "Con làm gì tiếp?", "Nói chuyện với mẹ", "Con làm tiếp nhé" }, _ => { });
        Check(Field<TextMeshProUGUI>(flow.UI, "conversationText").gameObject.activeSelf, "Mother conversation has a dedicated readable body");
        Check(!Field<Button>(flow.UI, "restartButton").gameObject.activeSelf, "Conversation hides irrelevant restart button");
        flow.UI.HideGame();
        flow.UI.ShowGame("Thử lại", "Thao tác bằng chuột", false, () => { }, () => { });
        Check(Field<Button>(flow.UI, "restartButton").gameObject.activeSelf &&
            Field<TextMeshProUGUI>(flow.UI, "hintText").gameObject.activeSelf, "Mini-game presentation restores after conversation");
        flow.UI.HideGame();

        Check(flow.CurrentChore == HouseholdChore.ReadLetter && QuestManager.Instance.CanFinishHouseholdChores, "Letter is the final required step and destination is available");
        Check(flow.Complete(HouseholdChore.ReadLetter, false), "Finishing the required story succeeds");
        Check(flow.ActiveIndex == 12 && flow.Pieces == 130 && QuestManager.Instance.IsLeavingHome, "Full story completes with optional bonus and arms outside transition");
        Check(!flow.Complete(HouseholdChore.ReadLetter, false), "Letter cannot grant twice");
        Check(Scene2Checkpoint.HasOutsideProgress, "Final completion saves destination checkpoint");

        // A second visit verifies the essential route does not depend on bonus chores.
        Scene2Checkpoint.Clear();
        yield return SceneManager.LoadSceneAsync(SceneNames.Act2_Home);
        yield return null;
        flow = HouseholdChoreFlow.Instance;
        Call(FindObjectOfType<BedWakeUpController>(), "GetOutOfBed");
        yield return new WaitForSeconds(1.3f);
        Check(flow.ActiveIndex == 0 && flow.OptionalPieces == 0, "Fresh visit starts without optional progress");
        foreach (var chore in HouseholdChoreFlow.Order)
        {
            Check(flow.Complete(chore, false), "Required-only route: " + chore);
            flow.Voice.Stop();
        }
        Check(flow.Pieces == 120 && flow.OptionalPieces == 0 && QuestManager.Instance.IsLeavingHome,
            "Required-only route finishes with 120 pieces, without forcing optional chores");
    }
    private bool ApproachWoodDestination(FirstPersonController player, Camera camera, Scene2RadioKitchenSequence radio)
    {
        float floor = player.transform.position.y;
        var body = player.GetComponent<CharacterController>();
        foreach (float radius in new[] { .8f, 1.2f, 1.55f })
        for (int direction = 0; direction < 16; direction++)
        {
            Vector3 feet = radio.woodRest.position + Quaternion.Euler(0, direction * 22.5f, 0) * Vector3.forward * radius;
            feet.y = floor;
            player.TeleportTo(feet, Quaternion.identity);
            camera.transform.LookAt(radio.woodRest.position);
            Physics.SyncTransforms();
            Vector3 center = body.transform.TransformPoint(body.center);
            float half = Mathf.Max(0, body.height * .5f - body.radius);
            bool blocked = Physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half,
                body.radius * .95f, ~0, QueryTriggerInteraction.Ignore).Any(c => !c.transform.IsChildOf(player.transform));
            if (!blocked && radio.CanPlaceWood()) return true;
        }
        return false;
    }
    private void OnLog(string text, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, text + "\n" + stack);
    }
    private void Finish(bool success, string message)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnLog;
        File.WriteAllText("Library/Scene2CompletionChecks.txt", (success ? "PASS" : "FAIL") + " · " + assertions + " assertions\n" + report + message);
        Debug.Log("[Scene2Completion] " + (success ? "PASS" : "FAIL") + " · " + assertions + " assertions · " + message);
        GameManager.Instance?.SetPaused(false);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
    }
    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        Application.runInBackground = background;
    }
}
