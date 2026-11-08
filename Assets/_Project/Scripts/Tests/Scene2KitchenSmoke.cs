#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class Scene2KitchenSmoke : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    readonly StringBuilder report = new StringBuilder();
    bool check, done, background;
    int assertions;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        string mode = SessionState.GetString("KuTy.KitchenHandsOn.Run", "");
        if (string.IsNullOrEmpty(mode)) return;
        SessionState.EraseString("KuTy.KitchenHandsOn.Run");
        new GameObject("Kitchen sequence check").AddComponent<Scene2KitchenSmoke>().check = mode == "check";
    }
    void Start()
    {
        background = Application.runInBackground; Application.runInBackground = true;
        if (check) Application.logMessageReceived += Log;
        StartCoroutine(Run());
    }
    void OnDestroy() { Application.runInBackground = background; Application.logMessageReceived -= Log; }
    void Log(string text, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, text + "\n" + stack); }
    void Assert(bool value, string message)
    { if (!value) { Finish(false, message); throw new InvalidOperationException(message); } assertions++; report.AppendLine("PASS " + message); }
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Private).Invoke(owner, args);

    IEnumerator Run()
    {
        yield return null;
        Call(FindObjectOfType<BedWakeUpController>(), "GetOutOfBed");
        // Wait for the actual exit lifecycle, not a frame-rate-dependent animation guess.
        float exitDeadline = Time.realtimeSinceStartup + 6f;
        while ((GameManager.Instance.InputBlocked || !HouseholdChoreFlow.Instance.ChecklistVisible) &&
            Time.realtimeSinceStartup < exitDeadline) yield return null;
        var kitchen = FindObjectOfType<Scene2KitchenHandsOn>(); var flow = HouseholdChoreFlow.Instance;
        var player = FindObjectOfType<FirstPersonController>(); var camera = Camera.main;
        flow.Voice.Stop();
        Vector3 feet = kitchen.washView.position - Vector3.up * camera.transform.localPosition.y;
        feet.y = player.transform.position.y;
        player.TeleportTo(feet, Quaternion.Euler(0, kitchen.washView.eulerAngles.y, 0));
        player.SetPitch(Mathf.DeltaAngle(0, kitchen.washView.eulerAngles.x));
        if (!check) yield break;
        player.enabled = false;
        Assert(flow.ActiveIndex == 5 && flow.IsCurrent(HouseholdChore.WashRice), "Preview starts at washing rice in the original chore order");
        Assert(kitchen.motherLines.Count == 15 && kitchen.motherLines.CollectValidationIssues(false).Count == 0,
            "All 15 contextual mother lines have valid authored text and unique IDs");
        Assert(kitchen.waterClip && kitchen.fireClip && kitchen.simmerClip, "All three local kitchen sound clips are bound");
        if (kitchen.importedKitchen)
        {
            Assert(kitchen.worldRoot.GetComponentsInChildren<MeshFilter>(true).All(m =>
                AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith("Assets/_Project/Art/Scene2/BlenderKitchen/")),
                "Every kitchen mesh comes from the delivered FBX assets rather than prototype primitives");
            Assert(kitchen.worldRoot.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).All(m =>
                m && m.shader.name == "Universal Render Pipeline/Lit"), "Imported materials all use the URP Lit shader");
            Bounds waterBounds = kitchen.waterRenderer.bounds;
            Assert(waterBounds.size.y < .01f && waterBounds.size.x > .1f && waterBounds.size.z > .1f,
                "Imported water lies horizontally after FBX axis conversion");
            Assert(kitchen.kindlingSlots.Length == 3 && kitchen.retainedColliders.All(c => c && c.enabled),
                "Authored wood destinations and physical stove/pot colliders remain bound");
        }
        kitchen.Begin(HouseholdChore.CookRice);
        Assert(!kitchen.IsWorking && !GameManager.Instance.InputBlocked, "Cooking cannot start before rice washing and lighting the fire");
        // Imported FBX roots are not guaranteed to sit at the visible vessel/handle.
        Assert(FindReachableTarget(player, kitchen.waterHit.bounds.center, HouseholdChore.WashRice),
            "The real rice pot can be targeted with E from a clear standing position");
        Vector3 home = camera.transform.localPosition; Quaternion rot = camera.transform.localRotation; float fov = camera.fieldOfView;
        player.GetComponent<PlayerInteraction>().CurrentTarget.Interact();
        yield return new WaitForSeconds(.5f);
        Assert(kitchen.IsWorking && GameManager.Instance.InputBlocked, "Washing acquires modal input without opening the old minigame");
        Assert(Clickable(camera, kitchen.waterHit) && Clickable(camera, kitchen.drainHit), "Water surface and tilt handle are clickable in the real pot view");
        Call(kitchen, "Drain", 1f);
        Assert(flow.ActiveIndex == 5 && kitchen.RinseRound == 0, "Unwashed rice cannot be completed by draining alone");
        Circle(kitchen, .6f);
        Assert(!kitchen.NeedsDrain, "A partial washing gesture does not skip the rinse");
        kitchen.End();
        Assert(!GameManager.Instance.InputBlocked && Vector3.Distance(home, camera.transform.localPosition) < .001f &&
            Quaternion.Angle(rot, camera.transform.localRotation) < .01f && Mathf.Abs(camera.fieldOfView - fov) < .001f,
            "Leaving the rice pot restores camera and input");
        kitchen.Begin(HouseholdChore.WashRice); yield return new WaitForSeconds(.5f);
        Circle(kitchen, 1.4f);
        Assert(kitchen.NeedsDrain, "Circular motion in the water makes it cloudy and enables draining");
        yield return null;
        ScreenCapture.CaptureScreenshot("Tools/Scene2Kitchen/play-wash.png"); yield return new WaitForSeconds(.15f);
        Call(kitchen, "Drain", .5f);
        Assert(kitchen.RinseRound == 0, "Half tilting the pot does not count as an emptied rinse");
        Call(kitchen, "Drain", 1f);
        Assert(kitchen.RinseRound == 1 && flow.ActiveIndex == 5, "The first drained rinse refills without awarding the chore");
        Circle(kitchen, 1.5f); Call(kitchen, "Drain", 1f);
        Assert(flow.ActiveIndex == 6 && flow.Pieces == 60 && Scene2Checkpoint.HomeChores == 6,
            "Two washes and drains save exactly one washing reward");
        Assert(!flow.Complete(HouseholdChore.WashRice), "Rice washing cannot award twice");
        Assert(FindReachableTarget(player, kitchen.hearthHit.bounds.center, HouseholdChore.LightFire),
            "The real fire stove has a clear E interaction");
        player.GetComponent<PlayerInteraction>().CurrentTarget.Interact(); yield return new WaitForSeconds(.5f);
        Assert(kitchen.kindlingHits.All(c => Clickable(camera, c)) && Clickable(camera, kitchen.blowHit) && Clickable(camera, kitchen.hearthHit),
            "Kindling, hearth and blowing tube all project into the close view");
        Call(kitchen, "Blow", .6f);
        Assert(!kitchen.FireLit && flow.ActiveIndex == 6, "Blowing before placing wood cannot light the stove");
        Call(kitchen, "PlaceKindling", 0); Call(kitchen, "PlaceKindling", 0);
        Assert(kitchen.KindlingCount == 1, "A wood piece cannot be placed twice");
        Call(kitchen, "PlaceKindling", 1); kitchen.End();
        kitchen.Begin(HouseholdChore.LightFire); yield return new WaitForSeconds(.5f);
        Assert(kitchen.KindlingCount == 2, "Leaving the stove preserves placed wood for the current visit");
        Call(kitchen, "PlaceKindling", 2);
        Call(kitchen, "Blow", .05f); Call(kitchen, "Blow", 2f);
        Assert(!kitchen.FireLit, "Very short and excessive breaths do not instantly light the stove");
        Call(kitchen, "Blow", .5f); Call(kitchen, "Blow", .5f);
        yield return null;
        ScreenCapture.CaptureScreenshot("Tools/Scene2Kitchen/play-fire.png"); yield return new WaitForSeconds(.15f);
        Call(kitchen, "Blow", .5f);
        Assert(kitchen.FireLit && flow.ActiveIndex == 7 && flow.Pieces == 70 && !GameManager.Instance.InputBlocked,
            "Three controlled breaths light the stove, save its reward and release input");
        Assert(FindReachableTarget(player, kitchen.lidHit.bounds.center, HouseholdChore.CookRice),
            "The real cooking stove can be targeted with E");
        player.GetComponent<PlayerInteraction>().CurrentTarget.Interact(); yield return new WaitForSeconds(.5f);
        Assert(Clickable(camera, kitchen.fuelHit) && Clickable(camera, kitchen.lidHit), "Firewood and lid knob are both reachable in the cooking view");
        Call(kitchen, "LiftLid", 1f);
        Assert(flow.ActiveIndex == 7, "Lifting the lid early cannot skip cooking");
        Call(kitchen, "Simmer", 2f);
        Assert(kitchen.Cooked == 0f, "Excessive heat does not advance cooking");
        Call(kitchen, "AdjustFuel", -.38f);
        for (int i = 0; i < 50; i++) Call(kitchen, "Simmer", .25f);
        Assert(kitchen.Cooked > 3f && kitchen.Cooked < 12f, "Adjusting real wood establishes a gentle simmer and advances cooking");
        float cooked = kitchen.Cooked;
        GameManager.Instance.SetPaused(true); yield return new WaitForSecondsRealtime(.25f);
        Assert(Mathf.Abs(kitchen.Cooked - cooked) < .01f, "Pause freezes cooking progress");
        GameManager.Instance.SetPaused(false);
        kitchen.End();
        Assert(!GameManager.Instance.InputBlocked && Mathf.Abs(kitchen.Cooked - cooked) < .1f, "Stepping away preserves simmer progress and releases input");
        kitchen.Begin(HouseholdChore.CookRice); yield return new WaitForSeconds(.5f);
        for (int i = 0; i < 55; i++) Call(kitchen, "Simmer", .25f);
        Assert(kitchen.Cooked >= 12f && flow.ActiveIndex == 7, "Cooked rice still waits for the player's lid check");
        if (kitchen.importedKitchen)
        {
            Vector3 lidStart = kitchen.lid.position;
            Call(kitchen, "LiftLid", .5f);
            Assert(Vector3.Dot(kitchen.lid.position - lidStart, Vector3.up) > .1f,
                "The imported lid lifts in world-up despite its authored Blender hierarchy");
        }
        yield return null;
        ScreenCapture.CaptureScreenshot("Tools/Scene2Kitchen/play-cook.png"); yield return new WaitForSeconds(.15f);
        Call(kitchen, "LiftLid", 1f);
        Assert(flow.ActiveIndex == 8 && flow.Pieces == 80 && Scene2Checkpoint.HomeChores == 8 && !GameManager.Instance.InputBlocked,
            "Checking the rice completes cooking exactly once and continues to setting the table");
        Assert(!flow.Complete(HouseholdChore.CookRice), "Cooking cannot award twice");
        Call(kitchen, "RestoreWorld");
        Assert(kitchen.FireLit && !kitchen.washRoot.gameObject.activeSelf && kitchen.cookRoot.gameObject.activeSelf,
            "A checkpoint reconstructs lit fire and the completed kitchen world");
        Call(flow, "ResetHomeChores"); yield return null;
        Assert(!kitchen.FireLit && kitchen.fireRoot.gameObject.activeSelf == kitchen.importedKitchen && kitchen.cookRoot.gameObject.activeSelf && kitchen.Cooked == 0f,
            "Chapter restart clears fire and cooking while retaining the room's stove");
        Finish(true, "All checks passed.");
    }

    bool Clickable(Camera camera, Collider collider)
    {
        Physics.SyncTransforms();
        Vector3 point = camera.WorldToScreenPoint(collider.bounds.center);
        bool visible = point.z > 0 && point.x > 0 && point.x < Screen.width && point.y > 0 && point.y < Screen.height &&
            collider.Raycast(camera.ScreenPointToRay(point), out _, 4f);
        if (!visible) report.AppendLine("Unclickable " + collider.name + " enabled=" + collider.enabled + " screen=" + point +
            " size=" + Screen.width + "x" + Screen.height + " bounds=" + collider.bounds + " camera=" + camera.transform.position);
        return visible;
    }
    static void Circle(Scene2KitchenHandsOn kitchen, float turns)
    {
        int steps = Mathf.CeilToInt(turns * 36f);
        for (int i = 0; i <= steps && !kitchen.NeedsDrain; i++)
        {
            float a = i * 10f * Mathf.Deg2Rad;
            Call(kitchen, "WashAt", kitchen.water.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .09f);
        }
    }
    bool FindReachableTarget(FirstPersonController player, Vector3 target, HouseholdChore task)
    {
        var body = player.GetComponent<CharacterController>(); var camera = Camera.main;
        var interaction = player.GetComponent<PlayerInteraction>();
        foreach (float radius in new[] { .8f, 1.2f, 1.65f })
        for (int i = 0; i < 16; i++)
        {
            Vector3 dir = Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward;
            Vector3 feet = target + dir * radius; feet.y = .05f;
            player.TeleportTo(feet, Quaternion.identity); camera.transform.LookAt(target); Physics.SyncTransforms();
            Vector3 center = body.transform.TransformPoint(body.center); float half = Mathf.Max(0, body.height * .5f - body.radius);
            var obstacles = Physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half, body.radius * .95f,
                ~0, QueryTriggerInteraction.Ignore).Where(c => !c.transform.IsChildOf(player.transform)).ToArray();
            if (obstacles.Length > 0)
            {
                if (i % 4 == 0) report.AppendLine(task + " blocked standing pose " + feet + ": " + string.Join(",", obstacles.Select(c => c.name)));
                continue;
            }
            Call(interaction, "HandleRaycast");
            if (interaction.CurrentTarget is HouseholdChoreInteractable t && t.Chore == task) return true;
            if (i % 4 == 0)
                report.AppendLine(task + " focus " + feet + " camera=" + camera.transform.position + " target=" + interaction.CurrentTarget + " ray=" +
                    string.Join(",", Physics.RaycastAll(camera.transform.position, camera.transform.forward, 3f).OrderBy(h => h.distance)
                        .Select(h => h.collider.name + ":" + h.distance.ToString("F2"))));
        }
        report.AppendLine("No E pose found for " + task + " at " + target); return false;
    }
    void Finish(bool success, string message)
    {
        if (done) return; done = true; Application.logMessageReceived -= Log;
        Directory.CreateDirectory("Tools/Scene2Kitchen");
        File.WriteAllText("Tools/Scene2Kitchen/verification.txt", (success ? "PASS" : "FAIL") + " — " + assertions + " assertions\n" + report + "\n" + message);
        Debug.Log("[Hands-on kitchen check] " + (success ? "PASS" : "FAIL") + ": " + message);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
    }
}
#endif
