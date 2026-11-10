using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Scene2WashingUxSliceChecks
{
    internal const string Output = "Tools/WashingUxSlice";
    private const string Pending = "KuTy.WashingSlice.Pending", Pump = "KuTy.WashingSlice.Pump", Background = "KuTy.WashingSlice.Background";
    static Scene2WashingUxSliceChecks()
    {
        EditorApplication.update += () => { if (EditorApplication.isPlaying && SessionState.GetBool(Pump, false)) EditorApplication.QueuePlayerLoopUpdate(); };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                Application.runInBackground = true; SessionState.SetBool(Pending, false);
                EditorApplication.delayCall += () => new GameObject("Washing UX evidence harness").AddComponent<Scene2WashingUxSliceRunner>();
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pump, false))
            { Application.runInBackground = SessionState.GetBool(Background, false); SessionState.SetBool(Pump, false); }
        };
    }
    [MenuItem("KuTy/Tests/Review washing UX slice with actual Game View movie")]
    public static void Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity")
            throw new InvalidOperationException("Saved Home in Edit Mode required; do not save over manual edits for a review.");
        Directory.CreateDirectory(Output); KuTyCheckpointTestScope.Capture();
        SessionState.SetBool(Background, Application.runInBackground); SessionState.SetBool(Pump, true); SessionState.SetBool(Pending, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game"); EditorApplication.EnterPlaymode();
    }
}

