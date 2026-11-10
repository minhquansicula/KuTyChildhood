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
using UnityEngine.UI;

[InitializeOnLoad]
public static class Scene2TableUxSliceChecks
{
    internal const string Output = "Tools/TableUxSlice";
    private const string Pending = "KuTy.TableSlice.Pending", Pump = "KuTy.TableSlice.Pump", Background = "KuTy.TableSlice.Background";
    static Scene2TableUxSliceChecks()
    {
        EditorApplication.update += () => { if (EditorApplication.isPlaying && SessionState.GetBool(Pump, false)) EditorApplication.QueuePlayerLoopUpdate(); };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                Application.runInBackground = true; SessionState.SetBool(Pending, false);
                EditorApplication.delayCall += () => new GameObject("Table slice evidence harness").AddComponent<Scene2TableUxSliceRunner>();
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pump, false))
            {
                Application.runInBackground = SessionState.GetBool(Background, false); SessionState.SetBool(Pump, false);
            }
        };
    }
    [MenuItem("KuTy/Tests/Review table UX slice with actual Game View movie")]
    public static void Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity")
            throw new InvalidOperationException("Saved Home in Edit Mode required; never save over manual edits for this test.");
        Directory.CreateDirectory(Output); KuTyCheckpointTestScope.Capture();
        SessionState.SetBool(Background, Application.runInBackground); SessionState.SetBool(Pump, true); SessionState.SetBool(Pending, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game"); EditorApplication.EnterPlaymode();
    }
}

