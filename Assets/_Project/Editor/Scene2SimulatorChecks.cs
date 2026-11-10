using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Scene2SimulatorChecks
{
    private const string Pending = "KuTy.SimulatorChecks.Pending";
    static Scene2SimulatorChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                EditorApplication.delayCall += () => new GameObject("Simulator regression checks").AddComponent<Scene2SimulatorCheckRunner>();
            }
        };
    }
    [MenuItem("KuTy/Tests/Check household simulator")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity" || scene.isDirty)
            throw new InvalidOperationException("Open and save the home scene before this test.");
        if (!UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>()) throw new InvalidOperationException("Install the simulator first.");
        KuTyCheckpointTestScope.Capture();
        SessionState.SetBool(Pending, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.EnterPlaymode();
    }
}

public sealed class Scene2SimulatorCheckRunner : MonoBehaviour
{
    private readonly System.Text.StringBuilder report = new System.Text.StringBuilder();
    private Scene2HouseholdSimulator sim;
    private HouseholdChoreFlow flow;
    private Camera view;
    private bool finished, background;
    private float start;
    private int assertions;
    private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private Transform World => (Transform)typeof(Scene2HouseholdSimulator).GetField("world", Flags).GetValue(sim);
    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        assertions++; report.AppendLine("PASS " + message);
    }
    private void Start()
    {
        background = Application.runInBackground; Application.runInBackground = true;
        start = Time.realtimeSinceStartup;
        Application.logMessageReceived += Log;
        StartCoroutine(Guarded());
    }
    private void Log(string text, string stack, LogType type)
    {
        if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish(false, text + "\n" + stack);
    }
    private void Update() { if (!finished && Time.realtimeSinceStartup - start > 100) Finish(false, "Timed out"); }
    private IEnumerator Guarded()
    {
        var run = Run();
        while (!finished)
        {
            object current;
            try { if (!run.MoveNext()) { Finish(true, "Simulator rules, real ray gestures and lifecycle checks passed."); yield break; } current = run.Current; }
            catch (Exception error) { Finish(false, error.ToString()); yield break; }
            yield return current;
        }
    }
    private IEnumerator Run()
    {
        yield return null;
        Rules();
        flow = HouseholdChoreFlow.Instance; sim = flow.GetComponent<Scene2HouseholdSimulator>(); view = Camera.main;
        var bed = FindObjectOfType<BedWakeUpController>();
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", Flags).Invoke(bed, null);
        yield return new WaitForSeconds(1.5f);
        Check(!GameManager.Instance.InputBlocked && flow.ChecklistVisible, "Wake-up releases control into the new activities");
        var player = FindObjectOfType<FirstPersonController>(); player.enabled = false;
        flow.Voice.Stop();
        // Exercise the real existing interactable route, not just a new component added in isolation.
        Check(sim.Handles(HouseholdChore.ArrangeKeepsakes), "Authored simulator station survives scene serialization");
        var target = FindObjectsOfType<HouseholdChoreInteractable>().First(t =>
            t.Chore == HouseholdChore.ArrangeKeepsakes && t.isActiveAndEnabled && !string.IsNullOrEmpty(t.GetPromptText()));
        target.Interact();
        Check(sim.IsWorking, "Existing E interaction routes to world-space simulator");
        sim.End();
        Check(!GameManager.Instance.InputBlocked && !flow.IsCompleted(HouseholdChore.ArrangeKeepsakes), "Cancelling never grants progress or leaks input");

        foreach (var chore in HouseholdChoreFlow.Order)
        {
            flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue();
            if (!sim.Handles(chore)) { Check(flow.Complete(chore, false), "Advance unchanged activity for simulator fixture: " + chore); continue; }
            if (chore == HouseholdChore.WashDishes)
            {
                var authored = Field<Scene2HouseholdSimulator.Station[]>("stations").First(s => s.chore == chore);
                var entry = authored.surface.GetComponent<HouseholdChoreInteractable>();
                Check(entry && entry.Chore == chore, "Washing entry lives outside the visual set that hides on Begin");
                Check(FocusAuthoredWashing(player, authored), "Real E focus reaches the authored washing props from a clear standing position");
                player.GetComponent<PlayerInteraction>().CurrentTarget.Interact();
                Check(sim.IsWorking && !authored.idleDressing.gameObject.activeSelf, "Real washing Interact survives hiding idle objects");
                sim.End();
                Check(authored.idleDressing.gameObject.activeSelf && !GameManager.Instance.InputBlocked,
                    "Real washing entry restores visuals and releases input on cancellation");
                yield return null;
            }
            Vector3 home = view.transform.localPosition; Quaternion rotation = view.transform.localRotation;
            float fov = view.fieldOfView, near = view.nearClipPlane;
            int rewards = flow.Pieces;
            Check(sim.Begin(chore, () => flow.Complete(chore, false), () => { }), "Begin " + chore);
            Check(!sim.Begin(chore, null, null), "Reject nested activity: " + chore);
            yield return new WaitForSeconds(.5f);
            string pausedState = JsonUtility.ToJson(sim.State);
            GameManager.Instance.SetPaused(true);
            sim.ProcessPointer(Vector2.zero, true, true, false, false, 1, 2);
            yield return new WaitForSecondsRealtime(.1f);
            Check(JsonUtility.ToJson(sim.State) == pausedState, "Pause prevents gestures and progression: " + chore);
            GameManager.Instance.SetPaused(false);
            yield return null;
            foreach (float aspect in new[] { 16f / 9f, 21f / 9f })
            {
                view.aspect = aspect;
                Bounds framed = chore == HouseholdChore.WashDishes || Field<bool>("importedTransfer") ? Field<Bounds>("dishWorkBounds") :
                    new Bounds(new Vector3(0, .12f, 0), new Vector3(1.1f, 0, .5f));
                foreach (var corner in new[] { framed.min, framed.max })
                {
                    Vector3 viewport = view.WorldToViewportPoint(World.TransformPoint(corner));
                    Check(viewport.z > 0 && viewport.x > 0 && viewport.x < 1 && viewport.y > 0 && viewport.y < 1,
                        "Work surface remains framed at " + aspect + ": " + chore);
                }
            }
            view.ResetAspect();
            if (chore == HouseholdChore.ArrangeKeepsakes)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Temporary simulator occlusion check";
                wall.transform.position = Vector3.Lerp(view.transform.position, World.TransformPoint(new Vector3(-.39f, .07f, -.22f)), .5f);
                wall.transform.localScale = Vector3.one * .3f;
                Physics.SyncTransforms();
                Pointer(new Vector3(-.39f, .07f, -.22f), true, true, false);
                Check((int)typeof(Scene2HouseholdSimulator).GetField("held", Flags).GetValue(sim) == -1,
                    "A real solid wall blocks manipulation, not just the visual ray");
                Destroy(wall);
                yield return null;
                Pointer(new Vector3(-.39f, .07f, -.22f), true, true, false);
                Pointer(new Vector3(-.39f, .12f, .18f), false, true, false);
                Pointer(new Vector3(-.39f, .12f, .18f), false, false, true);
                Check(sim.State.placedMask == 1, "Partial placement is not a completed chore");
                sim.End();
                Check(!GameManager.Instance.InputBlocked && Vector3.Distance(home, view.transform.localPosition) < .001f,
                    "Leaving partial work restores camera and input");
                string saved = Scene2Checkpoint.LoadWork(chore);
                Check(!string.IsNullOrEmpty(saved) && HouseholdSimulatorState.Restore(chore, saved).placedMask == 1,
                    "Partial work is persisted to the checkpoint, not just the open modal");
                var memory = (System.Collections.IDictionary)typeof(Scene2HouseholdSimulator).GetField("work", Flags).GetValue(sim);
                memory.Clear();
                Check(sim.Begin(chore, () => flow.Complete(chore, false), () => { }) && sim.State.placedMask == 1,
                    "Reopening reconstructs objects from persisted work");
                yield return new WaitForSeconds(.5f);
            }
            Directory.CreateDirectory("Tools/ProjectAudit/Simulator");
            ScreenCapture.CaptureScreenshot("Tools/ProjectAudit/Simulator/" + chore + ".png");
            yield return new WaitForEndOfFrame();
            yield return null;
            if (chore == HouseholdChore.WashDishes)
            {
                ImportedArt();
                CaptureWorkView("WashDishes-Blender-16x9");
                typeof(Scene2HouseholdSimulator).GetMethod("ToggleDishCloseView", Flags).Invoke(sim, null);
                // Synthetic input must be checked before live Update can release it in an
                // unfocused Editor. Losing focus is intentionally a safe release, not an equip failure.
                Check(Field<int>("held") == 1, "Close view equips the sponge without needing to click an offscreen tool");
                string beforeFocusLoss = JsonUtility.ToJson(sim.State);
                typeof(Scene2HouseholdSimulator).GetMethod("OnApplicationFocus", Flags).Invoke(sim, new object[] { false });
                Check(Field<int>("held") == -1 && JsonUtility.ToJson(sim.State) == beforeFocusLoss,
                    "Focus loss safely returns the close-view sponge without changing dish progress");
                yield return new WaitForSeconds(.4f);
                Check(Field<float>("dishZoomBlend") > .99f, "Close view makes small authored bowls practical to scrub");
                typeof(Scene2HouseholdSimulator).GetMethod("ToggleDishCloseView", Flags).Invoke(sim, null);
                typeof(Scene2HouseholdSimulator).GetMethod("ToggleDishCloseView", Flags).Invoke(sim, null);
                Check(Field<int>("held") == 1, "Close-view sponge can be equipped again after focus loss");
                CaptureWorkView("WashDishes-CloseView");
                typeof(Scene2HouseholdSimulator).GetMethod("ToggleDishCloseView", Flags).Invoke(sim, null);
                Check(Field<int>("held") == -1, "Leaving close view returns the sponge safely");
                yield return new WaitForSeconds(.4f);
                // Holding still cannot clean; a wrong early placement must not finish a dish.
                BowlDrag(Field<Vector3>("rackZone") + Vector3.up * .12f, .1f);
                Check(sim.State.delivered == 0, "Dirty bowl cannot be placed as clean");
                BowlDrag(Field<Vector3>("rinseZone") + Vector3.up * .12f, .2f);
                ClickSoap();
                ScrubFace(0);
                float savedDirt = sim.State.stains[0];
                sim.End();
                Check(Field<Scene2HouseholdSimulator.Station>("station").idleDressing.gameObject.activeSelf,
                    "Cancelling washing restores authored idle props beside the legacy fixture");
                Field<System.Collections.IDictionary>("work").Clear();
                var washEntry = Field<Scene2HouseholdSimulator.Station>("station").surface.GetComponent<HouseholdChoreInteractable>();
                washEntry.Interact();
                Check(sim.IsWorking && sim.State.wetted && sim.State.soaped &&
                    Mathf.Approximately(sim.State.stains[0], savedDirt), "Imported dish wetness, soap and dirt survive closing and checkpoint reload");
                yield return new WaitForSeconds(.5f);
                for (int dish = 0; dish < 3; dish++) WashDish();
                Check(sim.State.delivered == 3, "Three dishes require wetting, soap, both sides, rinsing and racking");
                CaptureWorkView("WashDishes-CleanRack");
            }
            else if (chore == HouseholdChore.FetchWater || chore == HouseholdChore.MeasureRice)
            {
                ImportedWaterRice();
                CaptureWorkView(chore + "-Blender-16x9");
                GrabTransfer();
                Pointer(TransferSourcePoint(), false, true, false, 0, 1f);
                Check(sim.State.fill > .9f, "Real scoop mouth fills at imported source");
                Pointer(new Vector3(-.5f, .12f, -.26f), false, true, false, 7, .3f);
                Check(sim.State.fill > .9f && sim.State.transferred == 0 && sim.State.delivered == 0,
                    "Tilting away from receiving mouth cannot consume contents or award progress");
                sim.End(); Field<System.Collections.IDictionary>("work").Clear();
                Check(Field<HouseholdJarSource>("activeTransferJar").original.enabled, "Cancelling restores the original closed jar renderer");
                Check(sim.Begin(chore, () => flow.Complete(chore, false), () => { }) && sim.State.fill > .9f,
                    "Water/rice carried fill survives checkpoint reload");
                yield return new WaitForSeconds(.5f);
                int safety = 0;
                while (!sim.State.Complete && safety++ < 12)
                {
                    GrabTransfer();
                    Pointer(TransferSourcePoint(), false, true, false, 0, 1f);
                    Check(sim.State.fill > .9f, "Scoop fills only at source");
                    float before = sim.State.transferred;
                    Vector3 pourPoint = TransferPourPoint();
                    Pointer(pourPoint, false, true, false, 0, .1f);
                    Check(sim.State.transferred == before, "Moving a full scoop alone does not pour");
                    Pointer(pourPoint, false, true, false, 7, .3f);
                    Check(Field<LineRenderer>("pourStream").enabled, "Actual pouring lip drives the visible stream");
                    CaptureWorkView(chore + "-Pouring");
                    Pointer(pourPoint, false, true, false, 0, 1.5f);
                    Pointer(pourPoint, false, false, true);
                }
                Check(sim.State.Complete, "Scoop and tilt complete " + chore);
                CaptureWorkView(chore + "-Received");
            }
            else if (chore == HouseholdChore.ReadLetter)
            {
                Pointer(new Vector3(0, .05f, 0), true, false, false);
                Pointer(new Vector3(0, .05f, 0), true, false, false);
                Check(sim.State.letterStep == 2, "Envelope opens and letter unfolds before reading");
                Pointer(new Vector3(0, .05f, 0), true, false, false);
                Check(!sim.State.Complete, "Rapid clicks cannot skip letter reading");
                yield return new WaitForSeconds(4.2f);
                Pointer(new Vector3(0, .05f, 0), true, false, false);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if ((sim.State.placedMask & (1 << i)) != 0) continue;
                    Pointer(new Vector3(-.39f + i * .38f, .07f, -.22f), true, true, false);
                    Pointer(new Vector3(-.39f + i * .38f, .12f, .18f), false, true, false);
                    Pointer(new Vector3(-.39f + i * .38f, .12f, .18f), false, false, true);
                }
                Check(sim.State.placedMask == 7, "All three objects placed in the correct world zones: " + chore);
            }
            yield return null;
            Check(!sim.IsWorking && flow.IsCompleted(chore), "Complete through original flow: " + chore);
            Check(flow.Pieces == rewards + HouseholdChoreFlow.PiecesPerChore, "Reward exactly once: " + chore);
            Check(Vector3.Distance(home, view.transform.localPosition) < .001f && Quaternion.Angle(rotation, view.transform.localRotation) < .05f &&
                Mathf.Abs(view.fieldOfView - fov) < .001f && Mathf.Abs(view.nearClipPlane - near) < .001f,
                "Camera position, rotation and lens restored: " + chore);
            if (chore != HouseholdChore.ReadLetter) Check(!GameManager.Instance.InputBlocked, "Input released: " + chore);
            if (chore == HouseholdChore.WashDishes)
                Check(!flow.Complete(chore, false), "Actual washing completion callback cannot award a second time");
        }
        sim.ResetWork();
        var cached = (System.Collections.IDictionary)typeof(Scene2HouseholdSimulator).GetField("work", Flags).GetValue(sim);
        Check(cached.Count == 0 && sim.State == null, "Chapter restart cannot reuse already completed simulator states");
        Check(Field<Scene2HouseholdSimulator.Station[]>("stations").Where(s => s.transferJar).All(s => s.transferJar.original.enabled),
            "Chapter restart restores both original jar renderers");
    }
    private void Rules()
    {
        var s = new HouseholdSimulatorState { chore = HouseholdChore.WashDishes };
        Check(!s.Scrub(0, 1) && !s.RackDish(), "No dry cleaning or premature reward");
        s.Wet(1); s.soaped = true;
        Check(!s.Scrub(3, .03f), "Cannot clean invisible underside");
        Check(!s.Scrub(0, 0), "A stationary sponge never cleans");
        Check(s.Scrub(0, .03f) && s.stains[1] == 1, "Only touched stain loses dirt");
        var restored = HouseholdSimulatorState.Restore(s.chore, JsonUtility.ToJson(s));
        Check(restored.stains[0] == s.stains[0] && restored.wetted, "Partial cleaning survives serialization");
        Check(HouseholdSimulatorState.Restore(HouseholdChore.SetTable, JsonUtility.ToJson(s)).placedMask == 0, "Mismatched save cannot complete another chore");
        var p = new HouseholdSimulatorState { chore = HouseholdChore.SetTable };
        Check(!p.Place(0, false) && p.Place(0, true) && !p.Place(0, true), "Wrong placement rejected; duplicates ignored");
    }
    private void GrabTransfer()
    {
        var collider = Field<System.Collections.Generic.List<Collider>>("pickers")[0];
        Pointer(World.InverseTransformPoint(collider.bounds.center), true, true, false);
        Check(Field<int>("held") == 0, "Imported ladle/cup can be picked through the real pointer ray");
    }
    private Vector3 TransferSourcePoint()
    {
        var moving = Field<Transform>("moving"); var marker = Field<Transform>("transferFillCenter");
        Vector3 point = Field<Vector3>("sourceZone") - moving.InverseTransformPoint(marker.position); point.y = .12f; return point;
    }
    private Vector3 TransferPourPoint()
    {
        var moving = Field<Transform>("moving"); var marker = Field<Transform>("transferSpout");
        Vector3 offset = Quaternion.Euler(0, 0, 98) * moving.InverseTransformPoint(marker.position);
        Vector3 point = Field<Vector3>("targetZone") - offset; point.y = .12f; return point;
    }
    private void ImportedWaterRice()
    {
        Check(sim.WaterRiceArt && sim.WaterRiceArt.IsReady && Field<bool>("importedTransfer"), "Water/rice activities load serialized Blender art");
        var waterShader = sim.WaterRiceArt.waterMaterial.shader;
        Check(waterShader.name == "KuTy/Household Water" && waterShader.isSupported && !UnityEditor.ShaderUtil.ShaderHasError(waterShader),
            "Household water reflection/ripple shader compiles on the active renderer");
        if (sim.State.chore == HouseholdChore.FetchWater)
        {
            var surfaceEffect = Field<WaterRiceFill>("sourceFill").GetComponent<HouseholdWaterSurface>();
            Check(surfaceEffect, "Water uses per-surface interaction ripples rather than shared material mutation");
            surfaceEffect.Disturb(Field<Transform>("transferSource").position);
            Check(surfaceEffect.RippleCount > 0, "Water disturbance submits a ripple to the renderer");
        }
        Check(Field<System.Collections.Generic.List<Collider>>("pickers").Count == 3, "Three transfer queries replace 130 exported physics proxies");
        Check(!World.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("COL_")), "Water/rice collider proxies never render at runtime");
        foreach (var renderer in World.GetComponentsInChildren<MeshRenderer>(true))
            Check(renderer.sharedMaterials.All(m => m && m.shader && m.shader.isSupported), "Supported imported water/rice renderer: " + renderer.name);
        var moving = Field<Transform>("moving");
        Check(Vector3.Distance(moving.position, TransferPart(moving, "Grip").position) < .01f, "Delivered tool pivot is at the grip");
        var source = Field<Transform>("transferSource"); var floor = TransferPart(source, "ContentsRoot");
        Check(Vector3.Dot(floor.forward, Vector3.up) > .99f, "Imported contents attachment points upward");
        var fill = Field<WaterRiceFill>("sourceFill");
        var profile = Field<Vector2[]>("transferSourceProfile");
        var jar = Field<HouseholdJarSource>("activeTransferJar");
        Check(jar && !jar.original.enabled && jar.original.gameObject.activeInHierarchy,
            "Activity uses the existing scene jar in place, not a new bucket or basket");
        Check(!World.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Water bucket" || t.name == "Rice basket" || t.name == "Work cloth" || t.name == "Workstand leg"),
            "Jar activity adds no replacement source vessel or procedural table");
        Check(Vector3.Distance(World.TransformPoint(Field<Vector3>("sourceZone")), jar.Mouth) < .001f,
            "Scooping source follows the actual existing jar opening");
        if (sim.State.chore == HouseholdChore.FetchWater)
        {
            Check(!jar.openedLid && jar.openBody.vertexCount == jar.original.GetComponent<MeshFilter>().sharedMesh.vertexCount &&
                jar.openBody.bounds == jar.original.GetComponent<MeshFilter>().sharedMesh.bounds,
                "Already-open water jar retains its complete original geometry; no artificial lid segment");
            var body = source.Find("Original jar · open body");
            var bounds = jar.openBody.bounds;
            float maxError = new[] { bounds.min, bounds.max, bounds.center }.Select(p => Vector3.Distance(jar.original.transform.TransformPoint(p), body.TransformPoint(p))).Max();
            report.AppendLine($"INFO Jar mouth={jar.Mouth:F4}, original={jar.original.bounds}, body={body.GetComponent<Renderer>().bounds}, vertexError={maxError}");
            Check(maxError < .001f, "Runtime water jar preserves the original world-space mesh transform without moving the authored vessel");
            var innerWall = source.Find("Jar inner wall").GetComponent<MeshFilter>().sharedMesh;
            Check(body.GetComponent<Renderer>().sharedMaterials.All(m => m.GetFloat("_Cull") == 0) && innerWall.vertexCount == 48 * 4,
                "Open water jar has a tapered inner wall without a floating replacement rim");
            Check(body.GetComponent<Renderer>().sharedMaterial != jar.original.sharedMaterial,
                "Double-sided jar rendering never mutates the original authored material");
            Check(source.Find("Jar inner wall").GetComponent<Renderer>().bounds.max.y < jar.Mouth.y - .10f && fill.SurfaceY < jar.Mouth.y - .18f,
                "Water and lining stay recessed below the irregular jar lip instead of floating above its front edge");
        }
        float previous = -1;
        foreach (float level in new[] { .1f, .5f, 1f })
        {
            fill.SetLevel(level);
            Check(fill.SurfaceY > previous && fill.VisualMesh.vertexCount > 0, "Contents height increases at " + level); previous = fill.SurfaceY;
            Check(fill.VisualMesh.vertices.All(v =>
            {
                Vector3 p = source.InverseTransformPoint(fill.transform.TransformPoint(v)) - source.InverseTransformPoint(floor.position);
                return p.y >= -.001f && new Vector2(p.x, p.z).magnitude <= WaterRiceFill.Radius(profile, Mathf.Max(0, p.y)) + .001f;
            }), "Dynamic contents stay within tapered cavity profile at " + level);
            var surface = fill.VisualMesh.vertices; var indices = fill.VisualMesh.triangles; float area = 0;
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 a = surface[indices[i]], b = surface[indices[i + 1]], c = surface[indices[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (normal.normalized.y > .99f) area += normal.magnitude * .5f;
            }
            float radius = WaterRiceFill.Radius(profile, fill.SurfaceY - floor.position.y - .0007f) - .0015f;
            float expectedArea = Mathf.PI * radius * radius;
            Check(area > expectedArea * .95f && area < expectedArea * 1.02f,
                "Horizontal contents cap has full coverage without star-shaped gaps at " + level);
        }
        var carried = Field<WaterRiceFill>("scoopFill");
        // A shallow ladle legitimately cannot retain a horizontal pool at a 60-degree
        // tilt. Check a carrying angle where the cavity can still retain contents.
        moving.localRotation = Quaternion.Euler(0, 0, 20); carried.SetLevel(.7f);
        var points = carried.VisualMesh.vertices; var normals = carried.VisualMesh.normals;
        Check(points.Length > 0 && points.Where((p, i) => normals[i].y > .99f).Any() &&
            points.Where((p, i) => normals[i].y > .99f).All(p => Mathf.Abs(carried.transform.TransformPoint(p).y - carried.SurfaceY) < .002f),
            "Tilted carried contents have a world-horizontal clipped surface");
        moving.localRotation = Quaternion.identity; carried.SetLevel(0);
        Check(!carried.GetComponent<MeshRenderer>().enabled, "Empty tool has no floating contents mesh");
    }
    private static Transform TransferPart(Transform root, string suffix) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name.EndsWith("_" + suffix));
    private bool FocusAuthoredWashing(FirstPersonController player, Scene2HouseholdSimulator.Station station)
    {
        var body = player.GetComponent<CharacterController>();
        var interaction = player.GetComponent<PlayerInteraction>();
        foreach (var renderer in station.idleDressing.GetComponentsInChildren<MeshRenderer>())
        foreach (float radius in new[] { .8f, 1.2f, 1.7f }) for (int i = 0; i < 16; i++)
        {
            Vector3 target = renderer.bounds.center;
            Vector3 feet = target + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * radius; feet.y = .05f;
            player.TeleportTo(feet, Quaternion.identity); view.transform.LookAt(target); Physics.SyncTransforms();
            Vector3 center = body.transform.TransformPoint(body.center);
            float half = Mathf.Max(0, body.height * .5f - body.radius);
            if (Physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half, body.radius * .95f,
                ~0, QueryTriggerInteraction.Ignore).Any(c => !c.transform.IsChildOf(player.transform))) continue;
            typeof(PlayerInteraction).GetMethod("HandleRaycast", Flags).Invoke(interaction, null);
            if (interaction.CurrentTarget is HouseholdChoreInteractable candidate && candidate.transform == station.surface) return true;
        }
        return false;
    }
    private void Pointer(Vector3 local, bool down, bool pressed, bool up, float scroll = 0, float dt = .02f)
    {
        Vector3 screen = view.WorldToScreenPoint(World.TransformPoint(local));
        Check(screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height, "Gesture target is in the game view");
        sim.ProcessPointer(screen, down, pressed, up, false, scroll, dt);
    }
    private void BowlDrag(Vector3 destination, float dt)
    {
        Pointer(Field<Transform>("moving").localPosition + Vector3.up * .035f, true, true, false);
        if (Field<int>("held") != 0)
        {
            Vector3 screen = view.WorldToScreenPoint(Field<Transform>("moving").position + World.up * .035f);
            var ray = view.ScreenPointToRay(screen);
            report.AppendLine("Bowl blocked: pick=" + typeof(Scene2HouseholdSimulator).GetMethod("Pick", Flags).Invoke(sim, new object[] { ray }) +
                " hits=" + string.Join(",", Physics.RaycastAll(ray, 8, ~0, QueryTriggerInteraction.Collide).OrderBy(h => h.distance).Select(h => h.collider.name + ":" + h.distance.ToString("F3"))));
            Check(false, "Authored bowl must be pickable");
        }
        Pointer(destination, false, true, false, 0, dt);
        Pointer(destination, false, false, true);
    }
    private void WashDish()
    {
        BowlDrag(Field<Vector3>("rinseZone") + Vector3.up * .12f, .2f);
        ClickSoap();
        for (int side = 0; side < 2; side++)
        {
            if (side == 1)
            {
                Pointer(Field<Transform>("moving").localPosition + Vector3.up * .035f, true, true, false, 1);
                Pointer(Field<Transform>("moving").localPosition + Vector3.up * .035f, false, false, true);
            }
            ScrubFace(side);
            if (sim.State.delivered == 0) CaptureWorkView(side == 0 ? "WashDishes-InteriorFoam" : "WashDishes-ExteriorFoam");
        }
        Check(sim.State.Scrubbed, "Actual ray-driven sponge cleans both faces");
        BowlDrag(Field<Vector3>("rinseZone") + Vector3.up * .12f, 1.6f);
        Check(sim.State.DishReady, "Bowl is rinsed only after dirt removed");
        ClickSoap();
        Check(!sim.State.DishReady && sim.State.rinse == 0, "Reapplying soap requires another rinse before racking");
        BowlDrag(Field<Vector3>("rinseZone") + Vector3.up * .12f, 1.6f);
        BowlDrag(Field<Vector3>("rackZone") + Vector3.up * .12f, .1f);
    }
    private T Field<T>(string name) => (T)typeof(Scene2HouseholdSimulator).GetField(name, Flags).GetValue(sim);
    private void ClickSoap()
    {
        var soap = Field<Transform>("soapVisual");
        Pointer(World.InverseTransformPoint(soap.GetComponentInChildren<Renderer>().bounds.center), true, false, false);
    }
    private void ScrubFace(int side)
    {
        Pointer(Field<Vector3>("toolHome") + Vector3.up * .015f, true, true, false);
        var stains = Field<Transform[]>("stains");
        Vector3 contact = World.InverseTransformPoint(stains[side * 3].position);
        // A stationary contact must not clean even when already wet and soaped.
        Pointer(contact, false, true, false);
        float before = sim.State.stains[side * 3];
        Pointer(contact, false, true, false, 0, .5f);
        Check(Mathf.Approximately(before, sim.State.stains[side * 3]), "Holding sponge still on imported surface cannot clean");
        // Swiping away from every stain must not clean an arbitrary angular sector.
        var unchanged = (float[])sim.State.stains.Clone();
        Pointer(Field<Transform>("moving").localPosition + new Vector3(.11f, .02f, 0), false, true, false);
        Pointer(Field<Transform>("moving").localPosition + new Vector3(.12f, .02f, 0), false, true, false);
        Check(unchanged.SequenceEqual(sim.State.stains), "Sponge outside the real bowl cannot remove dirt");
        for (int patch = side * 3; patch < side * 3 + 3; patch++)
        {
            contact = World.InverseTransformPoint(stains[patch].position);
            for (int stroke = 0; stroke < 18; stroke++)
                Pointer(contact + Vector3.right * (stroke % 2 == 0 ? -.009f : .009f), false, true, false);
            Check(sim.State.stains[patch] <= .02f, "Scrubbing a measured surface contact removes that stain; patch=" + patch +
                " held=" + Field<int>("held") + " wet=" + sim.State.wetted + " soap=" + sim.State.soaped + " dirt=" + sim.State.stains[patch]);
        }
        Pointer(Field<Vector3>("toolHome"), false, false, true);
    }
    private void ImportedArt()
    {
        var station = Field<Scene2HouseholdSimulator.Station>("station");
        Check(station.existingFixture && station.existingFixture.enabled && station.existingFixture.gameObject.activeInHierarchy,
            "Original washing fixture stays visible during washing");
        Check(station.idleDressing && !station.idleDressing.gameObject.activeSelf,
            "Authored idle set hides while runtime props are manipulated");
        Check(!World.GetComponentsInChildren<Transform>().Any(t => t.name == "Work cloth" || t.name == "Workstand leg"),
            "Reused washing area has no duplicate procedural table or legs");
        foreach (var pair in new[] { ("WashBasin", Field<Transform>("washBasin")), ("DryingRack", Field<Transform>("dryingRack")),
            ("ScrubSponge", Field<Transform>("spongeVisual")), ("SoapBar", Field<Transform>("soapVisual")), ("DishBowl", Field<Transform>("dishVisual")) })
        {
            var source = station.idleDressing.Find(pair.Item1); var live = pair.Item2;
            Check(source && Vector3.Distance(source.position, live.position) < .001f && Quaternion.Angle(source.rotation, live.rotation) < .05f &&
                Vector3.Distance(source.lossyScale, live.lossyScale) < .001f, "Runtime preserves authored position, rotation and scale: " + pair.Item1);
        }
        var art = sim.DishwashingArt;
        Check(art && art.IsReady && Field<bool>("importedDishes"), "Scene loads serialized Blender art, not procedural dish placeholders");
        Check(Vector3.Distance(art.bowlSize, new Vector3(.14f, .065f, .14f)) < .003f, "Imported bowl is correctly oriented and 14cm wide");
        Check(Field<System.Collections.Generic.List<Collider>>("pickers").Count == 5, "Imported station uses five query triggers, not 105 physics proxies");
        Check(!World.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("COL_")), "Collider proxy renderers never enter the runtime station");
        foreach (var renderer in World.GetComponentsInChildren<MeshRenderer>(true))
            Check(renderer.sharedMaterials.All(m => m && m.shader && m.shader.isSupported), "Runtime renderer has supported materials: " + renderer.name);
        var rack = Field<Transform[]>("racked");
        Check(rack.All(t => t && Vector3.Dot(t.up, World.up) < -.99f), "All three clean bowls are upside down at exported rack slots");
        Check(art.stainNormals.Take(3).All(n => n.y > .5f) && art.stainNormals.Skip(3).All(n => n.y < -.5f),
            "Dirt contacts follow actual interior and exterior surface normals");
        foreach (string name in new[] { "CeramicBody", "BlueRim", "BasinAluminum", "SpongeFoam", "SpongeScrub", "SoapIvory", "BambooWood" })
        {
            string root = DishwashingAssetSetup.Root;
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(root + "/Materials/" + name + ".mat");
            var baseMap = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(root + "/Textures/" + name + "_BaseColor.png");
            var packed = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(root + "/Textures/" + name + "_MetallicSmoothness.png");
            var normal = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(mat.GetTexture("_BumpMap")));
            Check(mat.shader.name == "Universal Render Pipeline/Lit" && mat.GetTexture("_BaseMap") && mat.GetTexture("_MetallicGlossMap") && mat.GetTexture("_BumpMap"),
                "URP material has all authored PBR maps: " + name);
            Check(baseMap.sRGBTexture && !packed.sRGBTexture && normal.textureType == UnityEditor.TextureImporterType.NormalMap,
                "Correct colour/data import settings: " + name);
        }
    }
    private void CaptureWorkView(string name)
    {
        // Explicit 16:9 art readback, independent of the user's docked Game View size.
        // Camera rendering excludes screen-space instructions; regular screenshots include the UI.
        var previous = view.targetTexture; var active = RenderTexture.active; float aspect = view.aspect;
        var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        try
        {
            view.targetTexture = target; view.aspect = 16f / 9f; view.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            File.WriteAllBytes("Tools/ProjectAudit/Simulator/" + name + ".png", image.EncodeToPNG());
        }
        finally { view.targetTexture = previous; view.aspect = aspect; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); Destroy(image); }
    }
    private void Finish(bool passed, string detail)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= Log;
        if (sim) sim.End();
        Application.runInBackground = background;
        Directory.CreateDirectory("Tools/ProjectAudit");
        File.WriteAllText("Tools/ProjectAudit/simulator-checks.txt", (passed ? "PASS" : "FAIL") + " · " + assertions + " assertions\n" + report + "\n" + detail);
        Debug.Log("[HouseholdSimulatorChecks] " + (passed ? "PASS " : "FAIL ") + assertions + " assertions · " + detail);
        EditorApplication.isPlaying = false;
    }
}