public sealed class Scene2WashingUxSliceRunner : MonoBehaviour
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private Scene2HouseholdSimulator sim;
    private HouseholdChoreFlow flow;
    private Camera camera;
    private MediaEncoder encoder;
    private WashingReviewGameView sizes;
    private int captureBefore, assertions, frames;
    private bool finished, recording;
    private float started;
    private Vector2 pointer;
    private RectTransform cursor;
    private UnityEngine.Video.VideoPlayer reader;
    private RenderTexture movieTarget;
    private readonly StringBuilder report = new StringBuilder();
    private T Field<T>(string name) => (T)typeof(Scene2HouseholdSimulator).GetField(name, Flags).GetValue(sim);
    private void Call(string name, params object[] args) => typeof(Scene2HouseholdSimulator).GetMethod(name, Flags).Invoke(sim, args);
    private void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); assertions++; report.AppendLine("PASS " + message); }
    private void Start() { captureBefore = Time.captureFramerate; started = Time.realtimeSinceStartup; Application.logMessageReceived += Log; StartCoroutine(Guarded()); }
    private void Log(string text, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) Finish(false, text + "\n" + stack); }
    private void Update() { if (!finished && Time.realtimeSinceStartup - started > 170) Finish(false, "Timed out"); }
    private IEnumerator Guarded()
    {
        var routine = Run();
        while (!finished)
        {
            object next;
            try { if (!routine.MoveNext()) { Finish(true, "Washing-only slice recorded. Scripted shared-pointer input, not OS-mouse UX acceptance."); yield break; } next = routine.Current; }
            catch (Exception error) { Finish(false, error.ToString()); yield break; }
            yield return next;
        }
    }
    private IEnumerator Run()
    {
        yield return null; flow = HouseholdChoreFlow.Instance; sim = flow.GetComponent<Scene2HouseholdSimulator>(); camera = Camera.main;
        var bed = FindObjectOfType<BedWakeUpController>(); bed.GetType().GetMethod("GetOutOfBed", Flags).Invoke(bed, null);
        yield return new WaitForSeconds(1.5f); FindObjectOfType<FirstPersonController>().enabled = false;
        foreach (var prerequisite in HouseholdChoreFlow.Order)
        {
            if (prerequisite == HouseholdChore.WashDishes) break;
            Check(flow.Complete(prerequisite, false), "Fixture-only API prerequisite, not a playtest or SetTable implementation: " + prerequisite);
        }
        flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue(); flow.GetComponent<Scene2KitchenHandsOn>().SilenceMother();
        sizes = new WashingReviewGameView(); sizes.Select(1280, 720); yield return null; yield return null;
        var layout = sim.GetComponent<Scene2DishwashingLayout>();
        var poses = new[] { layout.fixture, layout.basin, layout.faucet, layout.rack, layout.soap, layout.sponge }.Concat(layout.dishes.Select(d => d.source)).Select(t => (t, t.position, t.rotation, t.localScale)).ToArray();
        var materialRefs = poses.SelectMany(p => p.t.GetComponentsInChildren<MeshRenderer>(true)).Distinct().Select(r => (r, materials: r.sharedMaterials)).ToArray();
        Vector3 home = camera.transform.position; Quaternion rotation = camera.transform.rotation; float fov = camera.fieldOfView;
        Check(sim.Begin(HouseholdChore.WashDishes, () => flow.Complete(HouseholdChore.WashDishes, false), () => { }), "Authored washing opens through existing gameplay owner");
        sim.EditorWashingPointerPlayback = true; yield return new WaitForSeconds(.6f);
        Check(camera.transform.position.y - Scene2DishwashingLayout.VisualBounds(layout.basin).center.y < 1.35f, "Low washing camera retained");
        foreach (var resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1680, 720), new Vector2Int(1024, 768) })
        {
            sizes.Select(resolution.x, resolution.y); yield return null; yield return null; yield return null;
            pointer = new Vector2(Screen.width * .55f, Screen.height * .5f);
            yield return Frame("overview-" + resolution.x + "x" + resolution.y + ".png"); ValidateText();
            Check(Screen.width == resolution.x && Screen.height == resolution.y, "Actual Game View size " + resolution);
            foreach (var target in new[] { layout.faucet, layout.soap, layout.rack })
            {
                var p = camera.WorldToViewportPoint(Scene2DishwashingLayout.VisualBounds(target).center);
                Check(p.z > 0 && p.x > 0 && p.x < 1 && p.y > .07f && p.y < .95f, "Visible authored work target at " + resolution + ": " + target.name);
            }
            ClickButton("washingTextButton"); ClickButton("washingHelpButton");
            yield return Frame("help-large-" + resolution.x + "x" + resolution.y + ".png"); ValidateText();
            ClickButton("washingTextButton"); ClickButton("washingHelpButton");
        }
        sizes.Select(1280, 720); yield return null; yield return null; yield return null;
        var cursorCanvas = new GameObject("Recorded synthetic washing pointer", typeof(Canvas)); cursorCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; cursorCanvas.GetComponent<Canvas>().sortingOrder = 150;
        var dot = new GameObject("Pointer", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>(); dot.transform.SetParent(cursorCanvas.transform, false);
        cursor = dot.rectTransform; cursor.anchorMin = cursor.anchorMax = Vector2.zero; cursor.sizeDelta = new Vector2(22, 22);
        GameUiPresentation.Text(dot, 22, Color.white); dot.alignment = TextAlignmentOptions.Center; dot.text = "•";
        Time.captureFramerate = 20;
        encoder = new MediaEncoder(Scene2WashingUxSliceChecks.Output + "/washing-interaction.mp4", new VideoTrackAttributes { width = 1280, height = 720, frameRate = new MediaRational(20), includeAlpha = false }); recording = true;
        pointer = new Vector2(Screen.width * .5f, Screen.height * .5f);
        for (int i = 0; i < 12; i++) yield return Frame(i == 11 ? "idle.png" : null);
        pointer = camera.WorldToScreenPoint(Field<Transform>("moving").GetComponent<BoxCollider>().bounds.center);
        yield return Frame(null, true); yield return Frame(null, false, false, true);
        Check(Field<int>("held") == 0, "Quick click carries the current dish");
        Vector3 basin = Field<Bounds>("authoredBasinBounds").center;
        Vector3 local = Field<Transform>("world").InverseTransformPoint(basin); local.y = Field<float>("authoredPlaneHeight");
        Vector2 from = pointer, to = camera.WorldToScreenPoint(Field<Transform>("world").TransformPoint(local));
        for (int i = 1; i <= 14; i++) { pointer = Vector2.Lerp(from, to, i / 14f); yield return Frame(); }
        Check(sim.State.wetted, "Click-carried dish wets in the real basin without holding a pixel");
        var waterAnchor = layout.basin.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "WaterSurface");
        float waterY = waterAnchor ? waterAnchor.position.y : Field<Bounds>("authoredBasinBounds").center.y;
        Check(Scene2DishwashingLayout.VisualBounds(Field<Transform>("dishVisual")).min.y <= waterY + .005f,
            "Docked dish visibly touches authored water height rather than hovering above the basin");
        yield return Frame("wet.png");
        pointer = camera.WorldToScreenPoint(Scene2DishwashingLayout.VisualBounds(Field<Transform>("authoredSoap")).center);
        yield return Frame(null, true, false, true); Check(sim.State.soaped, "One soap click prepares the scrub tool");
        ClickButton("washingCloseButton"); for (int i = 0; i < 10; i++) yield return Frame(i == 9 ? "close-dirty.png" : null);
        for (int side = 0; side < 2; side++)
        {
            if (side == 1) { ClickButton("washingFlipButton"); for (int i = 0; i < 7; i++) yield return Frame(); }
            var stains = Field<Transform[]>("stains");
            for (int patch = side * 3; patch < side * 3 + 3; patch++)
            {
                int stroke = 0;
                while (sim.State.stains[patch] > .02f && stroke < 80)
                {
                    pointer = camera.WorldToScreenPoint(stains[patch].position + Vector3.right * (stroke % 2 == 0 ? -.004f : .004f));
                    yield return Frame(stroke == 6 && patch == 0 ? "scrubbing.png" : null, stroke == 0, true); stroke++;
                }
                Check(sim.State.stains[patch] <= .02f, "Visible mesh-contact strokes clean patch " + patch);
            }
            yield return Frame("clean-face-" + side + ".png", false, false, true);
        }
        Check(sim.State.Scrubbed, "Both real surfaces cleaned without progress injection");
        ClickButton("washingCloseButton"); for (int i = 0; i < 10; i++) yield return Frame();
        pointer = camera.WorldToScreenPoint(Field<Transform>("tapPick").GetComponent<Collider>().bounds.center); yield return Frame(null, true, false, true);
        pointer = camera.WorldToScreenPoint(Field<Transform>("moving").GetComponent<BoxCollider>().bounds.center);
        yield return Frame(null, true); yield return Frame(null, false, false, true);
        local = Field<Vector3>("rinseZone"); local.y = Field<float>("authoredPlaneHeight"); to = camera.WorldToScreenPoint(Field<Transform>("world").TransformPoint(local)); from = pointer;
        for (int i = 1; i <= 14; i++) { pointer = Vector2.Lerp(from, to, i / 14f); yield return Frame(); }
        for (int i = 0; i < 32; i++) yield return Frame(i == 10 ? "rinsing.png" : null);
        Check(sim.State.DishReady, "Docked running water removes the foam");
        local = Field<Vector3>("rackZone"); local.y = Field<float>("authoredPlaneHeight"); to = camera.WorldToScreenPoint(Field<Transform>("world").TransformPoint(local)); from = pointer;
        for (int i = 1; i <= 16; i++) { pointer = Vector2.Lerp(from, to, i / 16f); yield return Frame(); }
        Check(Field<bool>("washingRackReady"), "Visible rack preview agrees with commit validation");
        yield return Frame("rack-preview.png"); yield return Frame(null, true); yield return Frame(null, false, false, true);
        for (int i = 0; i < 8; i++) yield return Frame(i == 2 ? "settling.png" : null);
        Check(sim.State.delivered == 1 && Field<int>("held") == -1 && !sim.State.wetted, "First dish settles once; next dish is highlighted but not auto-picked");
        for (int i = 0; i < 10; i++) yield return Frame(i == 9 ? "next-dish.png" : null);
        recording = false; encoder.Dispose(); encoder = null; Destroy(cursorCanvas);
        sim.End();
        Check(flow.Pieces == 30 && !flow.IsCompleted(HouseholdChore.WashDishes), "One dish gives no premature chore reward");
        Field<System.Collections.IDictionary>("work").Clear();
        Check(sim.Begin(HouseholdChore.WashDishes, () => flow.Complete(HouseholdChore.WashDishes, false), () => { }) && sim.State.delivered == 1, "Actual checkpoint reconstructs partial delivered dish state");
        sim.End();
        Check(Vector3.Distance(camera.transform.position, home) < .001f && Quaternion.Angle(camera.transform.rotation, rotation) < .01f && Mathf.Abs(camera.fieldOfView - fov) < .01f, "Original camera and FOV restored");
        foreach (var pose in poses) Check(pose.t.position == pose.position && pose.t.rotation == pose.rotation && pose.t.localScale == pose.localScale, "Authored pose unchanged: " + pose.t.name);
        foreach (var entry in materialRefs) Check(entry.r.sharedMaterials.SequenceEqual(entry.materials), "Authored materials unchanged: " + entry.r.name);
        report.AppendLine("MOVIE frames=" + frames + "; 20fps; actual Game View including Canvas; synthetic pointer; silent clip, synthesized in-game audio placeholders.");
        movieTarget = new RenderTexture(1280, 720, 0); movieTarget.Create(); reader = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
        reader.playOnAwake = false; reader.source = UnityEngine.Video.VideoSource.Url; reader.url = Path.GetFullPath(Scene2WashingUxSliceChecks.Output + "/washing-interaction.mp4");
        reader.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture; reader.targetTexture = movieTarget; reader.Prepare(); float deadline = Time.realtimeSinceStartup + 20;
        while (!reader.isPrepared && Time.realtimeSinceStartup < deadline) yield return null;
        Check(reader.isPrepared && reader.width == 1280 && reader.height == 720 && Math.Abs((double)reader.frameCount - frames) <= 1, "Encoded washing clip decodes at expected size and duration");
        reader.Pause();
        foreach (int frame in new[] { 0, frames / 3, frames * 2 / 3, frames - 3 })
        {
            bool decoded = reader.frame == frame;
            UnityEngine.Video.VideoPlayer.EventHandler sought = _ => decoded = true;
            reader.seekCompleted += sought;
            if (!decoded) reader.frame = frame;
            deadline = Time.realtimeSinceStartup + 15;
            while (!decoded && Time.realtimeSinceStartup < deadline) yield return null;
            reader.seekCompleted -= sought; yield return null; yield return new WaitForEndOfFrame();
            Check(decoded && Math.Abs(reader.frame - frame) <= 1, "Native seek decodes movie sample " + frame + " (actual=" + reader.frame + ")");
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); var previous = RenderTexture.active; RenderTexture.active = movieTarget;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply(); File.WriteAllBytes(Scene2WashingUxSliceChecks.Output + "/movie-frame-" + frame + ".png", image.EncodeToPNG());
            RenderTexture.active = previous; Destroy(image);
        }
    }
    private void ClickButton(string name)
    {
        var button = Field<Button>(name); Check(button.interactable, "Available UI action: " + name);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    private IEnumerator Frame(string screenshot = null, bool down = false, bool pressed = false, bool up = false)
    {
        yield return null;
        if (sim && sim.IsWorking) { sim.ProcessPointer(pointer, down, pressed, up, false, 0, .05f); Call("UpdateCaption"); }
        if (cursor) cursor.anchoredPosition = pointer; Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        if (recording || screenshot != null)
        {
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            if (recording) { if (!encoder.AddFrame(image)) throw new InvalidOperationException("Encoder rejected a Game View frame."); frames++; }
            if (screenshot != null) File.WriteAllBytes(Scene2WashingUxSliceChecks.Output + "/" + screenshot, image.EncodeToPNG()); Destroy(image);
        }
    }
    private void ValidateText()
    {
        foreach (string name in new[] { "title", "instructions", "hint" })
        { var text = Field<TextMeshProUGUI>(name); text.ForceMeshUpdate(); Check(!text.isTextOverflowing && text.preferredHeight <= text.rectTransform.rect.height + 1, name + " fits at " + Screen.width + "x" + Screen.height); }
    }
    private void Finish(bool pass, string message)
    {
        if (finished) return; finished = true; Application.logMessageReceived -= Log;
        recording = false; encoder?.Dispose(); encoder = null; Time.captureFramerate = captureBefore;
        if (reader) reader.Stop(); if (movieTarget) { movieTarget.Release(); Destroy(movieTarget); }
        if (sim) { sim.EditorWashingPointerPlayback = false; sim.End(); }
        sizes?.Dispose(); File.WriteAllText(Scene2WashingUxSliceChecks.Output + "/checks.txt", (pass ? "PASS " : "FAIL ") + assertions + " assertions\n" + report + "\n" + message);
        EditorApplication.isPlaying = false;
    }
}