public sealed class Scene2TableUxSliceRunner : MonoBehaviour
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private Scene2HouseholdSimulator sim;
    private HouseholdChoreFlow flow;
    private Camera camera;
    private MediaEncoder encoder;
    private TableSliceGameViewSizes sizes;
    private int captureBefore, assertions, frames;
    private bool finished, recording;
    private float started;
    private Vector2 pointer;
    private RectTransform cursor;
    private UnityEngine.Video.VideoPlayer movieReader;
    private RenderTexture movieTarget;
    private readonly StringBuilder report = new StringBuilder();
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);
    private static void Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Flags).Invoke(owner, args);
    private void Check(bool condition, string text)
    {
        if (!condition) throw new InvalidOperationException(text);
        assertions++; report.AppendLine("PASS " + text);
    }
    private void Start()
    {
        started = Time.realtimeSinceStartup; captureBefore = Time.captureFramerate;
        Application.logMessageReceived += Log; StartCoroutine(Guarded());
    }
    private void Update() { if (!finished && Time.realtimeSinceStartup - started > 160) Finish(false, "Timed out"); }
    private void Log(string text, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) Finish(false, text + "\n" + stack); }
    private IEnumerator Guarded()
    {
        var routine = Run();
        while (!finished)
        {
            object next;
            try { if (!routine.MoveNext()) { Finish(true, "Visual slice recorded. Synthetic pointer playback, NOT an OS-mouse/user UX acceptance test."); yield break; } next = routine.Current; }
            catch (Exception error) { Finish(false, error.ToString()); yield break; }
            yield return next;
        }
    }
    private IEnumerator Run()
    {
        yield return null; flow = HouseholdChoreFlow.Instance; sim = flow.GetComponent<Scene2HouseholdSimulator>(); camera = Camera.main;
        Call(FindObjectOfType<BedWakeUpController>(), "GetOutOfBed"); yield return new WaitForSeconds(1.5f);
        FindObjectOfType<FirstPersonController>().enabled = false;
        foreach (var prerequisite in new[] { HouseholdChore.LightFire, HouseholdChore.CookRice })
            Check(flow.Complete(prerequisite, false), "Fixture advances prerequisite by API (not a mouse test): " + prerequisite);
        flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue(); flow.GetComponent<Scene2KitchenHandsOn>().SilenceMother();
        sizes = new TableSliceGameViewSizes(); sizes.Select(1280, 720);
        yield return null; yield return null;
        var layout = sim.GetComponent<Scene2AuthoredTableLayout>();
        var sources = new[] { layout.table.transform }.Concat(layout.dishes).Select(t => (t, t.position, t.rotation, t.localScale)).ToArray();
        var materials = layout.table.sharedMaterials;
        var sourceColors = materials.Select(m => m.GetColor(m.HasProperty("_BaseColor") ? "_BaseColor" : "_Color")).ToArray();
        foreach (var mat in materials)
            report.AppendLine("TABLE SOURCE material=" + AssetDatabase.GetAssetPath(mat) + "; shader=" + mat.shader.name + "; smoothness=" + (mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness").ToString() : "n/a"));
        Vector3 cameraHome = camera.transform.position; Quaternion cameraRotation = camera.transform.rotation; float fovHome = camera.fieldOfView;
        Check(sim.Begin(HouseholdChore.SetTable, () => flow.Complete(HouseholdChore.SetTable, false), () => { }), "Existing table activity opens without modifying layout");
        sim.EditorTablePointerPlayback = true;
        yield return new WaitForSeconds(.6f);
        var world = Field<Transform>(sim, "world"); var pieces = Field<Transform[]>(sim, "pieces"); var slots = Field<Vector3[]>(sim, "slots");
        Check(!world.GetComponentsInChildren<TextMeshPro>().Any(), "No permanent world-space dish/slot labels");
        Check(Field<LineRenderer[]>(sim, "tableGuides").All(g => !g.enabled), "No destinations shown before holding an item");
        Check(!layout.table.sharedMaterials.SequenceEqual(materials), "Table styling uses material copies only");
        pointer = new Vector2(Screen.width * .5f, Screen.height * .6f);
        Call(sim, "SetTableArtTone", false); yield return Frame("material-original-same-view.png");
        Call(sim, "SetTableArtTone", true); yield return Frame("material-muted-same-view.png");
        foreach (var resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1680, 720), new Vector2Int(1024, 768) })
        {
            sizes.Select(resolution.x, resolution.y); yield return null; yield return null; yield return null;
            Check(Screen.width == resolution.x && Screen.height == resolution.y, "Actual Game View resolution: " + resolution);
            pointer = new Vector2(Screen.width * .5f, Screen.height * .6f);
            yield return Frame("idle-" + resolution.x + "x" + resolution.y + ".png");
            ValidateText();
            foreach (var item in pieces)
            {
                Vector3 screen = camera.WorldToViewportPoint(item.GetComponent<BoxCollider>().bounds.center);
                Check(screen.z > 0 && screen.x > .025f && screen.x < .975f && screen.y > .09f && screen.y < .9f, "Dish stays inside safe working frame at " + resolution);
                Bounds bounds = Scene2DishwashingLayout.VisualBounds(item);
                foreach (float x in new[] { -1f, 1f }) foreach (float y in new[] { -1f, 1f }) foreach (float z in new[] { -1f, 1f })
                {
                    Vector3 corner = camera.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                    Check(corner.z > 0 && corner.x > .015f && corner.x < .985f && corner.y > .12f && corner.y < .9f, "Full dish bounds clear viewport/footer at " + resolution);
                }
            }
            Field<Button>(sim, "tableTextButton").onClick.Invoke();
            Field<Button>(sim, "tableHelpButton").onClick.Invoke();
            yield return Frame("help-large-" + resolution.x + "x" + resolution.y + ".png"); ValidateText();
            Field<Button>(sim, "tableTextButton").onClick.Invoke(); Field<Button>(sim, "tableHelpButton").onClick.Invoke();
        }
        sizes.Select(1280, 720); yield return null; yield return null; yield return null;
        pointer = new Vector2(Screen.width * .5f, Screen.height * .6f);
        var cursorCanvas = new GameObject("Recorded synthetic pointer", typeof(Canvas)); cursorCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        cursorCanvas.GetComponent<Canvas>().sortingOrder = 150;
        var cursorLabel = new GameObject("Pointer", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        cursorLabel.transform.SetParent(cursorCanvas.transform, false); cursor = cursorLabel.rectTransform; cursor.anchorMin = cursor.anchorMax = Vector2.zero;
        cursor.sizeDelta = new Vector2(24, 24); GameUiPresentation.Text(cursorLabel, 24, Color.white); cursorLabel.text = "•"; cursorLabel.alignment = TextAlignmentOptions.Center;
        Time.captureFramerate = 15;
        encoder = new MediaEncoder(Scene2TableUxSliceChecks.Output + "/table-interaction.mp4", new VideoTrackAttributes { width = (uint)Screen.width, height = (uint)Screen.height, frameRate = new MediaRational(15), includeAlpha = false });
        recording = true;
        for (int i = 0; i < 18; i++) yield return Frame(i == 17 ? "idle.png" : null);
        for (int item = 0; item < 3; item++)
        {
            Physics.SyncTransforms(); pointer = camera.WorldToScreenPoint(pieces[item].GetComponent<BoxCollider>().bounds.center);
            for (int i = 0; i < 8; i++) yield return Frame(i == 7 && item == 0 ? "hover.png" : null);
            yield return Frame(null, true, true);
            Check(Field<int>(sim, "held") == item, "Shared ray/button-edge pickup: " + item);
            if (item != 1) yield return Frame(null, false, false, true);
            for (int i = 0; i < 6; i++) yield return Frame(i == 5 && item == 0 ? "held.png" : null, false, item == 1);
            Vector2 from = pointer;
            Vector3 offset = Field<Vector3>(sim, "tableGrabOffset");
            Vector2 to = camera.WorldToScreenPoint(world.TransformPoint(slots[item] - offset + Vector3.up * .12f));
            for (int i = 1; i <= 16; i++) { pointer = Vector2.Lerp(from, to, i / 16f); yield return Frame(null, false, item == 1); }
            Check(Field<bool>(sim, "tableDropReady"), "Preview validates the same target used by commit: " + item);
            yield return Frame(item == 0 ? "valid.png" : null, false, item == 1);
            if (item != 1) yield return Frame(null, true, true);
            yield return Frame(null, false, false, true);
            for (int i = 0; i < 5; i++) yield return Frame(i == 4 ? "placed-" + item + ".png" : null);
            Check((sim.State.placedMask & (1 << item)) != 0, "Settled placement recorded once: " + item);
            if (item == 0) Check(!Field<TextMeshProUGUI>(sim, "instructions").text.Contains("1."), "Normal UI does not retain the technical stepper");
        }
        Check(sim.IsWorking && flow.IsCurrent(HouseholdChore.SetTable), "Completed table remains visible for a quiet beat before task advance");
        for (int i = 0; i < 4; i++) yield return Frame(i == 0 ? "completed.png" : null);
        recording = false; encoder.Dispose(); encoder = null;
        sim.End();
        Check(flow.Pieces == 20 && Scene2Checkpoint.LoadWork(HouseholdChore.SetTable).Contains("\"placedMask\":7"), "Leaving during the completion beat preserves all three placements without early reward");
        Field<System.Collections.IDictionary>(sim, "work").Clear();
        Check(sim.Begin(HouseholdChore.SetTable, () => flow.Complete(HouseholdChore.SetTable, false), () => { }) && sim.State.Complete, "Complete-but-pending table restores from checkpoint, not only a runtime cache");
        yield return new WaitForSeconds(1.6f);
        Check(flow.IsCurrent(HouseholdChore.WashDishes) && flow.Pieces == 30, "One table reward after the completion beat");
        Check(Vector3.Distance(camera.transform.position, cameraHome) < .001f && Quaternion.Angle(camera.transform.rotation, cameraRotation) < .01f && Mathf.Abs(camera.fieldOfView - fovHome) < .01f, "Original camera pose and FOV restored");
        Check(layout.table.sharedMaterials.SequenceEqual(materials), "Original table material references restored");
        for (int i = 0; i < materials.Length; i++) Check(materials[i].GetColor(materials[i].HasProperty("_BaseColor") ? "_BaseColor" : "_Color") == sourceColors[i], "Shared material colour untouched: " + i);
        foreach (var source in sources) Check(source.t.position == source.position && source.t.rotation == source.rotation && source.t.localScale == source.localScale, "Source pose preserved: " + source.t.name);
        report.AppendLine("MOVIE frames=" + frames + "; 15fps; actual Game View INCLUDING Canvas; synthetic pointer overlay; silent video (no claim of recorded foley).");
        Destroy(cursorCanvas);
        movieTarget = new RenderTexture(1280, 720, 0); movieTarget.Create();
        movieReader = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
        movieReader.playOnAwake = false; movieReader.source = UnityEngine.Video.VideoSource.Url;
        movieReader.url = Path.GetFullPath(Scene2TableUxSliceChecks.Output + "/table-interaction.mp4");
        movieReader.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture; movieReader.targetTexture = movieTarget;
        movieReader.Prepare(); float deadline = Time.realtimeSinceStartup + 20;
        while (!movieReader.isPrepared && Time.realtimeSinceStartup < deadline) yield return null;
        report.AppendLine("DECODE prepared=" + movieReader.isPrepared + "; size=" + movieReader.width + "x" + movieReader.height + "; estimatedFrames=" + movieReader.frameCount + "; seconds=" + movieReader.length);
        // VideoPlayer derives frameCount from native container timing; allow one frame of rounding.
        Check(movieReader.isPrepared && movieReader.width == 1280 && movieReader.height == 720 && Math.Abs((double)movieReader.frameCount - frames) <= 1,
            "Encoded MP4 decodes at expected dimensions and full duration (one-frame metadata rounding tolerance)");
        movieReader.Play(); if (movieReader.canSetPlaybackSpeed) movieReader.playbackSpeed = 3;
        foreach (int frame in new[] { 0, 35, 70, 105, 138 })
        {
            deadline = Time.realtimeSinceStartup + 15;
            while (movieReader.frame < frame && Time.realtimeSinceStartup < deadline) yield return null;
            Check(movieReader.frame >= frame, "Decoded movie reaches review sample " + frame);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            RenderTexture.active = movieTarget; image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            File.WriteAllBytes(Scene2TableUxSliceChecks.Output + "/movie-frame-" + frame + ".png", image.EncodeToPNG());
            RenderTexture.active = previous; Destroy(image);
        }
    }
    private IEnumerator Frame(string screenshot = null, bool down = false, bool pressed = false, bool up = false)
    {
        yield return null;
        if (sim && sim.IsWorking) { sim.ProcessPointer(pointer, down, pressed, up, false, 0, 1f / 15); Call(sim, "UpdateCaption"); }
        if (cursor) cursor.anchoredPosition = pointer;
        Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        if (recording || screenshot != null)
        {
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            if (recording) { if (!encoder.AddFrame(image)) throw new InvalidOperationException("MediaEncoder rejected a Game View frame."); frames++; }
            if (screenshot != null) File.WriteAllBytes(Scene2TableUxSliceChecks.Output + "/" + screenshot, image.EncodeToPNG());
            Destroy(image);
        }
    }
    private void ValidateText()
    {
        foreach (string name in new[] { "title", "instructions", "hint" })
        {
            var text = Field<TextMeshProUGUI>(sim, name); text.ForceMeshUpdate();
            Check(!text.isTextOverflowing && text.preferredHeight <= text.rectTransform.rect.height + 1, name + " fits actual UI at " + Screen.width + "x" + Screen.height);
        }
    }
    private void Finish(bool pass, string message)
    {
        if (finished) return; finished = true; Application.logMessageReceived -= Log;
        recording = false; encoder?.Dispose(); encoder = null; Time.captureFramerate = captureBefore;
        if (movieReader) movieReader.Stop();
        if (movieTarget) { movieTarget.Release(); Destroy(movieTarget); }
        if (sim) { sim.EditorTablePointerPlayback = false; sim.End(); }
        sizes?.Dispose();
        File.WriteAllText(Scene2TableUxSliceChecks.Output + "/checks.txt", (pass ? "PASS " : "FAIL ") + assertions + " assertions\n" + report + "\n" + message);
        EditorApplication.isPlaying = false;
    }
}

