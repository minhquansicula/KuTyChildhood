#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Exercises the real scene, shared interaction owner, checkpoint, cancellation and world feedback.</summary>
public sealed class Scene2RadioKitchenSmoke : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    readonly StringBuilder report = new StringBuilder();
    bool check, finished, previousBackground;
    int assertions;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        string mode = SessionState.GetString("KuTy.RadioKitchen.Run", "");
        if (string.IsNullOrEmpty(mode)) return;
        SessionState.EraseString("KuTy.RadioKitchen.Run");
        var smoke = new GameObject("Radio kitchen preview").AddComponent<Scene2RadioKitchenSmoke>();
        smoke.check = mode == "check";
    }

    void Start()
    {
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        if (check) Application.logMessageReceived += OnLog;
        StartCoroutine(Run());
    }
    void OnDestroy()
    {
        Application.runInBackground = previousBackground;
        Application.logMessageReceived -= OnLog;
    }
    void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, message + "\n" + stack); }
    void Assert(bool value, string message)
    { if (!value) { Finish(false, message); throw new InvalidOperationException(message); } assertions++; report.AppendLine("PASS " + message); }
    static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Private).Invoke(owner, args);

    IEnumerator Run()
    {
        yield return null;
        var bed = FindObjectOfType<BedWakeUpController>();
        Call(bed, "GetOutOfBed");
        yield return new WaitForSeconds(1.2f);
        var sequence = FindObjectOfType<Scene2RadioKitchenSequence>();
        var flow = HouseholdChoreFlow.Instance;
        var player = FindObjectOfType<FirstPersonController>();
        var interaction = player.GetComponent<PlayerInteraction>();
        var camera = Camera.main;
        flow.Voice.Stop();
        float eye = camera.transform.localPosition.y;
        float floor = player.transform.position.y;
        Vector3 feet = sequence.closeView.position - Vector3.up * eye;
        feet.y = player.transform.position.y;
        player.TeleportTo(feet, Quaternion.Euler(0f, sequence.closeView.eulerAngles.y, 0f));
        player.SetPitch(sequence.closeView.eulerAngles.x);
        flow.ShowChecklist();
        if (!check) yield break;
        player.enabled = false;
        yield return null;
        Assert(flow.ActiveIndex == 1, "Preview starts at radio, preserving original chapter order");
        Assert(sequence.stationClip && sequence.staticClip && sequence.motherClip, "All three local audio clips are bound");
        camera.transform.LookAt(sequence.radioRig.position);
        Call(interaction, "HandleRaycast");
        Assert(interaction.CurrentTarget is HouseholdChoreInteractable radioTarget && radioTarget.Chore == HouseholdChore.TuneRadio,
            "Looking at the radio finds its real E interaction");
        Vector3 position = camera.transform.localPosition;
        Quaternion rotation = camera.transform.localRotation;
        float fov = camera.fieldOfView;
        sequence.InteractWith(HouseholdChore.TuneRadio);
        Assert(sequence.IsTuning && GameManager.Instance.InputBlocked, "Radio owns player input");
        Call(sequence, "AdvanceSignalLock", 2f);
        Assert(flow.ActiveIndex == 1 && !sequence.RadioOn, "Noise cannot complete the radio chore");
        sequence.EndTuning();
        Assert(!GameManager.Instance.InputBlocked && !sequence.IsTuning, "Cancelling releases input");
        Assert(Vector3.Distance(position, camera.transform.localPosition) < .001f &&
            Quaternion.Angle(rotation, camera.transform.localRotation) < .01f && Mathf.Abs(fov - camera.fieldOfView) < .001f,
            "Cancellation restores camera pose and FOV");
        sequence.InteractWith(HouseholdChore.TuneRadio);
        yield return new WaitForSeconds(.6f);
        Vector3 screen = camera.WorldToScreenPoint(sequence.tuningKnob.position);
        Assert(sequence.knobCollider.Raycast(camera.ScreenPointToRay(screen), out _, 3f), "Physical dial is clickable in its close view");
        ScreenCapture.CaptureScreenshot("Tools/Scene2Radio/play-radio.png");
        yield return new WaitForSeconds(.15f);
        Call(sequence, "AdjustFrequency", .56f);
        Call(sequence, "AdvanceSignalLock", 1.4f);
        Assert(flow.ActiveIndex == 2 && sequence.RadioOn && !sequence.IsTuning, "A stable station completes tuning and releases the camera");
        Assert(flow.Pieces == 20 && Scene2Checkpoint.HomeChores == 2, "Radio reward and checkpoint are granted exactly once");
        Assert(!flow.Complete(HouseholdChore.TuneRadio), "Repeated completion cannot duplicate rewards");
        var station = (AudioSource)typeof(Scene2RadioKitchenSequence).GetField("stationAudio", Private).GetValue(sequence);
        Assert(station.isPlaying && station.spatialBlend == 1f, "Radio keeps playing spatial music after leaving the dial");
        yield return new WaitForSeconds(3.8f);
        var mother = (AudioSource)typeof(Scene2RadioKitchenSequence).GetField("motherAudio", Private).GetValue(sequence);
        Assert(mother.isPlaying && mother.transform.parent == sequence.kitchenSpot, "Mother calls from the kitchen after the music beat");
        float motherTime = mother.time;
        GameManager.Instance.SetPaused(true);
        yield return new WaitForSecondsRealtime(.3f);
        Assert(Mathf.Abs(mother.time - motherTime) < .15f, "Pause suspends the kitchen voice");
        GameManager.Instance.SetPaused(false);
        bool pickupReachable = false;
        var playerBody = player.GetComponent<CharacterController>();
        foreach (float radius in new[] { 1.2f, 1.6f, 2f })
        for (int i = 0; i < 16 && !pickupReachable; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward;
            feet = sequence.woodPickup.position + dir * radius;
            feet.y = floor;
            player.TeleportTo(feet, Quaternion.identity);
            camera.transform.LookAt(sequence.woodBundle.position);
            Physics.SyncTransforms();
            Vector3 center = playerBody.transform.TransformPoint(playerBody.center);
            float half = Mathf.Max(0f, playerBody.height * .5f - playerBody.radius);
            bool blocked = Physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half,
                playerBody.radius * .95f, ~0, QueryTriggerInteraction.Ignore).Any(c => !c.transform.IsChildOf(player.transform));
            Call(interaction, "HandleRaycast");
            if (!blocked && interaction.CurrentTarget is HouseholdChoreInteractable woodTarget && woodTarget.Chore == HouseholdChore.CarryWood)
            { pickupReachable = true; break; }
            report.AppendLine("Pickup pose " + feet + " camera=" + camera.transform.position + " blocked=" + blocked +
                " hits=" + string.Join(",", Physics.RaycastAll(camera.transform.position, camera.transform.forward, 3f, ~0,
                    QueryTriggerInteraction.Collide).OrderBy(h => h.distance).Select(h => h.collider.name + ":" + h.distance.ToString("F2"))));
        }
        Assert(pickupReachable, "The wood bundle has a clear reachable pose with a real E pickup interaction");
        sequence.InteractWith(HouseholdChore.CarryWood);
        Assert(sequence.CarryingWood && sequence.woodBundle.parent == camera.transform && flow.ActiveIndex == 2,
            "Picking up real wood does not prematurely complete the chore");
        Assert(!sequence.CanPlaceWood(), "Wood cannot be delivered from the bedroom");
        sequence.ReturnWood();
        Assert(!sequence.CarryingWood && !GameManager.Instance.InputBlocked, "Returning the bundle leaves the task retryable");
        sequence.InteractWith(HouseholdChore.CarryWood);
        Assert(sequence.CarryingWood, "Bundle can be picked up again after returning it");
        bool reachable = false;
        var body = player.GetComponent<CharacterController>();
        foreach (float radius in new[] { .8f, 1.2f, 1.55f })
        for (int i = 0; i < 16 && !reachable; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward;
            feet = sequence.woodRest.position + dir * radius;
            feet.y = floor;
            player.TeleportTo(feet, Quaternion.identity);
            camera.transform.LookAt(sequence.woodRest.position);
            Physics.SyncTransforms();
            Vector3 center = body.transform.TransformPoint(body.center);
            float half = Mathf.Max(0f, body.height * .5f - body.radius);
            var obstacles = Physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half,
                body.radius * .95f, ~0, QueryTriggerInteraction.Ignore).Where(c => !c.transform.IsChildOf(player.transform)).ToArray();
            bool clear = sequence.CanPlaceWood();
            if (obstacles.Length > 0 || !clear)
            {
                Physics.Linecast(camera.transform.position, sequence.woodRest.position, out var obstacle, ~0, QueryTriggerInteraction.Ignore);
                report.AppendLine("Pose " + feet + " blocked=" + string.Join(",", obstacles.Select(c => c.name)) +
                    " placement=" + clear + " sight=" + (obstacle.collider ? obstacle.collider.name : "clear") +
                    " camera=" + camera.transform.position + " distance=" + Vector3.Distance(camera.transform.position, sequence.woodRest.position) +
                    " carry=" + sequence.CarryingWood + " current=" + flow.IsCurrent(HouseholdChore.CarryWood));
                continue;
            }
            reachable = true; break;
        }
        Assert(reachable, "Stove destination has a clear, physically reachable standing pose");
        Call(interaction, "HandleRaycast");
        Assert(interaction.CurrentTarget is Scene2WoodPlaceInteractable, "The shared E targeting finds the actual wood destination");
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = Vector3.Lerp(camera.transform.position, sequence.woodRest.position, .5f);
        wall.transform.localScale = Vector3.one * .35f;
        Physics.SyncTransforms();
        Assert(!sequence.CanPlaceWood(), "A wall blocks placement instead of accepting wood through scenery");
        Destroy(wall);
        yield return null;
        ScreenCapture.CaptureScreenshot("Tools/Scene2Radio/play-carry-wood.png");
        yield return new WaitForSeconds(.15f);
        sequence.PlaceWood();
        Assert(!sequence.CarryingWood && sequence.woodBundle.parent == sequence.woodRest && flow.ActiveIndex == 3,
            "Placing wood changes the world and continues to fetching water");
        Assert(flow.Pieces == 30 && Scene2Checkpoint.HomeChores == 3, "Delivery saves exactly one chore reward");
        sequence.PlaceWood();
        Assert(flow.Pieces == 30, "Repeated delivery does not grant another reward");
        Call(sequence, "RestoreWorld");
        Assert(sequence.RadioOn && sequence.woodBundle.parent == sequence.woodRest, "Checkpoint reconstruction restores music and delivered wood");
        Call(flow, "ResetHomeChores");
        yield return null;
        Assert(!sequence.RadioOn && !sequence.CarryingWood && !station.isPlaying && sequence.woodBundle.parent.name == "WoodPickup",
            "Restart resets music, carried props and pending kitchen call");
        Finish(true, "All checks passed.");
    }

    void Finish(bool success, string message)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnLog;
        Directory.CreateDirectory("Tools/Scene2Radio");
        File.WriteAllText("Tools/Scene2Radio/verification.txt", (success ? "PASS" : "FAIL") +
            " — " + assertions + " assertions\n" + report + "\n" + message);
        Debug.Log("[Radio kitchen smoke] " + (success ? "PASS" : "FAIL") + ": " + message);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
    }
}
#endif