internal sealed class WashingReviewGameView : IDisposable
{
    private readonly EditorWindow window;
    private readonly object group;
    private readonly PropertyInfo selected;
    private readonly int original;
    private readonly List<int> added = new List<int>();
    private readonly Dictionary<string, int> indices = new Dictionary<string, int>();
    internal WashingReviewGameView()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        var assembly = typeof(Editor).Assembly; window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        selected = window.GetType().GetProperty("selectedSizeIndex", flags); original = (int)selected.GetValue(window);
        var type = assembly.GetType("UnityEditor.GameViewSizes"); var singleton = type.GetProperty("instance", flags).GetValue(null); var get = type.GetMethod("GetGroup", flags);
        group = get.Invoke(singleton, new[] { Enum.Parse(get.GetParameters()[0].ParameterType, "Standalone") });
    }
    internal void Select(int width, int height)
    {
        string key = width + "x" + height;
        if (!indices.TryGetValue(key, out int index))
        {
            var assembly = typeof(Editor).Assembly;
            var size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new[] { Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), (object)width, height, "KuTy washing scoped " + key });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size }); index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
            indices[key] = index; added.Add(index);
        }
        selected.SetValue(window, index); window.Repaint();
    }
    public void Dispose()
    {
        selected.SetValue(window, original); int builtIn = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
        foreach (int index in added.OrderByDescending(i => i)) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { index - builtIn }); window.Repaint();
    }
}
