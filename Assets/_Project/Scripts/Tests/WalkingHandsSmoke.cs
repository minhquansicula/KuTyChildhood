#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class WalkingHandsSmoke : MonoBehaviour
{
    private const string Pending = "KuTy.WalkingHandsSmoke.Pending";
    private bool oldBackground;
    private int oldCaptureRate;
    private FirstPersonController player;
    private ChildWalkingHands hands;
    private Camera view;
    private int leftVisible, rightVisible;

    [MenuItem("Tools/KuTy/Tests/Walking Hands Smoke")]
    private static void Run()
    {
        if (EditorApplication.isPlaying) return;
        System.Type.GetType("KuTyCheckpointTestScope, Assembly-CSharp-Editor").GetMethod("Capture").Invoke(null, null);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("Walking Hands Smoke").AddComponent<WalkingHandsSmoke>();
    }

    private IEnumerator Start()
    {
        oldBackground = Application.runInBackground;
        oldCaptureRate = Time.captureFramerate;
        Application.runInBackground = true;
        yield return null;
        player = FindObjectOfType<FirstPersonController>();
        hands = FindObjectOfType<ChildWalkingHands>();
        var bed = FindObjectOfType<BedWakeUpController>();
        if (!Check(player != null && hands != null && bed != null, "Walking hands attach in Scene2")) yield break;
        if (!Check(!hands.IsVisible, "Walking hands stay hidden during wake-up")) yield break;
        float deadline = Time.realtimeSinceStartup + 20f;
        while (!bed.CanPauseWhileWaiting && Time.realtimeSinceStartup < deadline) yield return null;
        if (!Check(bed.CanPauseWhileWaiting, "Existing wake-up reaches E prompt")) yield break;
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bed, null);
        yield return new WaitForSeconds(1.5f);
        player.enabled = false; // Drive the real controller without competing physical keyboard input.
        view = player.PlayerCamera.GetComponent<Camera>();
        view.aspect = 16f / 9f;
        float nearClip = view.nearClipPlane;
        Vector4 first = hands.SampleStrideForTests(.25f);
        if (!Check(ValidateFraming(.09f), "Right hand rises clearly into the frame at full reach")) yield break;
        if (!Check(rightVisible > 0 && leftVisible == 0, "Right hand appears while left hand is below frame")) yield break;
        Vector4 opposite = hands.SampleStrideForTests(.75f);
        if (!Check(ValidateFraming(.09f), "Left hand rises clearly into the frame at full reach")) yield break;
        if (!Check(leftVisible > 0 && rightVisible == 0, "Left hand appears while right hand is below frame")) yield break;
        Debug.Log("[WalkingHandsSmoke] Quarter stride=" + first + "; opposite stride=" + opposite);
        if (!Check(first.x > opposite.x + .01f && first.y > opposite.y + .02f &&
            first.z < opposite.z - .01f && first.w < opposite.w - .02f,
            "Right advances while left retreats, then both exchange on the next step")) yield break;
        string folder = "Tools/Cutscene/WalkingHands/Frames";
        Directory.CreateDirectory(folder);
        var times = new List<string>();
        float elapsed = 0f, capture = 0f, distance = 0f;
        bool sawHands = false, framingChecked = false;
        while (elapsed < 6f)
        {
            var before = player.transform.position;
            Vector2 input = Mathf.FloorToInt(elapsed / .55f) % 2 == 0 ? Vector2.up : Vector2.down;
            player.SimulateMovementForTests(input, false, Time.deltaTime);
            distance += Vector3.Distance(before, player.transform.position);
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
            if (hands.IsVisible) sawHands = true;
            if (!framingChecked && hands.MotionAmount > .95f)
            {
                ScreenCapture.CaptureScreenshot("Tools/Cutscene/WalkingHands/walking.png");
                yield return null;
                if (!Check(ValidateFraming(), "Hands remain at the bottom corners")) yield break;
                framingChecked = true;
            }
            if (elapsed >= capture)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "frame-" + times.Count.ToString("D4") + ".png"));
                times.Add(elapsed.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
                capture = elapsed + .1f;
            }
        }
        File.WriteAllLines(Path.Combine(folder, "times.txt"), times);
        if (!Check(distance > .3f && sawHands, "Actual controller movement reveals the hands")) yield break;
        if (!Check(framingChecked, "Measured hands during an actual full stride")) yield break;
        yield return new WaitForSeconds(.8f);
        if (!Check(!hands.IsVisible, "Standing still lowers hands out of view")) yield break;
        float stoppedPhase = hands.StridePhase;
        yield return new WaitForSeconds(.2f);
        if (!Check(Mathf.Abs(hands.StridePhase - stoppedPhase) < .0001f, "No stride animation without travel")) yield break;
        var interaction = player.GetComponent<PlayerInteraction>();
        var targetField = typeof(PlayerInteraction).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic);
        var target = new ReachTarget();
        targetField.SetValue(interaction, null);
        if (!Check(interaction.TryInteract() && hands.IsReaching, "E raises the hand without an interaction target in Scene2")) yield break;
        if (!Check(!interaction.TryInteract(), "Repeated empty E does not restart the hand motion")) yield break;
        yield return new WaitForSeconds(.65f);
        if (!Check(!hands.IsReaching && !hands.IsVisible, "Empty E returns the hand below frame")) yield break;
        interaction.IsInteractionLocked = true;
        targetField.SetValue(interaction, target);
        if (!Check(!interaction.TryInteract() && !hands.IsReaching, "Locked interaction cannot start a reach")) yield break;
        interaction.IsInteractionLocked = false;
        Time.captureFramerate = 30; // Deterministic frames while recording the short reach.
        if (!Check(interaction.TryInteract() && hands.IsReaching && target.count == 0,
            "E starts the reach before dispatching the target")) yield break;
        if (!Check(!interaction.TryInteract(), "Repeated E cannot queue duplicate interactions")) yield break;
        float reachStart = Time.time;
        bool checkedReach = false;
        int reachShot = 0;
        string reachFolder = "Tools/Cutscene/InteractionHands/Frames";
        Directory.CreateDirectory(reachFolder);
        var reachTimes = new List<string>();
        while (Time.time - reachStart < 1.2f)
        {
            yield return new WaitForEndOfFrame();
            float t = Time.time - reachStart;
            if (!checkedReach && t > .14f && t < .25f)
            {
                if (!Check(ValidateFraming(0f, .42f) && rightVisible > 0 && leftVisible == 0,
                    "Only right hand rises during interaction")) yield break;
                checkedReach = true;
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(reachFolder, "frame-" + reachShot.ToString("D4") + ".png"));
            reachTimes.Add(t.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            reachShot++;
        }
        File.WriteAllLines(Path.Combine(reachFolder, "times.txt"), reachTimes);
        if (!Check(checkedReach && !hands.IsReaching && !hands.IsVisible, "Reach returns below frame without leaving a hand stuck")) yield break;
        if (!Check(target.count == 1, "Delayed interaction calls target exactly once")) yield break;
        var modal = new ReachTarget();
        bool visibleBeforeModal = false;
        modal.onInteract = () =>
        {
            visibleBeforeModal = hands.IsVisible;
            GameManager.Instance.AcquireInput(this);
        };
        targetField.SetValue(interaction, modal);
        interaction.TryInteract();
        yield return new WaitForSeconds(.12f);
        if (!Check(modal.count == 0 && hands.IsVisible, "Hand is visible before dialogue opens")) yield break;
        yield return new WaitForSeconds(.20f);
        if (!Check(modal.count == 1 && visibleBeforeModal && !hands.IsVisible,
            "Dialogue opens after reaching and then hides the hand")) yield break;
        GameManager.Instance.ReleaseInput(this);
        Time.captureFramerate = oldCaptureRate;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Walking Hands Test Floor";
        floor.transform.position = new Vector3(0f, 999.95f, 0f);
        floor.transform.localScale = new Vector3(40f, .1f, 40f);
        floor.GetComponent<Renderer>().enabled = false;
        player.TeleportTo(new Vector3(0f, 1000.02f, 0f), Quaternion.identity);
        Physics.SyncTransforms();
        yield return null;
        foreach (float aspect in new[] { 16f / 9f, 21f / 9f })
        {
            view.aspect = aspect;
            for (float t = 0f; t < .4f; t += Time.deltaTime)
            {
                player.SimulateMovementForTests(Vector2.down, false, Time.deltaTime);
                yield return null;
            }
            // Both arms intentionally leave view at the exchange between steps.
            hands.SampleStrideForTests(.25f);
            if (!Check(ValidateFraming(.09f), "Framing remains clear at aspect " + aspect)) yield break;
            hands.SampleStrideForTests(.75f);
            if (!Check(ValidateFraming(.09f), "Opposite hand framing at aspect " + aspect)) yield break;
        }
        view.ResetAspect();
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Walking Hands Test Wall";
        wall.transform.SetPositionAndRotation(player.transform.position + player.transform.forward * .55f + Vector3.up,
            player.transform.rotation);
        wall.transform.localScale = new Vector3(4f, 3f, .15f);
        Physics.SyncTransforms();
        float wallPhase = 0f;
        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            player.SimulateMovementForTests(Vector2.up, false, Time.deltaTime);
            if (t < .9f) wallPhase = hands.StridePhase;
            yield return null;
        }
        if (!Check(!hands.IsVisible && Mathf.Abs(hands.StridePhase - wallPhase) < .01f,
            "Pushing into a wall stops the hand stride")) yield break;
        Destroy(wall);
        GameManager.Instance.AcquireInput(this);
        yield return null;
        yield return new WaitForEndOfFrame();
        if (!Check(!hands.IsVisible, "Modal/task input lock hides walking hands")) yield break;
        GameManager.Instance.ReleaseInput(this);
        GameManager.Instance.SetPaused(true);
        yield return new WaitForSecondsRealtime(.1f);
        if (!Check(!hands.IsVisible, "Pause hides walking hands")) yield break;
        GameManager.Instance.SetPaused(false);
        if (!Check(Mathf.Approximately(view.nearClipPlane, nearClip), "Walking hands preserve camera near clip")) yield break;
        Debug.Log("[WalkingHandsSmoke] PASS");
        EditorApplication.isPlaying = false;
    }

    private sealed class ReachTarget : IInteractable
    {
        public int count;
        public System.Action onInteract;
        public string GetPromptText() => "Reach test";
        public void Interact() { count++; onInteract?.Invoke(); }
    }

    private bool ValidateFraming(float minimumHeight = 0f, float maximumHeight = .30f)
    {
        var renderer = hands.GetComponentInChildren<SkinnedMeshRenderer>();
        var mesh = new Mesh();
        renderer.BakeMesh(mesh);
        int visible = 0, outsideCorners = 0;
        leftVisible = rightVisible = 0;
        float highest = 0f;
        foreach (var vertex in mesh.vertices)
        {
            Vector3 point = view.WorldToViewportPoint(renderer.transform.TransformPoint(vertex));
            if (point.z <= view.nearClipPlane || point.x < 0f || point.x > 1f || point.y < 0f || point.y > 1f) continue;
            visible++;
            if (point.x < .5f) leftVisible++; else rightVisible++;
            highest = Mathf.Max(highest, point.y);
            if (point.y > maximumHeight || (point.x > .32f && point.x < .68f)) outsideCorners++;
        }
        Destroy(mesh);
        Debug.Log($"[WalkingHandsSmoke] Framing: {visible} visible vertices; highest={highest:F3}; outside corners={outsideCorners}");
        return visible > 0 && highest >= minimumHeight && outsideCorners == 0;
    }

    private static bool Check(bool valid, string message)
    {
        if (valid) { Debug.Log("[WalkingHandsSmoke] OK: " + message); return true; }
        Debug.LogError("[WalkingHandsSmoke] FAIL: " + message);
        EditorApplication.isPlaying = false;
        return false;
    }

    private void OnDestroy()
    {
        Application.runInBackground = oldBackground;
        Time.captureFramerate = oldCaptureRate;
    }
}
#endif
