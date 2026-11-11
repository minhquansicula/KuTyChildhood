#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Scene2SpinningTopSmoke : MonoBehaviour
{
    private const string Pending = "KuTy.SpinningTopSmoke";
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("KuTy/Tests/Check Scene 2 Spinning Top")]
    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().name != SceneNames.Act2_Home)
            throw new InvalidOperationException("Open Scene 2 in Edit Mode first.");
        Type.GetType("KuTyCheckpointTestScope, Assembly-CSharp-Editor")
            .GetMethod("Capture").Invoke(null, null);
        SessionState.SetBool(Pending, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartTest()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        SessionState.EraseBool(Pending);
        new GameObject("Spinning top smoke").AddComponent<Scene2SpinningTopSmoke>();
    }

    private IEnumerator Start()
    {
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        var checks = Check();
        while (true)
        {
            object step;
            try
            {
                if (!checks.MoveNext()) { Debug.Log("[SpinningTopSmoke] PASS"); break; }
                step = checks.Current;
            }
            catch (Exception error)
            {
                Debug.LogError("[SpinningTopSmoke] FAIL " + error);
                break;
            }
            yield return step;
        }
        Application.runInBackground = background;
        EditorApplication.ExitPlaymode();
    }

    private static IEnumerator Check()
    {
        yield return null;
        var bed = FindObjectOfType<BedWakeUpController>();
        var toy = FindObjectOfType<Scene2SpinningTop>();
        var player = FindObjectOfType<FirstPersonController>();
        var flow = HouseholdChoreFlow.Instance;
        if (bed == null || toy == null || player == null || flow == null)
            throw new InvalidOperationException("Scene 2 is missing its intro bindings.");
        var visual = toy.LookTarget.GetComponentInChildren<MeshRenderer>();
        if (visual == null || visual.bounds.size.y < .11f || visual.bounds.size.y > .15f ||
            visual.sharedMaterial.GetTexture("_BaseMap") == null)
            throw new InvalidOperationException("The new spinning top mesh or wood texture did not load at the expected size.");
        float deadline = Time.realtimeSinceStartup + 20f;
        while (!bed.CanPauseWhileWaiting && Time.realtimeSinceStartup < deadline) yield return null;
        if (!bed.CanPauseWhileWaiting) throw new InvalidOperationException("Wake-up never reached the exit prompt.");
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", PrivateInstance).Invoke(bed, null);
        yield return new WaitForSecondsRealtime(2f);
        if (flow.ChecklistVisible) throw new InvalidOperationException("The chores opened before the toy.");

        var view = player.PlayerCamera;
        player.enabled = false;
        Vector3 towardPlayer = Vector3.ProjectOnPlane(view.position - toy.LookTarget.position, Vector3.up).normalized;
        Vector3 focus = toy.LookTarget.position + Vector3.up * .04f;
        var interaction = player.GetComponent<PlayerInteraction>();
        var aim = typeof(PlayerInteraction).GetMethod("HandleRaycast", PrivateInstance);
        Vector3 position = view.position;
        Physics.SyncTransforms();
        for (int angle = 0; angle < 360; angle += 30)
        {
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * towardPlayer;
            position = toy.LookTarget.position + direction * .76f + Vector3.up * .83f;
            view.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position));
            aim.Invoke(interaction, null);
            if (interaction.CurrentTarget == toy) break;
        }
        player.TeleportTo(new Vector3(position.x, player.transform.position.y, position.z), player.transform.rotation);
        view.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position));
        if (interaction.CurrentTarget != toy || !interaction.TryInteract())
            throw new InvalidOperationException($"The toy is obstructed from {position}, toy at {toy.LookTarget.position}.");
        yield return new WaitForSecondsRealtime(.8f);
        typeof(Scene2SpinningTop).GetMethod("Cancel", PrivateInstance).Invoke(toy, null);
        if (string.IsNullOrEmpty(toy.GetPromptText())) throw new InvalidOperationException("Cancel did not return the toy to the table.");
        toy.Interact();
        yield return new WaitForSecondsRealtime(.8f);
        var beginWind = typeof(Scene2SpinningTop).GetMethod("BeginWinding", PrivateInstance);
        var trackWind = typeof(Scene2SpinningTop).GetMethod("TrackWinding", PrivateInstance);
        var endWind = typeof(Scene2SpinningTop).GetMethod("EndWinding", PrivateInstance);
        var windingCamera = view.GetComponent<Camera>();
        Vector3 knot = ((Transform)typeof(Scene2SpinningTop).GetField("cordKnot", PrivateInstance).GetValue(toy)).position;
        Vector3 center = toy.LookTarget.position + Vector3.up * .009f;
        Vector2 Point(int step) => (Vector2)windingCamera.WorldToScreenPoint(center +
            Quaternion.AngleAxis(-step * 4.5f, Vector3.up) * (knot - center));
        typeof(Scene2SpinningTop).GetMethod("Launch", PrivateInstance).Invoke(toy, null);
        if (toy.ReadyToThrow) throw new InvalidOperationException("The top launched before winding.");
        beginWind.Invoke(toy, new object[] { Point(40) });
        trackWind.Invoke(toy, new object[] { Point(41) });
        if (toy.WindingProgress > 0f)
            throw new InvalidOperationException("The cord could be grabbed away from its knot.");
        beginWind.Invoke(toy, new object[] { Point(0) });
        for (int step = 1; step <= 72; step++) trackWind.Invoke(toy, new object[] { Point(step) });
        float beforeReverse = toy.WindingProgress;
        for (int step = 71; step >= 64; step--) trackWind.Invoke(toy, new object[] { Point(step) });
        if (toy.WindingProgress >= beforeReverse)
            throw new InvalidOperationException("Reversing did not unwind the cord.");
        for (int step = 65; step <= 304; step++) trackWind.Invoke(toy, new object[] { Point(step) });
        endWind.Invoke(toy, null);
        if (!toy.ReadyToThrow || toy.WindingProgress < 1f)
            throw new InvalidOperationException("Three full circles did not prepare the throw.");
        typeof(Scene2SpinningTop).GetField("pull", PrivateInstance).SetValue(toy, .7f);
        typeof(Scene2SpinningTop).GetField("throwDirection", PrivateInstance).SetValue(toy, Vector3.ProjectOnPlane(player.transform.position - toy.LookTarget.position, Vector3.up).normalized);
        Vector3 startTop = toy.LookTarget.position;
        Vector3 startPlayer = player.transform.position;
        typeof(Scene2SpinningTop).GetMethod("Launch", PrivateInstance).Invoke(toy, null);
        yield return new WaitForSeconds(2.8f);
        if (Vector3.Distance(toy.LookTarget.position, startTop) > .01f)
            throw new InvalidOperationException("The top moved before its three-second spin finished.");
        float followDeadline = Time.realtimeSinceStartup + 35f;
        while (!toy.AwaitingKitchenVisit && Time.realtimeSinceStartup < followDeadline) yield return null;
        if (!toy.AwaitingKitchenVisit || flow.ChecklistVisible || Vector3.Distance(player.transform.position, startPlayer) > .02f)
            throw new InvalidOperationException($"The top must wait in the kitchen while the player remains free: waiting={toy.AwaitingKitchenVisit}, checklist={flow.ChecklistVisible}, moved={Vector3.Distance(player.transform.position, startPlayer):0.000}.");
        player.TeleportTo(toy.LookTarget.position + Vector3.forward * .5f, player.transform.rotation);
        yield return new WaitForSeconds(.2f);
        if (flow.ChecklistVisible) throw new InvalidOperationException("Chores opened before the doorway dialogue.");
        yield return new WaitForSeconds(4.4f);
        if (!flow.ChecklistVisible || !Scene2Checkpoint.HasCheckpoint)
            throw new InvalidOperationException("The mother's request did not appear when the player reached the kitchen.");

    }
}
#endif