internal sealed class TableSliceGameViewSizes : IDisposable
{
    private readonly EditorWindow window;
    private readonly object group;
    private readonly PropertyInfo selected;
    private readonly int original;
    private readonly List<int> added = new List<int>();
    private readonly Dictionary<string, int> indices = new Dictionary<string, int>();
    internal TableSliceGameViewSizes()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        var assembly = typeof(Editor).Assembly;
        window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        selected = window.GetType().GetProperty("selectedSizeIndex", flags); original = (int)selected.GetValue(window);
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = sizesType.GetProperty("instance", flags).GetValue(null);
        var method = sizesType.GetMethod("GetGroup", flags);
        group = method.Invoke(singleton, new[] { Enum.Parse(method.GetParameters()[0].ParameterType, "Standalone") });
    }
    internal void Select(int width, int height)
    {
        string key = width + "x" + height;
        if (!indices.TryGetValue(key, out int index))
        {
            var assembly = typeof(Editor).Assembly;
            var size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new[] { Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), (object)width, height, "KuTy scoped review " + key });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
            indices[key] = index; added.Add(index);
        }
        selected.SetValue(window, index); window.Repaint();
    }
    public void Dispose()
    {
        selected.SetValue(window, original);
        int builtIn = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
        foreach (int index in added.OrderByDescending(i => i)) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { index - builtIn });
        window.Repaint();
    }
}
