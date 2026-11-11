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
        Vector3 startTop = toy.LookTarget.position;
        Vector3 startPlayer = player.transform.position;
        typeof(Scene2SpinningTop).GetMethod("Launch", PrivateInstance).Invoke(toy, null);
        yield return new WaitForSeconds(2.8f);
        if (Vector3.Distance(toy.LookTarget.position, startTop) > .01f)
            throw new InvalidOperationException("The top moved before its three-second spin finished.");
        float followDeadline = Time.realtimeSinceStartup + 45f;
        while (!flow.ChecklistVisible && Time.realtimeSinceStartup < followDeadline) yield return null;
        if (!flow.ChecklistVisible || Vector3.Distance(player.transform.position, startPlayer) < .2f)
            throw new InvalidOperationException("The automatic follow did not advance the player and open the chores.");

        var dream = Act2CountrysideShot.Play();
        try
        {
            if (!dream.MoveNext()) throw new InvalidOperationException("The countryside shot could not start.");
            yield return new WaitForEndOfFrame();
            var camera = GameObject.Find("Dream camera")?.GetComponent<Camera>();
            if (camera == null || camera.targetTexture == null || !camera.targetTexture.IsCreated())
                throw new InvalidOperationException("The countryside camera did not render.");
        }
        finally
        {
            (dream as IDisposable)?.Dispose();
        }
    }
}
#endif
