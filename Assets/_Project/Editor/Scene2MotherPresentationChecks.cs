using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class Scene2MotherPresentationChecks
{
    internal const string Prefab = "Assets/_Project/Characters/MomUserDemo/MomUser.prefab";
    private const string Pending = "KuTy.MotherPresentation.Pending";
    private const string PreviousStart = "KuTy.MotherPresentation.PreviousStart";

    static Scene2MotherPresentationChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                new GameObject("Mother presentation checks").AddComponent<Scene2MotherPresentationCheckRunner>();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending, false))
            {
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(PreviousStart, ""));
                SessionState.EraseBool(Pending);
                SessionState.EraseString(PreviousStart);
            }
        };
    }

    [MenuItem("KuTy/Tests/Check MomUser standing interactions in isolation")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        var animator = prefab ? prefab.GetComponentInChildren<Animator>() : null;
        if (!animator || !animator.avatar || !animator.avatar.isValid || animator.isHuman)
            throw new InvalidOperationException("Verified MomUser Generic prefab required.");
        var controller = animator.runtimeAnimatorController as AnimatorController;
        string[] expected = { "Mother_Idle_Standing", "Mother_CallChild", "Mother_TalkIdle", "Mother_NodApprove" };
        if (!controller || expected.Any(n => !controller.layers[0].stateMachine.states.Any(s => s.state.name == n)))
            throw new InvalidOperationException("MomUser must contain all four standing states.");
        const string path = "Assets/_Project/Generated/MotherPresentationSmoke.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try { EditorSceneManager.SaveScene(scene, path); }
        finally { EditorSceneManager.CloseScene(scene, true); }
        SessionState.SetString(PreviousStart, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Pending, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class Scene2MotherPresentationCheckRunner : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly StringBuilder report = new StringBuilder();
    private bool finished, previousBackground;
    private float started;

    private void Start()
    {
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        Application.logMessageReceived += OnLog;
        StartCoroutine(Run());
    }
    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup - started > 50f) Finish(false, "Timeout");
    }
    private static void Set(object target, string name, object value)
        => target.GetType().GetField(name, Private).SetValue(target, value);
    private static T Get<T>(object target, string name)
        => (T)target.GetType().GetField(name, Private).GetValue(target);
    private void Check(bool value, string message)
    {
        if (!value) { Finish(false, message); throw new InvalidOperationException(message); }
        report.AppendLine("PASS " + message);
    }
    private static string CheckpointSnapshot()
        => string.Join("|", new[] { "Version", "Stage", "Chores", "Optional", "Step", "Money", "Candy", "Marbles", "Key", "Memories" }
            .Select(n => PlayerPrefs.HasKey("KuTy.Scene2Checkpoint." + n) + ":" + PlayerPrefs.GetInt("KuTy.Scene2Checkpoint." + n, -999)));

    private IEnumerator Run()
    {
        yield return null;
        string checkpoint = CheckpointSnapshot();
        var game = GameManager.Instance;
        Check(game != null && HouseholdChoreFlow.Instance == null, "Isolated scene has no initialized household flow");
        var actor = new GameObject("MomUser interaction fixture");
        actor.SetActive(false);
        var model = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Scene2MotherPresentationChecks.Prefab), actor.transform);
        var animator = model.GetComponentInChildren<Animator>();
        Vector3 scale = model.transform.localScale;
        var collider = actor.AddComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.height = 1.65f;
        collider.center = Vector3.up * .825f;
        var presentation = actor.AddComponent<Scene2MotherDemo>();

        // Inactive flow is an event/data fixture only. Awake/Complete never run; no saves or rewards.
        var flowHost = new GameObject("Inactive household event fixture");
        flowHost.SetActive(false);
        var flow = flowHost.AddComponent<HouseholdChoreFlow>();
        var radio = flowHost.AddComponent<Scene2RadioKitchenSequence>();
        var companionHost = new GameObject("Conversation fixture");
        companionHost.SetActive(false);
        var companion = companionHost.AddComponent<Scene2MotherCompanion>();
        Set(companion, "showPlaceholder", false);
        Set(companion, "actor", actor.transform);
        Set(companion, "animator", animator);
        var uiHost = new GameObject("Conversation UI fixture");
        Set(flow, "ui", new HouseholdChoreUI(uiHost.transform));
        Set(flow, "checklistVisible", true);
        presentation.Configure(animator, companion, radio);
        companionHost.SetActive(true);
        actor.SetActive(true);
        var camera = new GameObject("Mother fixture camera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 1.2f, 2f);
        camera.transform.LookAt(collider.bounds.center);
        yield return null;
        Set(companion, "flow", flow);
        yield return new WaitForSeconds(.3f);
        Check(animator.avatar.isValid && !animator.isHuman && !animator.applyRootMotion, "Verified Generic rig, no root motion");
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Mother_Idle_Standing"), "Standing mother begins in Idle");
        Check(Get<Action>(radio, "OnMotherCalled")?.GetInvocationList().Length == 1, "Call event subscribed exactly once");
        Check(Get<Action<HouseholdChore, int>>(flow, "OnChoreCompleted")?.GetInvocationList().Length == 1, "Chore event subscribed exactly once");

        Check(companion.CanTalk, "Existing E-conversation preconditions accept the authored actor");
        companion.BeginConversation();
        presentation.Greet();
        yield return new WaitForSeconds(.3f);
        Check(companion.IsConversing && companion.IsPresentingReply && game.InputBlocked, "Conversation presents a reply and owns input");
        Check(presentation.IsTalking && !presentation.IsCalling && animator.GetCurrentAnimatorStateInfo(0).IsName("Mother_TalkIdle"), "Conversation uses TalkIdle, never the call gesture");
        yield return new WaitForSeconds(2.7f);
        Check(companion.IsConversing && !companion.IsPresentingReply && !presentation.IsTalking, "Mother stops talking while player reads choices");
        typeof(Scene2MotherCompanion).GetMethod("Select", Private).Invoke(companion, new object[] { 1 });
        yield return new WaitForSeconds(.3f);
        Check(presentation.IsTalking, "Choosing another reply resumes TalkIdle");
        float beforePause = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        float speed = animator.speed;
        game.SetPaused(true);
        yield return new WaitForSecondsRealtime(.4f);
        Check(animator.speed == 0f && Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime - beforePause) < .02f, "Pause freezes the mother's animation");
        game.SetPaused(false);
        Check(Mathf.Approximately(animator.speed, speed), "Resume restores the original Animator speed");
        companion.EndConversation();
        yield return new WaitForSeconds(.3f);
        Check(!presentation.IsTalking && !game.InputBlocked && animator.GetCurrentAnimatorStateInfo(0).IsName("Mother_Idle_Standing"), "Closing conversation restores Idle and input");

        var hand = model.GetComponentsInChildren<Transform>().Single(t => t.name == "Hand.R");
        Vector3 handRest = hand.position;
        Get<Action>(radio, "OnMotherCalled")();
        yield return new WaitForSeconds(.7f);
        Check(presentation.IsCalling && animator.GetCurrentAnimatorStateInfo(0).IsName("Mother_CallChild"), "Authored radio call event plays CallChild");
        Check(Vector3.Distance(handRest, hand.position) > .02f, "CallChild moves the imported MomUser hand");
        float callTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        Get<Action>(radio, "OnMotherCalled")();
        yield return new WaitForSeconds(.2f);
        Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > callTime, "Repeated call does not restart the gesture");
        yield return new WaitForSeconds(2.8f);
        Check(!presentation.IsCalling && animator.GetCurrentAnimatorStateInfo(0).IsName("Mother_Idle_Standing"), "CallChild finishes once and returns to Idle");

        Physics.SyncTransforms();
        Get<Action<HouseholdChore, int>>(flow, "OnChoreCompleted")(HouseholdChore.CarryWood, 30);
        yield return new WaitForSeconds(.35f);
        Check(presentation.IsNodding && animator.GetCurrentAnimatorStateInfo(0).IsName("Mother_NodApprove"), "Nearby chore-completed event plays Nod");
        float nodTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        presentation.RequestApproval();
        yield return new WaitForSeconds(.2f);
        Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > nodTime, "Repeated approval does not restart Nod");
        yield return new WaitForSeconds(1.7f);
        Check(!presentation.IsNodding && animator.GetCurrentAnimatorStateInfo(0).IsName("Mother_Idle_Standing"), "Nod returns to Idle");
        Set(presentation, "nextApproval", 0f);
        Get<Action<HouseholdChore, int>>(flow, "OnChoreCompleted")(HouseholdChore.ReadLetter, 120);
        yield return null;
        Check(!presentation.IsNodding && !Get<bool>(presentation, "approvalPending"), "Letter does not trigger a distracting approval gesture");
        camera.transform.position = Vector3.forward * 20f;
        Get<Action<HouseholdChore, int>>(flow, "OnChoreCompleted")(HouseholdChore.FetchWater, 40);
        yield return null;
        Check(!Get<bool>(presentation, "approvalPending"), "Distant chore completion does not queue a stale Nod");
        camera.transform.position = new Vector3(0, 1.2f, 2f);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0, 1f, 1f);
        wall.transform.localScale = new Vector3(3, 3, .1f);
        Physics.SyncTransforms();
        Get<Action<HouseholdChore, int>>(flow, "OnChoreCompleted")(HouseholdChore.FetchWater, 40);
        Check(!Get<bool>(presentation, "approvalPending"), "Occluded mother does not react through a wall");
        Destroy(wall);
        presentation.enabled = false;
        Check(Get<Action>(radio, "OnMotherCalled") == null && Get<Action<HouseholdChore, int>>(flow, "OnChoreCompleted") == null, "Disable unsubscribes call and chore events");
        presentation.enabled = true;
        Check(Get<Action>(radio, "OnMotherCalled")?.GetInvocationList().Length == 1 && Get<Action<HouseholdChore, int>>(flow, "OnChoreCompleted")?.GetInvocationList().Length == 1, "Re-enable binds each event once");
        Check(actor.transform.position == Vector3.zero && model.transform.localScale == scale, "Presentation never moves or rescales the mother");
        Check(flow.ActiveIndex == 0 && flow.Pieces == 0 && HouseholdChoreFlow.Instance == null && CheckpointSnapshot() == checkpoint, "Animation/events never complete chores, grant pieces or write checkpoints");
        Finish(true, "MomUser standing interaction checks passed in isolation; no home scene edits or saves.");
    }

    private void OnLog(string message, string stack, LogType type)
    {
        if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish(false, message + "\n" + stack);
    }
    private void Finish(bool success, string message)
    {
        if (finished) return;
        finished = true;
        Directory.CreateDirectory("Tools/MotherDelivery/Source");
        File.WriteAllText("Tools/MotherDelivery/Source/mother-presentation-checks.txt", (success ? "PASS\n" : "FAIL\n") + report + message);
        GameManager.Instance?.SetPaused(false);
        EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
    }
    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        Application.runInBackground = previousBackground;
    }
}
