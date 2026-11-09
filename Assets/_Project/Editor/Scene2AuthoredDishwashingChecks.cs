using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Scene2AuthoredDishwashingChecks
{
    private const string Pending = "KuTy.AuthoredDishwashingChecks.Pending";
    static Scene2AuthoredDishwashingChecks()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            EditorApplication.delayCall += () => new GameObject("Authored dishwashing checks").AddComponent<Scene2AuthoredDishwashingCheckRunner>();
        };
    }
    [MenuItem("KuTy/Tests/Check user-arranged dishwashing simulator")]
    public static void Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity")
            throw new InvalidOperationException("Open saved Home in Edit Mode first.");
        var layout = UnityEngine.Object.FindObjectOfType<Scene2DishwashingLayout>();
        if (!layout || !layout.IsReady) throw new InvalidOperationException("Bind the arranged dishwashing layout first.");
        KuTyCheckpointTestScope.Capture();
        Directory.CreateDirectory(Scene2AuthoredDishwashingSetup.Output);
        SessionState.SetBool(Pending, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.EnterPlaymode();
    }
}

public sealed class Scene2AuthoredDishwashingCheckRunner : MonoBehaviour
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly StringBuilder report = new StringBuilder();
    private Scene2HouseholdSimulator sim;
    private HouseholdChoreFlow flow;
    private Scene2DishwashingLayout layout;
    private Camera view;
    private bool finished, background;
    private int assertions;
    private float started;
    private T Field<T>(string name) => (T)typeof(Scene2HouseholdSimulator).GetField(name, Flags).GetValue(sim);
    private Transform World => Field<Transform>("world");
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
    private void Update() { if (!finished && Time.realtimeSinceStartup - started > 100) Finish(false, "Timed out"); }
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
            try { if (!run.MoveNext()) { Finish(true, "Ray-driven washing sequence and lifecycle checks completed."); yield break; } next = run.Current; }
            catch (Exception error) { Finish(false, error.ToString()); yield break; }
            yield return next;
        }
    }
    private IEnumerator Run()
    {
        yield return null;
        flow = HouseholdChoreFlow.Instance; sim = flow.GetComponent<Scene2HouseholdSimulator>();
        layout = sim.GetComponent<Scene2DishwashingLayout>(); view = Camera.main;
        var bed = FindObjectOfType<BedWakeUpController>();
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", Flags).Invoke(bed, null);
        yield return new WaitForSeconds(1.5f);
        Check(!GameManager.Instance.InputBlocked && flow.ChecklistVisible, "Wake-up leaves the activity available in the story flow");
        var poses = new[] { layout.fixture, layout.faucet, layout.basin, layout.rack, layout.soap, layout.sponge }
            .Concat(layout.dishes.Select(d => d.source)).Select(t => (t, t.position, t.rotation, t.localScale)).ToArray();
        foreach (var chore in HouseholdChoreFlow.Order)
        {
            if (chore == HouseholdChore.WashDishes) break;
            flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue();
            Check(flow.Complete(chore, false), "Test fixture advances preceding story task: " + chore);
        }
        flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue();
        int pieces = flow.Pieces;
        var player = FindObjectOfType<FirstPersonController>(); player.enabled = false;
        Check(FocusFixture(player), "PlayerInteraction ray can focus the new washing fixture from a clear standing position");
        var entry = player.GetComponent<PlayerInteraction>().CurrentTarget;
        entry.Interact();
        Check(sim.IsWorking && Field<bool>("authoredDishes"), "Existing Interact route opens the user-arranged simulator");
        Check(layout.fixture.gameObject.activeInHierarchy && layout.faucet.gameObject.activeInHierarchy, "Fixture and faucet remain visible while working");
        Check(layout.dishes.All(d => !d.source.gameObject.activeSelf), "Original dishes are hidden only while their runtime copies are used");
        Check(!sim.Begin(HouseholdChore.WashDishes, null, null), "Nested activity is rejected");
        yield return new WaitForSeconds(.6f);
        Capture("01-station");
        foreach (float aspect in new[] { 16f / 9f, 21f / 9f })
        {
            view.aspect = aspect;
            foreach (var t in new[] { layout.fixture, layout.faucet, layout.rack })
            {
                Vector3 p = view.WorldToViewportPoint(Scene2DishwashingLayout.VisualBounds(t).center);
                Check(p.z > 0 && p.x > .02f && p.x < .98f && p.y > .1f && p.y < .90f, "Visible work area at aspect " + aspect + ": " + t.name);
            }
        }
        view.ResetAspect();
        Click(Field<Transform>("authoredSoap"));
        Check(!sim.State.soaped, "Dry dish cannot acquire soap");
        Drag(Field<Vector3>("rackZone"), 1);
        Check(sim.State.delivered == 0, "Dirty dish cannot be delivered");
        Drag(Field<Vector3>("rinseZone"), 10);
        Check(!sim.State.wetted && sim.State.rinse == 0, "Closed tap cannot wet or rinse outside the basin");
        Click(Field<Transform>("tapPick"));
        Check(Field<bool>("faucetOpen"), "Clicking the actual faucet opens running water");
        GameManager.Instance.SetPaused(true);
        string paused = JsonUtility.ToJson(sim.State);
        sim.ProcessPointer(Vector2.zero, true, true, false, false, 1, .05f);
        Check(!Field<bool>("faucetOpen") && JsonUtility.ToJson(sim.State) == paused, "Pause closes the tap and prevents all gestures");
        GameManager.Instance.SetPaused(false);
        yield return null;
        Click(Field<Transform>("tapPick"));
        Drag(Field<Vector3>("rinseZone"), 10);
        Check(sim.State.wetted && sim.State.rinse == 0, "Running tap wets a dirty dish but cannot finish rinsing it");
        Click(Field<Transform>("authoredSoap"));
        Check(sim.State.soaped, "Clicking soap after wetting lathers the dish");
        PickSponge();
        var patch = Field<Transform[]>("stains")[0];
        float dirt = sim.State.stains[0];
        for (int i = 0; i < 20; i++) Pointer(patch.position, false, true, false);
        Check(Mathf.Approximately(dirt, sim.State.stains[0]), "Holding the sponge still does not clean a patch");
        sim.ProcessPointer(Vector2.zero, false, false, false, true, 0, .02f);
        typeof(Scene2HouseholdSimulator).GetMethod("ToggleDishCloseView", Flags).Invoke(sim, null);
        Check(Field<int>("held") == 1, "F close-view shortcut equips the sponge without clicking a hidden tool");
        yield return new WaitForSeconds(.4f);
        Check(Field<float>("dishZoomBlend") > .99f, "Close-up smoothly frames the current physical dish");
        Vector3 closeTarget = view.WorldToViewportPoint(Field<Transform>("moving").position);
        Check(closeTarget.z > 0 && closeTarget.x > .1f && closeTarget.x < .9f && closeTarget.y > .1f && closeTarget.y < .9f, "Current dish stays visible in close view");
        var closePatch = Field<Transform[]>("stains")[0];
        float beforeCloseScrub = sim.State.stains[0];
        Pointer(closePatch.position - Vector3.right * .004f, true, true, false);
        Check(Field<int>("held") == 1, "Actual mouse-down on the close-up dish keeps the sponge equipped");
        for (int i = 0; i < 4; i++) Pointer(closePatch.position + Vector3.right * (i % 2 == 0 ? .004f : -.004f), false, true, false);
        Pointer(closePatch.position, false, false, true);
        Check(sim.State.stains[0] < beforeCloseScrub, "Close-up mouse strokes actually clean the contacted patch");
        Capture("02-close-foam");
        ScreenCapture.CaptureScreenshot(Scene2AuthoredDishwashingSetup.Output + "/02-close-foam-ui.png");
        yield return new WaitForEndOfFrame();
        yield return null;
        typeof(Scene2HouseholdSimulator).GetMethod("ToggleDishCloseView", Flags).Invoke(sim, null);
        yield return new WaitForSeconds(.4f);
        PickSponge();
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "Temporary washing occlusion test";
        obstacle.transform.position = Vector3.Lerp(view.transform.position, Field<Transform[]>("stains")[0].position, .5f);
        obstacle.transform.localScale = Vector3.one * .16f;
        string blocked = JsonUtility.ToJson(sim.State);
        for (int i = 0; i < 10; i++) Pointer(Field<Transform[]>("stains")[0].position + Vector3.right * (i % 2 == 0 ? -.004f : .004f), false, true, false);
        Check(JsonUtility.ToJson(sim.State) == blocked, "A solid obstacle blocks held-sponge contact, not only initial picking");
        Destroy(obstacle);
        yield return null;
        ScrubFace(0);
        Check(sim.State.stains.Take(3).All(v => v <= .02f) && !sim.State.Scrubbed, "Only the exposed interior is cleaned; underside stays dirty");
        string partial = JsonUtility.ToJson(sim.State);
        typeof(Scene2HouseholdSimulator).GetMethod("OnApplicationFocus", Flags).Invoke(sim, new object[] { false });
        Check(Field<int>("held") == -1 && !Field<bool>("faucetOpen") && JsonUtility.ToJson(sim.State) == partial, "Focus loss returns tools, closes tap and preserves work");
        sim.End();
        Check(!GameManager.Instance.InputBlocked && layout.dishes.All(d => d.source.gameObject.activeSelf), "Cancel restores all original dishes and releases input");
        Check(flow.Pieces == pieces && !flow.IsCompleted(HouseholdChore.WashDishes), "Partial washing gives no reward or story completion");
        Check(!string.IsNullOrEmpty(Scene2Checkpoint.LoadWork(HouseholdChore.WashDishes)), "Partial scrub state is saved to checkpoint");
        ((IDictionary)typeof(Scene2HouseholdSimulator).GetField("work", Flags).GetValue(sim)).Clear();
        Check(sim.Begin(HouseholdChore.WashDishes, () => flow.Complete(HouseholdChore.WashDishes, false), () => { }), "Reopen from saved work");
        yield return new WaitForSeconds(.5f);
        Check(sim.State.stains.Take(3).All(v => v <= .02f) && sim.State.soaped, "Reopening reconstructs actual partial work, not just a progress label");
        for (int dish = 0; dish < 3; dish++)
        {
            if (dish > 0)
            {
                Check(!sim.State.wetted && !sim.State.soaped && sim.State.stains.All(v => v > .99f), "Next physical item starts dirty: " + dish);
                Click(Field<Transform>("tapPick"));
                if (!Field<bool>("faucetOpen")) Click(Field<Transform>("tapPick"));
                Drag(Field<Vector3>("rinseZone"), 8);
                Click(Field<Transform>("authoredSoap"));
                ScrubFace(0);
            }
            Flip(); ScrubFace(1);
            Check(sim.State.Scrubbed, "Both surfaces scrubbed by mesh-contact ray gestures: " + layout.dishes[dish].label);
            Capture("02-scrubbed-" + dish);
            if (!Field<bool>("faucetOpen")) Click(Field<Transform>("tapPick"));
            Drag(Field<Vector3>("rinseZone"), 35);
            Check(sim.State.DishReady, "Sustained running water removes lather: " + dish);
            if (dish == 0)
            {
                Click(Field<Transform>("authoredSoap"));
                Check(!sim.State.DishReady && sim.State.rinse == 0, "Reapplying soap requires another rinse");
                Drag(Field<Vector3>("rinseZone"), 35);
            }
            Capture("03-rinsed-" + dish);
            Drag(Field<Vector3>("rackZone"), 1);
            Check(sim.State.delivered == dish + 1, "Only releasing clean item over the actual rack delivers it: " + dish);
        }
        Capture("04-all-clean");
        Check(sim.State.Complete && !Field<bool>("faucetOpen"), "All three items delivered; faucet closes safely");
        var visuals = Field<Transform[]>("authoredVisuals");
        for (int i = 0; i < 3; i++)
        {
            Bounds a = Scene2DishwashingLayout.VisualBounds(visuals[i]);
            Check(a.min.y >= Scene2DishwashingLayout.VisualBounds(layout.rack).min.y + .045f, "Clean dish sits above the rack base: " + i);
            for (int j = i + 1; j < 3; j++)
                Check(!a.Intersects(Scene2DishwashingLayout.VisualBounds(visuals[j])), "Racked dishes do not overlap: " + i + "/" + j);
        }
        yield return null;
        Check(flow.IsCompleted(HouseholdChore.WashDishes) && flow.Pieces == pieces + HouseholdChoreFlow.PiecesPerChore, "Completion grants exactly one normal chore reward");
        Check(!flow.Complete(HouseholdChore.WashDishes, false), "Duplicate reward is rejected");
        Check(!sim.IsWorking && !GameManager.Instance.InputBlocked, "Completion restores player control");
        foreach (var p in poses)
            Check(Vector3.Distance(p.t.position, p.position) < .00001f && Quaternion.Angle(p.t.rotation, p.rotation) < .001f && p.t.localScale == p.localScale,
                "Original authored pose preserved: " + p.t.name);
        sim.ResetWork();
        Check(layout.dishes.All(d => d.source.gameObject.activeSelf) && layout.sponge.gameObject.activeSelf && layout.soap.gameObject.activeSelf, "Reset restores every source prop, not duplicate runtime models");
    }
    private bool FocusFixture(FirstPersonController player)
    {
        var body = player.GetComponent<CharacterController>(); var interaction = player.GetComponent<PlayerInteraction>();
        var bounds = Scene2DishwashingLayout.VisualBounds(layout.fixture);
        foreach (float offset in new[] { 0f, -.7f, .7f })
        foreach (float radius in new[] { 1.1f, 1.5f, 1.9f }) for (int i = 0; i < 16; i++)
        {
            Vector3 target = bounds.center + Vector3.right * offset;
            Vector3 feet = target + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * radius; feet.y = .05f;
            player.TeleportTo(feet, Quaternion.identity); view.transform.LookAt(target); Physics.SyncTransforms();
            Vector3 center = body.transform.TransformPoint(body.center); float half = Mathf.Max(0, body.height * .5f - body.radius);
            if (Physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half, body.radius * .95f, ~0, QueryTriggerInteraction.Ignore)
                .Any(c => !c.transform.IsChildOf(player.transform))) continue;
            typeof(PlayerInteraction).GetMethod("HandleRaycast", Flags).Invoke(interaction, null);
            if (interaction.CurrentTarget is HouseholdChoreInteractable candidate && candidate.transform == layout.fixture) return true;
        }
        return false;
    }
    private void Pointer(Vector3 position, bool down, bool pressed, bool up, float scroll = 0)
    {
        Vector3 screen = view.WorldToScreenPoint(position);
        if (screen.z <= 0) throw new InvalidOperationException("Target behind camera");
        sim.ProcessPointer(screen, down, pressed, up, false, scroll, .05f);
    }
    private void Click(Transform target)
    {
        Physics.SyncTransforms();
        Vector3 point = target.GetComponent<Collider>() ? target.GetComponent<Collider>().bounds.center : Scene2DishwashingLayout.VisualBounds(target).center;
        Pointer(point, true, false, true);
    }
    private void PickDish()
    {
        Physics.SyncTransforms();
        var moving = Field<Transform>("moving");
        Pointer(moving.GetComponent<BoxCollider>().bounds.center, true, false, false);
        if (Field<int>("held") != 0) DiagnosePick(moving.position, "Current dish is blocked");
    }
    private void PickSponge()
    {
        Physics.SyncTransforms();
        Pointer(Field<Transform>("tool").GetComponent<BoxCollider>().bounds.center, true, false, false);
        if (Field<int>("held") != 1) DiagnosePick(Field<Transform>("tool").position, "Sponge is blocked");
    }
    private void Drag(Vector3 point, int frames)
    {
        PickDish(); point.y = Field<float>("authoredPlaneHeight");
        for (int i = 0; i < frames; i++) Pointer(World.TransformPoint(point), false, true, false);
        Pointer(World.TransformPoint(point), false, false, true);
    }
    private void Flip()
    {
        PickDish(); Pointer(Field<Transform>("moving").position, false, false, true, 1);
        Check(sim.State.flipped, "Mouse wheel turns the held item to its underside");
    }
    private void ScrubFace(int side)
    {
        PickSponge();
        for (int i = side * 3; i < side * 3 + 3; i++)
        {
            for (int cycle = 0; cycle < 120 && sim.State.stains[i] > .02f; cycle++)
            {
                var stain = Field<Transform[]>("stains")[i];
                Vector3 p = stain.position;
                // A small physical wiping stroke; it must actually intersect the visible mesh.
                p.x += cycle % 2 == 0 ? -.004f : .004f;
                Pointer(p, false, true, false);
            }
            if (sim.State.stains[i] > .02f)
            {
                var stain = Field<Transform[]>("stains")[i];
                var ray = view.ScreenPointToRay(view.WorldToScreenPoint(stain.position));
                string hits = string.Join(", ", Physics.RaycastAll(ray, 8, ~0, QueryTriggerInteraction.Collide).OrderBy(h => h.distance)
                    .Select(h => h.collider.name + ":" + h.distance.ToString("F3") + " delta=" + Vector3.Distance(h.point, stain.position).ToString("F4")));
                Capture("diagnostic-blocked-patch");
                throw new InvalidOperationException("Patch did not clean: dish=" + sim.State.delivered + " side=" + side + " patch=" + i + " dirt=" + sim.State.stains[i] + "; " + hits);
            }
        }
        sim.ProcessPointer(Vector2.zero, false, false, false, true, 0, .05f);
    }
    private void DiagnosePick(Vector3 point, string detail)
    {
        var ray = view.ScreenPointToRay(view.WorldToScreenPoint(point));
        throw new InvalidOperationException(detail + "; pick=" + typeof(Scene2HouseholdSimulator).GetMethod("Pick", Flags).Invoke(sim, new object[] { ray }) +
            "; hits=" + string.Join(", ", Physics.RaycastAll(ray, 8, ~0, QueryTriggerInteraction.Collide).OrderBy(h => h.distance).Select(h => h.collider.name + ":" + h.distance.ToString("F3"))));
    }
    private void Capture(string name)
    {
        var previous = view.targetTexture; var active = RenderTexture.active; float aspect = view.aspect;
        var target = RenderTexture.GetTemporary(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        try
        {
            view.targetTexture = target; view.aspect = 16f / 9f; view.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            File.WriteAllBytes(Scene2AuthoredDishwashingSetup.Output + "/" + name + ".png", image.EncodeToPNG());
        }
        finally { view.targetTexture = previous; view.aspect = aspect; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); Destroy(image); }
    }
    private void Finish(bool passed, string detail)
    {
        if (finished) return; finished = true; Application.logMessageReceived -= Log;
        if (sim) sim.End(); Application.runInBackground = background;
        File.WriteAllText(Scene2AuthoredDishwashingSetup.Output + "/checks.txt", (passed ? "PASS" : "FAIL") + " · " + assertions + " assertions\n" + report + "\n" + detail);
        Debug.Log("[AuthoredDishwashingChecks] " + (passed ? "PASS " : "FAIL ") + assertions + " assertions · " + detail);
        EditorApplication.isPlaying = false;
    }
}
