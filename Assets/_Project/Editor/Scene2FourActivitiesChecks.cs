using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Scene2FourActivitiesChecks
{
    private const string Pending = "KuTy.FourActivitiesChecks.Pending";
    static Scene2FourActivitiesChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            EditorApplication.delayCall += () => new GameObject("Four activities checks").AddComponent<Scene2FourActivitiesCheckRunner>();
        };
    }
    [MenuItem("KuTy/Tests/Check four activities and supported cooking lid")]
    public static void Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity")
            throw new InvalidOperationException("Open saved Home in Edit Mode first.");
        var kitchen = UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>();
        if (!kitchen || !kitchen.lidRestPoint) throw new InvalidOperationException("Bind the supported lid rest first.");
        Directory.CreateDirectory(Scene2FourActivitiesSetup.Output);
        KuTyCheckpointTestScope.Capture();
        SessionState.SetBool(Pending, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.EnterPlaymode();
    }
}

public sealed class Scene2FourActivitiesCheckRunner : MonoBehaviour
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly StringBuilder report = new StringBuilder();
    private HouseholdChoreFlow flow;
    private Scene2HouseholdSimulator simulator;
    private Scene2KitchenHandsOn kitchen;
    private Scene2RadioKitchenSequence radio;
    private Camera view;
    private bool finished, background;
    private float started;
    private int assertions;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);
    private static void Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Flags).Invoke(owner, args);
    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        assertions++; report.AppendLine("PASS " + message);
    }
    private void Start()
    {
        background = Application.runInBackground; Application.runInBackground = true;
        started = Time.realtimeSinceStartup; Application.logMessageReceived += Log; StartCoroutine(Guarded());
    }
    private void Update() { if (!finished && Time.realtimeSinceStartup - started > 100f) Finish(false, "Timed out"); }
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
            try { if (!run.MoveNext()) { Finish(true, "Simple UI routes, retained kitchen/table activities and supported lid passed."); yield break; } next = run.Current; }
            catch (Exception error) { Finish(false, error.ToString()); yield break; }
            yield return next;
        }
    }
    private HouseholdChoreInteractable Target(HouseholdChore chore) => FindObjectsOfType<HouseholdChoreInteractable>()
        .First(t => t.Chore == chore && t.isActiveAndEnabled && !string.IsNullOrEmpty(t.GetPromptText()));
    private void ClickConfirm()
    {
        var button = Field<Button[]>(flow.UI, "choiceButtons")[0];
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    private void ClickCancel()
    {
        var button = Field<Button>(flow.UI, "leaveButton");
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    private IEnumerator Run()
    {
        yield return null;
        flow = HouseholdChoreFlow.Instance; simulator = flow.GetComponent<Scene2HouseholdSimulator>();
        kitchen = flow.GetComponent<Scene2KitchenHandsOn>(); radio = flow.GetComponent<Scene2RadioKitchenSequence>(); view = Camera.main;
        Call(FindObjectOfType<BedWakeUpController>(), "GetOutOfBed");
        yield return new WaitForSeconds(1.5f);
        FindObjectOfType<FirstPersonController>().enabled = false;
        flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue();
        Check(flow.ChecklistVisible && !GameManager.Instance.InputBlocked, "Wake-up and story control still work");
        var expected = new[] { HouseholdChore.LightFire, HouseholdChore.CookRice, HouseholdChore.WashDishes, HouseholdChore.SetTable };
        foreach (HouseholdChore chore in Enum.GetValues(typeof(HouseholdChore)))
        {
            Check(Scene2HouseholdActivityPolicy.IsMinigame(chore) == expected.Contains(chore), "Activity policy: " + chore);
            bool handled = simulator.Handles(chore) || kitchen.Handles(chore) || radio.Handles(chore);
            Check(handled == expected.Contains(chore), "Only retained activities have a physical handler: " + chore);
        }
        Check(HouseholdChoreFlow.Order.Length == 12 && HouseholdChoreFlow.TotalPieces == 130, "Story task count, serialized IDs and reward total stay compatible");
        Check(!simulator.Begin(HouseholdChore.FetchWater, null, null), "Direct calls cannot reopen removed transfer minigame");
        foreach (var optional in new[] { HouseholdChore.SortTools, HouseholdChore.GrindCorn })
        {
            int before = flow.Pieces; Target(optional).Interact(); yield return null;
            Check(flow.UI.SimpleActionVisible, "Optional task is a simple confirmation: " + optional);
            ClickConfirm();
            Check(flow.Pieces == before + 5 && flow.ActiveIndex == 0, "Optional UI preserves optional reward/order: " + optional);
            flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue();
        }
        foreach (var chore in HouseholdChoreFlow.Order.Take(6))
        {
            int before = flow.Pieces, index = flow.ActiveIndex;
            Vector3 cameraHome = view.transform.localPosition; Quaternion rotation = view.transform.localRotation;
            var target = Target(chore); target.Interact();
            Check(flow.UI.SimpleActionVisible && !simulator.IsWorking && !kitchen.IsWorking && !radio.IsTuning && !radio.CarryingWood,
                "E routes to simple UI, not a legacy game: " + chore);
            Check(!Field<GameObject>(flow.UI, "meter").activeSelf && !Field<GameObject>(flow.UI, "progressBar").activeSelf,
                "No timing/progress mini-game UI: " + chore);
            flow.UI.ConfirmSimpleAction();
            Check(flow.ActiveIndex == index, "Opening key cannot also confirm in the same frame: " + chore);
            yield return null;
            GameManager.Instance.SetPaused(true); ClickConfirm();
            Check(flow.ActiveIndex == index && flow.Pieces == before, "Pause blocks UI completion: " + chore);
            GameManager.Instance.SetPaused(false); yield return null;
            ClickCancel();
            Check(!GameManager.Instance.InputBlocked && flow.ActiveIndex == index && flow.Pieces == before,
                "Cancel releases input without reward: " + chore);
            target.Interact(); yield return null;
            if (chore == HouseholdChore.FetchWater)
            {
                ScreenCapture.CaptureScreenshot(Scene2FourActivitiesSetup.Output + "/SimpleWater-UI.png");
                yield return new WaitForEndOfFrame(); yield return null;
            }
            ClickConfirm(); ClickConfirm();
            Check(flow.ActiveIndex == index + 1 && flow.Pieces == before + 10 && !flow.UI.SimpleActionVisible,
                "Confirmation completes once only: " + chore);
            Check(Vector3.Distance(cameraHome, view.transform.localPosition) < .00001f && Quaternion.Angle(rotation, view.transform.localRotation) < .001f,
                "Simple UI does not teleport camera: " + chore);
            Check(Scene2Checkpoint.HomeChores == flow.ActiveIndex, "Existing checkpoint records confirmed task: " + chore);
            flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue();
            yield return null;
        }
        Check(radio.RadioOn && !radio.IsTuning && !radio.CarryingWood && radio.woodBundle.parent == radio.woodRest,
            "Radio and delivered wood visuals still reflect simple confirmations");
        kitchen.Begin(HouseholdChore.WashRice);
        Check(!kitchen.IsWorking, "Direct API also rejects the removed rice-washing gesture game");
        Check(flow.IsCurrent(HouseholdChore.LightFire), "Story reaches retained wood placement normally");
        Target(HouseholdChore.LightFire).Interact();
        Check(kitchen.IsWorking && !flow.UI.SimpleActionVisible, "Place wood/ignite remains a physical activity");
        yield return new WaitForSeconds(.5f);
        CaptureView("WoodPlacement-Layout.png");
        Call(kitchen, "Blow", .55f);
        Check(!kitchen.FireLit, "Fire cannot finish before placing the three logs");
        for (int i = 0; i < 3; i++) Call(kitchen, "PlaceKindling", i);
        Check(kitchen.KindlingCount == 3, "All three wood placements remain required");
        for (int i = 0; i < 3; i++) Call(kitchen, "Blow", .55f);
        Check(kitchen.FireLit && flow.IsCurrent(HouseholdChore.CookRice) && !GameManager.Instance.InputBlocked,
            "Ignition completes to the cooking stage and releases control");
        kitchen.SilenceMother();
        Target(HouseholdChore.CookRice).Interact(); yield return new WaitForSeconds(.5f);
        Check(kitchen.IsWorking && !flow.UI.SimpleActionVisible, "Rice cooking remains hands-on");
        Call(kitchen, "Simmer", 2f);
        Check(kitchen.Cooked == 0, "Excessive heat still does not cook rice");
        Call(kitchen, "AdjustFuel", -.38f);
        for (int i = 0; i < 80; i++) Call(kitchen, "Simmer", .25f);
        Check(kitchen.Cooked >= 12, "Adjusting real fuel allows the gentle simmer to cook rice");
        // Compare with the authored home, not the last frame's simmer vibration.
        Vector3 closed = kitchen.lid.parent.TransformPoint(Field<Vector3>(kitchen, "lidHome"));
        Call(kitchen, "LiftLid", .5f);
        Check(Vector3.Distance(kitchen.lid.position, closed + Vector3.up * .11f) < .001f, "Lid lift uses world metres despite imported hierarchy scale");
        GameManager.Instance.SetPaused(true);
        Check(Vector3.Distance(kitchen.lid.position, closed) < .0001f && flow.IsCurrent(HouseholdChore.CookRice),
            "Pause safely closes a half-lifted lid without finishing the task");
        GameManager.Instance.SetPaused(false); yield return null;
        kitchen.End();
        Check(Vector3.Distance(kitchen.lid.position, closed) < .0001f && !GameManager.Instance.InputBlocked,
            "Cancelling the lid check returns it safely to the pot");
        kitchen.Begin(HouseholdChore.CookRice); yield return new WaitForSeconds(.5f);
        Call(kitchen, "LiftLid", 1f);
        Check(flow.IsCurrent(HouseholdChore.SetTable) && !kitchen.IsWorking, "Lifting the lid completes cooking normally");
        Check(Vector3.Distance(kitchen.lid.position, kitchen.lidRestPoint.position) < .0001f,
            "Completed lid is placed at supported marker immediately, not left at a raised offset");
        ValidateLid(); CaptureLid("Lid-Supported-AfterCooking.png");
        Call(kitchen, "RestoreWorld");
        Check(Vector3.Distance(kitchen.lid.position, kitchen.lidRestPoint.position) < .0001f,
            "Loading completed kitchen state reconstructs the supported lid pose");
        ValidateLid();
        Check(simulator.Begin(HouseholdChore.SetTable, () => flow.Complete(HouseholdChore.SetTable, false), () => { }),
            "Setting the table retains its world-space mini-game");
        yield return new WaitForSeconds(.5f);
        CaptureView("SetTable-Layout.png");
        var world = Field<Transform>(simulator, "world"); var pieces = Field<Transform[]>(simulator, "pieces");
        var slots = Field<Vector3[]>(simulator, "slots");
        for (int i = 0; i < 3; i++)
        {
            Physics.SyncTransforms();
            Pointer(pieces[i].GetComponent<BoxCollider>().bounds.center, true, false, false);
            Check(Field<int>(simulator, "held") == i, "Table item is picked by ray: " + i);
            Vector3 to = world.TransformPoint(slots[i] + Vector3.up * .12f);
            Pointer(to, false, true, false); Pointer(to, false, false, true);
        }
        Check(simulator.State.Complete, "All three real table placements complete the table activity");
        yield return null;
        Check(flow.IsCurrent(HouseholdChore.WashDishes) && !simulator.IsWorking, "Table completion unlocks washing without input leaks");
        // The separate 77-assertion suite exercises all dishwashing rays and lifecycle; do not duplicate it here.
        Check(flow.Complete(HouseholdChore.WashDishes, false), "Advance washing in this fixture only; dedicated suite covers the retained game");
        Target(HouseholdChore.PutAwayDishes).Interact(); yield return null;
        Check(flow.UI.SimpleActionVisible && !simulator.IsWorking, "Putting dishes away is now a simple UI interaction");
        ClickConfirm();
        Check(flow.IsCurrent(HouseholdChore.ReadLetter), "The story and letter remain after the retained mini-games");
        flow.Voice.Stop(); DialogueUI.Instance?.HideDialogue();
        Target(HouseholdChore.ReadLetter).Interact();
        Check(flow.UI.SimpleActionVisible, "Letter opens as reading UI rather than a paper-fold puzzle");
        yield return null; ClickConfirm();
        Check(!flow.IsCompleted(HouseholdChore.ReadLetter), "Letter cannot be skipped immediately");
        GameManager.Instance.SetPaused(true);
        float remaining = Field<float>(flow.UI, "simpleReadRemaining");
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Approximately(remaining, Field<float>(flow.UI, "simpleReadRemaining")), "Reading delay does not run while paused");
        GameManager.Instance.SetPaused(false); yield return null; ClickCancel();
        Check(!GameManager.Instance.InputBlocked && !flow.IsCompleted(HouseholdChore.ReadLetter), "Leaving the letter does not complete the story");
        // Exercise the read-complete button without invoking a scene transition in the test fixture.
        bool read = false;
        GameManager.Instance.AcquireInput(this);
        flow.UI.ShowSimpleAction(HouseholdChore.ReadLetter, () => read = true, () => { });
        yield return new WaitForSeconds(4.2f); ClickConfirm();
        Check(read, "Read-complete button becomes available after deliberate reading time");
        flow.UI.HideGame(); GameManager.Instance.ReleaseInput(this);
        Check(Scene2Checkpoint.HomeChores == 11, "Test leaves letter uncompleted; no outside-scene transition or checkpoint reset");
    }
    private void Pointer(Vector3 position, bool down, bool pressed, bool up)
    {
        Vector3 screen = view.WorldToScreenPoint(position);
        Check(screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height, "Placement target is inside the real viewport");
        simulator.ProcessPointer(screen, down, pressed, up, false, 0, .02f);
    }
    private void ValidateLid()
    {
        Physics.SyncTransforms();
        Bounds bounds = Scene2DishwashingLayout.VisualBounds(kitchen.lid);
        var hits = Physics.RaycastAll(bounds.center + Vector3.up, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore)
            .Where(h => !h.transform.IsChildOf(kitchen.cookRoot) && h.normal.y > .95f && h.point.y <= bounds.min.y).OrderBy(h => h.distance).ToArray();
        Check(hits.Length > 0, "A real solid surface exists below the rested lid");
        float gap = bounds.min.y - hits[0].point.y;
        Check(gap >= -.001f && gap <= .006f, "Lid-bottom support gap <= 6mm: " + gap.ToString("F6"));
    }
    private void CaptureView(string name)
    {
        var target = RenderTexture.GetTemporary(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        var previousTarget = view.targetTexture; var previous = RenderTexture.active; float aspect = view.aspect;
        try
        {
            view.targetTexture = target; view.aspect = 16f / 9f; view.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            File.WriteAllBytes(Scene2FourActivitiesSetup.Output + "/" + name, image.EncodeToPNG());
        }
        finally { view.targetTexture = previousTarget; view.aspect = aspect; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Destroy(image); }
    }
    private void CaptureLid(string name)
    {
        var go = new GameObject("Temporary supported lid review") { hideFlags = HideFlags.HideAndDontSave };
        var camera = go.AddComponent<Camera>();
        Vector3 focus = Vector3.Lerp(kitchen.lid.position, kitchen.cookRoot.position, .5f) + Vector3.up * .3f;
        camera.transform.position = focus + new Vector3(1.8f, 1.2f, 1.8f); camera.transform.LookAt(focus);
        camera.nearClipPlane = .02f; camera.fieldOfView = 48;
        var rt = RenderTexture.GetTemporary(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            File.WriteAllBytes(Scene2FourActivitiesSetup.Output + "/" + name, image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); Destroy(image); Destroy(go); }
    }
    private void Finish(bool passed, string detail)
    {
        if (finished) return; finished = true; Application.logMessageReceived -= Log;
        if (simulator) simulator.End(); if (kitchen) kitchen.End();
        if (flow) flow.UI.HideGame(); GameManager.Instance?.ReleaseInput(this);
        Application.runInBackground = background;
        File.WriteAllText(Scene2FourActivitiesSetup.Output + "/checks.txt", (passed ? "PASS" : "FAIL") + " · " + assertions + " assertions\n" + report + "\n" + detail);
        Debug.Log("[FourActivitiesChecks] " + (passed ? "PASS " : "FAIL ") + assertions + " · " + detail);
        EditorApplication.isPlaying = false;
    }
}
