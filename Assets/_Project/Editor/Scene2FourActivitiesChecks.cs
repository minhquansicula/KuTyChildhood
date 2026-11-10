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
        Check(!Field<GameObject>(HUDController.Instance, "memoryCollectionCard").activeSelf,
            "Home task UI does not show the unrelated three chapter-token counter");
        var hud=flow.GetComponent<Scene2QuestHUD>();
        string board=(string)typeof(Scene2QuestHUD).GetMethod("BuildBoardText",Flags).Invoke(hud,null);
        Check(board.Contains("/5")&&!board.Contains("LÀM THÊM"),"Task board has five tasks and no optional task block");
        foreach(var chore in HouseholdChoreFlow.Order)Check(board.Contains(HouseholdChoreFlow.Titles[(int)chore]),"Board includes active task: "+chore);
        foreach(HouseholdChore chore in Enum.GetValues(typeof(HouseholdChore)))
            if(!Scene2HouseholdActivityPolicy.IsActive(chore))Check(!board.Contains(HouseholdChoreFlow.Titles[(int)chore]),"Removed task absent from visible board: "+chore);
        Field<GameObject>(hud,"board").SetActive(true);
        ValidateText(Field<TMPro.TextMeshProUGUI>(hud, "boardText"), "Five-task board");
        ValidateText(Field<TMPro.TextMeshProUGUI>(hud, "objectiveText"), "Current objective");
        ScreenCapture.CaptureScreenshot(Scene2FiveTaskSetup.Output+"/five-task-board.png");yield return new WaitForEndOfFrame();yield return null;
        Field<GameObject>(hud,"board").SetActive(false);
        var letter=FindObjectOfType<Scene2TabletopLetter>();
        Check(letter&&letter.Table&&letter.Table.name=="bandai_khach","Mother letter follows the existing indoor living-room table, not the outside mesh");
        var letterBounds=Scene2DishwashingLayout.VisualBounds(letter.transform);Physics.SyncTransforms();
        Check(letter.Table.Raycast(new Ray(letterBounds.center+Vector3.up,Vector3.down),out var letterSupport,3f)&&letterBounds.min.y-letterSupport.point.y>=-.001f&&letterBounds.min.y-letterSupport.point.y<=.025f,
            "Original letter is supported by the real indoor tabletop");
        var expected = new[] { HouseholdChore.LightFire, HouseholdChore.CookRice, HouseholdChore.WashDishes, HouseholdChore.SetTable };
        foreach (HouseholdChore chore in Enum.GetValues(typeof(HouseholdChore)))
        {
            Check(Scene2HouseholdActivityPolicy.IsMinigame(chore) == expected.Contains(chore), "Activity policy: " + chore);
            bool handled = simulator.Handles(chore) || kitchen.Handles(chore) || radio.Handles(chore);
            Check(handled == expected.Contains(chore), "Only retained activities have a physical handler: " + chore);
        }
        Check(HouseholdChoreFlow.Order.SequenceEqual(new[] { HouseholdChore.LightFire, HouseholdChore.CookRice, HouseholdChore.SetTable, HouseholdChore.WashDishes, HouseholdChore.ReadLetter }) && HouseholdChoreFlow.TotalPieces == 50, "Exactly four physical tasks plus mother letter; five rewards, stable enum IDs");
        Check(!simulator.Begin(HouseholdChore.FetchWater, null, null), "Direct calls cannot reopen removed transfer minigame");
        foreach (var removed in Enum.GetValues(typeof(HouseholdChore)).Cast<HouseholdChore>().Where(c => !Scene2HouseholdActivityPolicy.IsActive(c)))
        {
            Check(!flow.IsCurrent(removed) && !flow.Complete(removed, false), "Removed task cannot become current or award progress: " + removed);
            foreach (var target in FindObjectsOfType<HouseholdChoreInteractable>(true).Where(t => t.Chore == removed))
                Check(!target.enabled && string.IsNullOrEmpty(target.GetPromptText()), "Removed task has no active prompt: " + removed);
        }
        Check(!radio.IsTuning && !radio.CarryingWood, "Removed radio/wood tasks cannot capture input");
        kitchen.Begin(HouseholdChore.WashRice);
        Check(!kitchen.IsWorking, "Direct API also rejects the removed rice-washing gesture game");
        Check(flow.IsCurrent(HouseholdChore.LightFire), "Story reaches retained wood placement normally");
        Target(HouseholdChore.LightFire).Interact();
        Check(kitchen.IsWorking && !flow.UI.SimpleActionVisible, "Place wood/ignite remains a physical activity");
        yield return new WaitForSeconds(.5f);
        CaptureView("WoodPlacement-Layout.png");
        ValidateText(Field<TMPro.TextMeshProUGUI>(kitchen, "controlText"), "Fire controls");
        ScreenCapture.CaptureScreenshot(Scene2FourActivitiesSetup.Output + "/Fire-UI.png");
        yield return new WaitForEndOfFrame(); yield return null;
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
        ValidateText(Field<TMPro.TextMeshProUGUI>(kitchen, "controlText"), "Cooking controls");
        ScreenCapture.CaptureScreenshot(Scene2FourActivitiesSetup.Output + "/Cooking-UI.png");
        yield return new WaitForEndOfFrame(); yield return null;
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
        Vector3 beforeTablePosition = view.transform.position;
        Quaternion beforeTableRotation = view.transform.rotation;
        float beforeTableFov = view.fieldOfView;
        Check(simulator.Begin(HouseholdChore.SetTable, () => flow.Complete(HouseholdChore.SetTable, false), () => { }),
            "Setting the table retains its world-space mini-game");
        yield return new WaitForSeconds(.5f);
        CaptureView("SetTable-Layout.png");
        foreach (string label in new[] { "title", "instructions", "hint" })
            ValidateText(Field<TMPro.TextMeshProUGUI>(simulator, label), "Table " + label);
        ScreenCapture.CaptureScreenshot(Scene2FourActivitiesSetup.Output + "/Table-UI.png");
        yield return new WaitForEndOfFrame(); yield return null;
        var world = Field<Transform>(simulator, "world"); var pieces = Field<Transform[]>(simulator, "pieces");
        var slots = Field<Vector3[]>(simulator, "slots");
        var tableLayout = simulator.GetComponent<Scene2AuthoredTableLayout>();
        Vector3 tableCameraPosition = view.transform.position;
        float tableCameraFov = view.fieldOfView;
        if (tableLayout && tableLayout.IsReady)
        {
            Check(!world.GetComponentsInChildren<Transform>().Any(t => t.name == "Work cloth" || t.name == "Workstand leg" || t.name == "Chopstick" || t.name == "Rice serving pot"),
                "Authored table creates no replacement workstand, primitive chopsticks or generated serving pot");
            foreach (var mesh in pieces.SelectMany(p => p.GetComponentsInChildren<MeshFilter>()))
                Check(tableLayout.dishes.SelectMany(d => d.GetComponentsInChildren<MeshFilter>(true)).Any(m => m.sharedMesh == mesh.sharedMesh),
                    "Table gameplay reuses an exact existing source mesh: " + mesh.name);
            for (int i = 0; i < pieces.Length; i++)
            {
                var bounds = Scene2DishwashingLayout.VisualBounds(pieces[i]);
                Check(bounds.min.y >= tableLayout.table.bounds.max.y && bounds.min.y - tableLayout.table.bounds.max.y < .015f,
                    "Existing dish is supported by the actual tabletop: " + i);
                Check(bounds.min.x > tableLayout.table.bounds.min.x && bounds.max.x < tableLayout.table.bounds.max.x &&
                    bounds.min.z > tableLayout.table.bounds.min.z && bounds.max.z < tableLayout.table.bounds.max.z,
                    "Existing dish footprint fits on the actual tabletop: " + i);
            }
            Pointer(pieces[0].GetComponent<BoxCollider>().bounds.center, true, false, false);
            Vector3 wrong = world.TransformPoint(new Vector3(-.4f, .12f, -.55f));
            Pointer(wrong, false, true, false); Pointer(wrong, false, false, true);
            Check(!Field<bool>(simulator, "tableDropReady"), "Invalid release clears ready-to-drop feedback");
            Check(simulator.State.placedMask == 0, "Dropping an authored dish away from its setting does not grant progress");
            Check(pieces[0].localPosition == tableLayout.sourceLocal[0], "Invalid drop returns dish to its supported source position");
            Pointer(pieces[0].GetComponent<BoxCollider>().bounds.center, true, false, false);
            simulator.ProcessPointer(new Vector2(-10, Screen.height / 2f), false, false, true, false, 0, .02f);
            Check(simulator.State.placedMask == 0 && Field<int>(simulator, "held") == -1 && pieces[0].localPosition == tableLayout.sourceLocal[0],
                "Release outside viewport safely returns dish without awarding placement");

            Pointer(pieces[0].GetComponent<BoxCollider>().bounds.center, true, false, false);
            Pointer(pieces[0].GetComponent<BoxCollider>().bounds.center, false, false, true);
            Check(Field<int>(simulator, "held") == 0 && simulator.State.placedMask == 0, "Short click keeps dish held without placing on its mouse-up");
            var previews = Field<Transform[]>(simulator, "tablePreviews");
            Check(previews[0].gameObject.activeSelf && !previews[0].GetComponentsInChildren<Collider>().Any(), "Selected dish has a mesh-pose preview with no picking colliders");
            CaptureView("Table-ClickHeld-Preview.png");

            // Real GraphicRaycaster path, not an injected canPlace flag.
            var blocker = new GameObject("Test activity UI blocker", typeof(Canvas), typeof(GraphicRaycaster));
            blocker.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var image = new GameObject("Blocking button", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(blocker.transform, false);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Check(EventSystem.current != null && rect.rect.width > 0, "UI blocker fixture has a live EventSystem and laid-out graphic");
            Vector2 validScreen = view.WorldToScreenPoint(world.TransformPoint(slots[0] + Vector3.up * .12f));
            simulator.ProcessPointer(validScreen, true, true, false, false, 0, .02f);
            Check(Field<int>(simulator, "held") == -1 && simulator.State.placedMask == 0, "UI click cancels carry and never places through the UI");
            blocker.SetActive(false); Destroy(blocker);
            simulator.ProcessPointer(validScreen, false, false, true, false, 0, .02f);
            Check(simulator.State.placedMask == 0, "Mouse-up after UI click is consumed");

            Pointer(pieces[0].GetComponent<BoxCollider>().bounds.center, true, false, false);
            GameManager.Instance.SetPaused(true); GameManager.Instance.SetPaused(false);
            simulator.ProcessPointer(validScreen, false, false, true, false, 0, .02f);
            Check(Field<int>(simulator, "held") == -1 && simulator.State.placedMask == 0, "Pause/resume cannot turn an old gesture into a placement");
            Pointer(pieces[0].GetComponent<BoxCollider>().bounds.center, true, false, false);
            Call(simulator, "OnApplicationFocus", false);
            simulator.ProcessPointer(validScreen, false, false, true, false, 0, .02f);
            Check(Field<int>(simulator, "held") == -1 && simulator.State.placedMask == 0, "Focus loss returns carried dish and consumes stale release");
            Pointer(pieces[0].GetComponent<BoxCollider>().bounds.center, true, false, false);
            Pointer(pieces[0].GetComponent<BoxCollider>().bounds.center, false, false, true);
            simulator.ProcessPointer(validScreen, false, false, false, true, 0, .02f);
            Check(Field<int>(simulator, "held") == -1 && pieces[0].localPosition == tableLayout.sourceLocal[0], "Right click returns a click-carried dish to its safe source pose");
        }
        for (int i = 0; i < 3; i++)
        {
            Physics.SyncTransforms();
            Pointer(pieces[i].GetComponent<BoxCollider>().bounds.center, true, false, false);
            Check(Field<int>(simulator, "held") == i, "Table item is picked by ray: " + i);
            var guides = Field<LineRenderer[]>(simulator, "tableGuides");
            if (tableLayout && tableLayout.IsReady)
                Check(guides[i] && guides[i].enabled && guides[i].widthMultiplier <= .003f, "Picked dish shows only its own quiet placement guide: " + i);
            Vector3 to = world.TransformPoint(slots[i] + Vector3.up * .12f);
            if (i == 0)
                Pointer(pieces[i].GetComponent<BoxCollider>().bounds.center, false, false, true);
            Pointer(to, false, true, false);
            Check(Field<bool>(simulator, "tableDropReady"), "Drop feedback agrees with valid placement radius: " + i);
            if (i == 0)
            {
                Vector3 offset = Field<Vector3>(simulator, "tableGrabOffset");
                Pointer(world.TransformPoint(slots[0] - offset + new Vector3(.17f, .12f, 0)), false, false, false);
                Check(Field<bool>(simulator, "tableDropReady"), "Hysteresis retains a valid target at the edge without flicker");
                Pointer(world.TransformPoint(slots[0] - offset + new Vector3(.21f, .12f, 0)), false, false, false);
                Check(!Field<bool>(simulator, "tableDropReady"), "Leaving the target clears green feedback");
                Pointer(to, false, false, false);
                var target = world.TransformPoint(slots[0] + Vector3.up * .10f);
                var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = "Temporary placement occluder";
                obstacle.transform.position = Vector3.Lerp(view.transform.position, target, .5f);
                obstacle.transform.localScale = Vector3.one * .3f;
                Physics.SyncTransforms(); Pointer(to, false, false, false);
                Check(!Field<bool>(simulator, "tableDropReady") && simulator.State.placedMask == 0, "A solid occluder blocks preview and placement instead of snapping through it");
                obstacle.SetActive(false); Destroy(obstacle); Physics.SyncTransforms();
                Pointer(to, false, false, false);
                Pointer(to, true, true, false);
                CaptureView("Table-Valid-Preview.png");
                Call(simulator, "UpdateCaption");
                ValidateText(Field<TMPro.TextMeshProUGUI>(simulator, "instructions"), "Table held action");
            }
            Pointer(to, false, false, true);
            Check((simulator.State.placedMask & (1 << i)) == 0, "Placement waits for controlled settling: " + i);
            // Repeated edges during settling must not pick/commit twice.
            Pointer(to, true, true, false);
            // Editor may be unfocused. Tick the shared path explicitly rather than defeating focus safety.
            simulator.ProcessPointer(view.WorldToScreenPoint(to), false, false, false, false, 0, .2f);
            if (tableLayout && tableLayout.IsReady)
                Check(!guides[i].enabled, "Completed placement hides its guide: " + i);
            if (i == 0)
            {
                Check(simulator.State.placedMask == 1 && flow.Pieces == 20, "Partial placement changes only item state, not task reward");
                simulator.End();
                Check(Vector3.Distance(view.transform.position, beforeTablePosition) < .001f && Quaternion.Angle(view.transform.rotation, beforeTableRotation) < .01f && Mathf.Abs(view.fieldOfView - beforeTableFov) < .01f, "Leaving partial table work restores original camera pose and FOV");
                Check(Scene2Checkpoint.LoadWork(HouseholdChore.SetTable).Contains("\"placedMask\":1"), "Partial table checkpoint retains original item-to-slot mapping");
                Field<System.Collections.IDictionary>(simulator, "work").Clear();
                Check(simulator.Begin(HouseholdChore.SetTable, () => flow.Complete(HouseholdChore.SetTable, false), () => { }), "Table can reopen after partial work");
                yield return new WaitForSeconds(.5f);
                world = Field<Transform>(simulator, "world"); pieces = Field<Transform[]>(simulator, "pieces"); slots = Field<Vector3[]>(simulator, "slots");
                Check(pieces[0].localPosition == slots[0] && simulator.State.placedMask == 1, "Reopened table restores placed dish pose without duplicating reward");
                Check(Vector3.Distance(view.transform.position, tableCameraPosition) < .001f && Mathf.Abs(view.fieldOfView - tableCameraFov) < .01f, "Reopened table uses the same activity camera and FOV");
            }
        }
        Check(simulator.State.Complete, "All three real table placements complete the table activity");
        if (tableLayout && tableLayout.IsReady)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                var bounds = Scene2DishwashingLayout.VisualBounds(pieces[i]);
                Check(bounds.min.y >= tableLayout.table.bounds.max.y && bounds.min.y - tableLayout.table.bounds.max.y < .015f,
                    "Completed dish remains supported by actual dining furniture: " + i);
            }
        }
        yield return new WaitForSeconds(1f);
        Check(flow.IsCurrent(HouseholdChore.WashDishes) && !simulator.IsWorking, "Table completion unlocks washing without input leaks");
        Check(flow.Pieces == 30 && !flow.Complete(HouseholdChore.SetTable, false), "Completed table awards once and refuses a duplicate reward");
        if (tableLayout && tableLayout.useFoodAssets)
        {
            Check(tableLayout.dishes.All(t => !t.gameObject.activeSelf), "Completed Dat meal has only the retained task visuals, not duplicate source models");
            var washingLayout = simulator.GetComponent<Scene2DishwashingLayout>();
            Check(washingLayout.dishes.All(d => d.source.gameObject.activeSelf), "Dat meal completion leaves every original washing source available");
            simulator.ResetWork();
            Check(tableLayout.dishes.All(t => t.gameObject.activeSelf), "Reset restores Dat meal sources independently of washing props");
        }
        // The separate 77-assertion suite exercises all dishwashing rays and lifecycle; do not duplicate it here.
        Check(flow.Complete(HouseholdChore.WashDishes, false), "Advance washing in this fixture only; dedicated suite covers the retained game");
        Check(flow.IsCurrent(HouseholdChore.ReadLetter) && flow.Pieces == 40, "Washing unlocks mother letter directly, with no removed cupboard task");
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
        Check(Scene2Checkpoint.HomeChores == 4, "Test leaves letter uncompleted after four physical tasks; no outside-scene transition or checkpoint reset");
    }
    private void Pointer(Vector3 position, bool down, bool pressed, bool up)
    {
        Vector3 screen = view.WorldToScreenPoint(position);
        Check(screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height, "Placement target is inside the real viewport");
        simulator.ProcessPointer(screen, down, pressed, up, false, 0, .02f);
    }
    private void ValidateText(TMPro.TextMeshProUGUI label, string name)
    {
        Canvas.ForceUpdateCanvases(); label.ForceMeshUpdate();
        Check(label.preferredHeight <= label.rectTransform.rect.height + 1f && !label.isTextOverflowing,
            name + " fits its panel without clipping");
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
