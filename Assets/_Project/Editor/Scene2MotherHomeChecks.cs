using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Scene2MotherHomeChecks
{
    private const string Pending = "KuTy.MotherHomeChecks.Pending";
    static Scene2MotherHomeChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.EraseBool(Pending);
                new GameObject("Mother home regression checks").AddComponent<Scene2MotherHomeCheckRunner>();
            }
        };
    }
    [MenuItem("KuTy/Tests/Check mother walking and conversations in home")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != Scene2MotherHomeSetup.Home || scene.isDirty)
            throw new InvalidOperationException("Open saved Home in Edit Mode first.");
        if (!UnityEngine.Object.FindObjectOfType<Scene2MotherHomeMovement>()) throw new InvalidOperationException("Bind reviewed mother home movement first.");
        var mother = UnityEngine.Object.FindObjectOfType<Scene2MotherDemo>();
        SessionState.SetString("KuTy.MotherHomeChecks.AuthoredVisual", JsonUtility.ToJson(mother.Animator.transform.position));
        KuTyCheckpointTestScope.Capture();
        SessionState.SetBool(Pending, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.EnterPlaymode();
    }
}

public sealed class Scene2MotherHomeCheckRunner : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly StringBuilder report = new StringBuilder();
    private Scene2MotherHomeMovement movement;
    private Scene2MotherCompanion companion;
    private Scene2MotherDemo presentation;
    private HouseholdChoreFlow flow;
    private Camera view;
    private bool finished, background;
    private float started;
    private int assertions;
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        assertions++; report.AppendLine("PASS " + message);
    }
    private void Start()
    {
        background = Application.runInBackground; Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        Application.logMessageReceived += Log;
        StartCoroutine(Guarded());
    }
    private void Update() { if (!finished && Time.realtimeSinceStartup - started > 210f) Finish(false, "Timeout"); }
    private void Log(string text, string stack, LogType type)
    {
        if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish(false, text + "\n" + stack);
    }
    private IEnumerator Guarded()
    {
        var run = Run();
        while (!finished)
        {
            object next;
            try { if (!run.MoveNext()) { Finish(true, "Reviewed walk, safe room routes and conversations passed without chore rewards."); yield break; } next = run.Current; }
            catch (Exception e) { Finish(false, e.ToString()); yield break; }
            yield return next;
        }
    }
    private IEnumerator Run()
    {
        yield return null;
        movement = FindObjectOfType<Scene2MotherHomeMovement>();
        presentation = movement.GetComponent<Scene2MotherDemo>();
        flow = HouseholdChoreFlow.Instance; companion = flow.GetComponent<Scene2MotherCompanion>(); view = Camera.main;
        Check(movement.NavigationReady, "Private indoor navigation samples the authored mother's standing point");
        report.AppendLine($"INFO home={movement.Home}, places={movement.ReachablePlaces}, canOccupyHome={movement.CanOccupy(movement.Home)}, speed={movement.WalkSpeed}");
        Check(movement.ReachablePlaces >= 2, "At least two safe destinations are reachable without moving furniture");
        Check(Vector3.ProjectOnPlane(presentation.Animator.transform.position - movement.transform.position, Vector3.up).magnitude < .01f,
            "Visible authored mother, interaction root, body and navigation align in runtime");
        Vector3 authoredVisual = JsonUtility.FromJson<Vector3>(SessionState.GetString("KuTy.MotherHomeChecks.AuthoredVisual", "{}"));
        SessionState.EraseString("KuTy.MotherHomeChecks.AuthoredVisual");
        Check(Vector3.ProjectOnPlane(presentation.Animator.transform.position - authoredVisual, Vector3.up).magnitude < .001f,
            "Runtime rebasing preserves the visible world placement chosen by the user");
        Check(presentation.Animator.avatar.isValid && !presentation.Animator.isHuman && !presentation.Animator.applyRootMotion,
            "Existing Generic model remains bound; no root-motion teleporting or humanoid assumption");
        Check(presentation.Animator.HasState(0, Animator.StringToHash("Base Layer.Mother_Walk")), "Separate controller contains the reviewed walk state");
        ValidateWalk();
        var bed = FindObjectOfType<BedWakeUpController>();
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", Private).Invoke(bed, null);
        yield return new WaitForSeconds(1.6f);
        flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue();
        var player = FindObjectOfType<FirstPersonController>(); player.enabled = false;
        Set(movement, "nextTrip", Time.time + 1000f);
        Check(flow.ChecklistVisible && !GameManager.Instance.InputBlocked, "Waking enables routine without stealing player input");
        Vector3 scale = movement.transform.localScale;
        int pieces = flow.Pieces, index = flow.ActiveIndex;
        var goals = Field<List<Vector3>>(movement, "destinations").OrderBy(p => Vector3.Distance(p, movement.Home)).ToArray();
        report.AppendLine("INFO destinations=" + string.Join("; ", goals.Select(g => g.ToString("F2"))));
        Vector3 first = goals.First(g => Vector3.Distance(g, movement.transform.position) > 1f);
        Check(movement.TryVisit(first), "First room route is complete, not a straight-line fallback");
        float until = Time.time + 9f;
        while (!movement.IsMoving && Time.time < until) yield return null;
        Check(movement.IsMoving, "Mother actually advances along the route");
        yield return new WaitForSeconds(.25f);
        Check(presentation.IsWalking, "Actual movement selects Walk instead of sliding in Idle");
        Vector3 pausePosition = movement.transform.position; Quaternion pauseRotation = movement.transform.rotation;
        GameManager.Instance.SetPaused(true);
        yield return new WaitForSecondsRealtime(.35f);
        Check(movement.transform.position == pausePosition && movement.transform.rotation == pauseRotation && presentation.Animator.speed == 0f,
            "Pause freezes both navigation and animation");
        GameManager.Instance.SetPaused(false);
        var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name = "Temporary dynamic mother route blocker";
        blocker.transform.position = movement.transform.position + movement.transform.forward * 1.08f + Vector3.up * 1.4f;
        blocker.transform.rotation = movement.transform.rotation;
        blocker.transform.localScale = new Vector3(2, 2.8f, .6f);
        Physics.SyncTransforms();
        yield return new WaitForSeconds(1.6f);
        Check(!movement.IsMoving && movement.CanOccupy(movement.transform.position), "Live capsule checks stop for a newly placed obstacle without intersecting it");
        Destroy(blocker); yield return null; Physics.SyncTransforms();
        Check(movement.TryVisit(first), "A cleared route can resume without a warp");
        float routeUntil = Time.time + 65f;
        bool clear = true, bounded = true; Vector3 previous = movement.transform.position;
        while (Vector3.Distance(movement.transform.position, first) > .12f && Time.time < routeUntil)
        {
            yield return null;
            clear &= movement.CanOccupy(movement.transform.position);
            bounded &= Vector3.Distance(previous, movement.transform.position) <= movement.WalkSpeed * Mathf.Min(Time.deltaTime, .05f) + .003f;
            previous = movement.transform.position;
        }
        Check(Vector3.Distance(movement.transform.position, first) < .12f, "Mother reaches a safe household destination");
        Check(clear && bounded, "Every sampled route frame stays collision-free and speed-bounded");
        Capture("Mother-AtHouseholdStop");
        var chorePlaces = Field<Dictionary<HouseholdChore, Vector3>>(movement, "chorePlaces");
        Vector3 second = chorePlaces.TryGetValue(HouseholdChore.SetTable, out var dining) ? dining :
            goals.Last(g => Vector3.Distance(g, movement.Home) > 4f && Vector3.Distance(g, movement.Home) < 8f);
        report.AppendLine($"INFO longer route goal={second}, diningAreaReachable={chorePlaces.ContainsKey(HouseholdChore.SetTable)}");
        Check(movement.TryVisit(second), "A longer household route passes both NavMesh and full-segment live collision checks");
        routeUntil = Time.time + 75f;
        bool capturedWalking = false;
        while (Vector3.Distance(movement.transform.position, second) > .12f && Time.time < routeUntil)
        {
            yield return null;
            if (!capturedWalking && movement.IsMoving && Vector3.Distance(movement.transform.position, first) > 1f)
            { Capture("Mother-Walking"); capturedWalking = true; }
        }
        Check(Vector3.Distance(movement.transform.position, second) < .12f, "Mother completes the longer household trip, not only a nearby shuffle");
        var kitchen = flow.GetComponent<Scene2KitchenHandsOn>();
        var voice = Field<AudioSource>(kitchen, "motherVoice");
        Check(Vector3.Distance(voice.transform.position, kitchen.motherVoiceAnchor.position) < .02f, "Kitchen speech follows the moving mother, not her old position");
        Check(FocusMother(player), "Real player ray can focus the moving mother with E");
        Vector3 beforeCollision = player.transform.position;
        var controller = player.GetComponent<CharacterController>();
        Vector3 towardMother = Vector3.ProjectOnPlane(movement.transform.position - beforeCollision, Vector3.up).normalized;
        controller.Move(towardMother * 3f);
        Check(Vector3.Dot(Vector3.ProjectOnPlane(player.transform.position - movement.transform.position, Vector3.up), -towardMother) > .5f,
            "The child's real CharacterController cannot walk through mother's runtime body");
        Check(FocusMother(player), "Solid body retains the normal E interaction target");
        player.GetComponent<PlayerInteraction>().CurrentTarget.Interact();
        Check(companion.IsConversing && GameManager.Instance.InputBlocked, "Real E interaction opens the existing dialogue choices");
        Vector3 conversationPosition = movement.transform.position;
        yield return new WaitForSeconds(.4f);
        Check(!movement.IsMoving && movement.transform.position == conversationPosition && presentation.IsTalking,
            "Conversation stops walking and plays TalkIdle");
        typeof(Scene2MotherCompanion).GetMethod("Select", Private).Invoke(companion, new object[] { 1 });
        yield return new WaitForSeconds(1.7f);
        Vector3 toPlayer = Vector3.ProjectOnPlane(view.transform.position - movement.transform.position, Vector3.up).normalized;
        Check(Vector3.Dot(movement.transform.forward, toPlayer) > .9f, "Mother turns to the child while talking instead of speaking with her back turned");
        Capture("Mother-Conversation");
        view.transform.LookAt(movement.GetComponent<Collider>().bounds.center);
        ScreenCapture.CaptureScreenshot("Tools/MotherHome/Mother-Conversation-UI.png");
        yield return new WaitForEndOfFrame();
        companion.EndConversation();
        Check(!companion.IsConversing && !GameManager.Instance.InputBlocked, "Closing a conversation releases input");
        Check(Scene2MotherCompanion.SmallTalk(HouseholdChore.TuneRadio, 0) != Scene2MotherCompanion.SmallTalk(HouseholdChore.CookRice, 0),
            "Small talk changes with household progress");
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Temporary conversation occlusion check";
        wall.transform.position = Vector3.Lerp(view.transform.position, movement.GetComponent<Collider>().bounds.center, .5f);
        wall.transform.localScale = new Vector3(.6f, 3, .6f);
        Physics.SyncTransforms();
        yield return new WaitForSeconds(.5f);
        companion.BeginConversation();
        Check(!companion.IsConversing && !GameManager.Instance.InputBlocked, "Nearby walls block dialogue without leaking an input lease");
        Destroy(wall); yield return null;
        // Far and occluded direct calls cannot bypass the normal E reach rules.
        player.TeleportTo(movement.transform.position + Vector3.right * 15f, Quaternion.identity); Physics.SyncTransforms();
        yield return new WaitForSeconds(.5f);
        companion.BeginConversation();
        Check(!companion.IsConversing, "A distant direct call cannot open a through-house conversation");
        Check(!movement.TryVisit(movement.Home + Vector3.up * 6f), "Unreachable elevated goals are rejected instead of teleporting");
        Check(movement.TryVisit(movement.Home), "Original standing point remains a reachable return destination");
        routeUntil = Time.time + 65f;
        while (Vector3.Distance(movement.transform.position, movement.Home) > .12f && Time.time < routeUntil) yield return null;
        Check(Vector3.Distance(movement.transform.position, movement.Home) < .12f && movement.DistanceWalked > 1.5f,
            "Mother walks back home after visiting household furniture");
        yield return new WaitForSeconds(.5f);
        Set(movement, "nextTrip", Time.time);
        until = Time.time + 8f;
        while (Vector3.Distance(movement.transform.position, movement.Home) < .1f && Time.time < until) yield return null;
        Check(movement.IsMoving && Vector3.Distance(movement.transform.position, movement.Home) >= .1f,
            "Idle routine starts its next trip autonomously without a test-issued destination");
        Check(movement.transform.localScale == scale && flow.Pieces == pieces && flow.ActiveIndex == index,
            "Routine and dialogue never rescale the mother, complete chores or grant rewards");
    }

    private void ValidateWalk()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Characters/MomUserDemo/MomUser.prefab");
        var holder = new GameObject("Mother clip inspection holder"); holder.transform.position = Vector3.right * 100f;
        var fixture = Instantiate(prefab, holder.transform); fixture.name = "Mother walk clip inspection";
        var animator = fixture.GetComponentInChildren<Animator>(); animator.enabled = false;
        var idle = animator.runtimeAnimatorController.animationClips.First(c => c.name == "Mother_Idle_Standing");
        var walk = presentation.Animator.runtimeAnimatorController.animationClips.First(c => c.name == "Mother_Walk");
        var foot = fixture.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Foot.L");
        float Floor()
        {
            float minimum = float.PositiveInfinity;
            foreach (var renderer in fixture.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh(); renderer.BakeMesh(mesh);
                foreach (var vertex in mesh.vertices) minimum = Mathf.Min(minimum, renderer.transform.TransformPoint(vertex).y);
                Destroy(mesh);
            }
            return minimum;
        }
        try
        {
            idle.SampleAnimation(fixture, 0); float floor = Floor();
            walk.SampleAnimation(fixture, 0); Vector3 first = foot.localPosition; Quaternion rotation = foot.localRotation;
            Vector3 firstWorld = foot.position; float displacement = 0, minimum = 100;
            for (int i = 0; i <= 48; i++)
            {
                walk.SampleAnimation(fixture, walk.length * i / 48f);
                minimum = Mathf.Min(minimum, Floor()); displacement = Mathf.Max(displacement, Vector3.Distance(firstWorld, foot.position));
            }
            report.AppendLine($"INFO imported walk floor relative idle={minimum - floor}, foot displacement={displacement}, duration={walk.length}");
            Check(displacement > .04f, "Imported walk really drives the existing model's foot bones");
            Check(minimum > floor - .005f, "All 49 imported walk frames keep the mesh within 5mm of the standing floor");
            Check(Vector3.Distance(first, foot.localPosition) < .001f && Quaternion.Angle(rotation, foot.localRotation) < .2f,
                "Imported walk loop joins without a foot-pose snap");
            fixture.transform.localScale *= presentation.Animator.transform.lossyScale.y;
            animator.runtimeAnimatorController = presentation.Animator.runtimeAnimatorController;
            animator.applyRootMotion = false; animator.Rebind();
            animator.Play("Mother_Idle_Standing", 0, 0); animator.Update(0);
            float standingFloor = Floor(), transitionFloor = standingFloor;
            var clearance = holder.AddComponent<Scene2MotherFootClearance>(); clearance.Configure(animator);
            Check(clearance.ProbeCount > 0, "Generic foot clearance samples existing sole vertices without changing skin weights");
            float maxLift = 0;
            animator.CrossFadeInFixedTime("Mother_Walk", .2f, 0, 0);
            for (int i = 0; i < 8; i++) { animator.Update(1f / 30f); clearance.CorrectNow(); maxLift = Mathf.Max(maxLift, clearance.Lift); transitionFloor = Mathf.Min(transitionFloor, Floor()); }
            animator.CrossFadeInFixedTime("Mother_Idle_Standing", .2f, 0, 0);
            for (int i = 0; i < 8; i++) { animator.Update(1f / 30f); clearance.CorrectNow(); maxLift = Mathf.Max(maxLift, clearance.Lift); transitionFloor = Mathf.Min(transitionFloor, Floor()); }
            report.AppendLine($"INFO corrected actual-scale transition floor delta={transitionFloor - standingFloor}, max visual lift={maxLift}");
            Check(transitionFloor > standingFloor - .005f, "Actual-scale Idle/Walk crossfades stay within 5mm of the standing floor");
            Check(maxLift > .001f && maxLift < .02f, "Sole correction applies only a small visual clearance, not an actor warp");
        }
        finally { Destroy(holder); }
    }

    private bool FocusMother(FirstPersonController player)
    {
        var focus = movement.GetComponent<Collider>().bounds.center;
        for (int i = 0; i < 16; i++)
        {
            Vector3 feet = movement.transform.position + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * 1.8f;
            if (Physics.CheckCapsule(feet + Vector3.up * .4f, feet + Vector3.up * 1.4f, .32f, ~0, QueryTriggerInteraction.Ignore)) continue;
            player.TeleportTo(feet, Quaternion.identity); view.transform.LookAt(focus); Physics.SyncTransforms();
            typeof(PlayerInteraction).GetMethod("HandleRaycast", Private).Invoke(player.GetComponent<PlayerInteraction>(), null);
            if (player.GetComponent<PlayerInteraction>().CurrentTarget is Scene2MotherInteractable) return true;
        }
        return false;
    }

    private void Capture(string name)
    {
        Directory.CreateDirectory("Tools/MotherHome");
        var host = new GameObject("Temporary mother QA camera", typeof(Camera)); var camera = host.GetComponent<Camera>();
        camera.enabled = false; camera.nearClipPlane = .04f; camera.farClipPlane = view.farClipPlane;
        camera.cullingMask = view.cullingMask; camera.clearFlags = view.clearFlags; camera.backgroundColor = view.backgroundColor;
        var renderers = movement.GetComponentsInChildren<SkinnedMeshRenderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        Vector3 focus = movement.GetComponent<Collider>().bounds.center;
        bool ClearCamera(Vector3 position)
        {
            bool Relevant(Collider hit) => !hit.transform.IsChildOf(movement.transform) && !hit.GetComponentInParent<FirstPersonController>();
            if (Physics.OverlapSphere(position, .05f, ~0, QueryTriggerInteraction.Ignore).Any(Relevant)) return false;
            Vector3 ray = focus - position;
            return !Physics.RaycastAll(position, ray.normalized, ray.magnitude, ~0, QueryTriggerInteraction.Ignore).Any(hit => Relevant(hit.collider));
        }
        bool found = false;
        foreach (float distance in new[] { 3.8f, 3f, 2.2f, 1.6f, 1.2f, .9f })
        {
            for (int i = 0; i < 24; i++)
            {
                Vector3 position = focus + Quaternion.Euler(0, i * 15f, 0) * movement.transform.forward * distance + Vector3.up * (distance >= 1.6f ? .25f : 0);
                if (!ClearCamera(position)) continue;
                host.transform.position = position; found = true; break;
            }
            if (found) break;
        }
        if (!found && ClearCamera(view.transform.position)) { host.transform.position = view.transform.position; found = true; }
        if (!found) { Destroy(host); throw new InvalidOperationException("No clear mother QA camera position."); }
        host.transform.LookAt(focus);
        camera.fieldOfView = Mathf.Clamp(2 * Mathf.Atan2(bounds.extents.magnitude * 1.15f, Vector3.Distance(host.transform.position, focus)) * Mathf.Rad2Deg, 45f, 110f);
        var rt = RenderTexture.GetTemporary(1280, 900, 24); var old = RenderTexture.active;
        var image = new Texture2D(1280, 900, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt; camera.aspect = 1280f / 900; camera.Render(); RenderTexture.active = rt;
            Vector3 framed = camera.WorldToViewportPoint(bounds.center);
            Check(framed.z > 0 && framed.x > 0 && framed.x < 1 && framed.y > 0 && framed.y < 1,
                "QA camera frames the visible mother, not only her old invisible interaction root: " + name);
            image.ReadPixels(new Rect(0, 0, 1280, 900), 0, 0); image.Apply();
            File.WriteAllBytes("Tools/MotherHome/" + name + ".png", image.EncodeToPNG());
        }
        finally { camera.targetTexture = null; RenderTexture.active = old; RenderTexture.ReleaseTemporary(rt); Destroy(image); Destroy(host); }
    }
    private void Finish(bool passed, string detail)
    {
        if (finished) return;
        finished = true; Application.logMessageReceived -= Log;
        companion?.EndConversation(); GameManager.Instance?.SetPaused(false);
        Application.runInBackground = background;
        Directory.CreateDirectory("Tools/MotherHome");
        File.WriteAllText("Tools/MotherHome/home-checks.txt", (passed ? "PASS" : "FAIL") + " · " + assertions + " assertions\n" + report + "\n" + detail);
        Debug.Log("[MotherHomeChecks] " + (passed ? "PASS " : "FAIL ") + assertions + " · " + detail);
        EditorApplication.isPlaying = false;
    }
}
