#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class Scene2PreciseTargetingSmoke : MonoBehaviour
{
    const string Pending = "KuTy.Scene2PreciseTargeting";
    const string Result = "Tools/ProjectAudit/scene2-precise-targeting.txt";
    readonly StringBuilder report = new StringBuilder();
    bool finished;
    bool background;
    int assertions;

    public static void Prepare()
    {
        SessionState.SetBool(Pending, true);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("Scene 2 precise targeting smoke").AddComponent<Scene2PreciseTargetingSmoke>();
    }
    void Start()
    {
        background = Application.runInBackground;
        Application.runInBackground = true;
        Application.logMessageReceived += OnLog;
        StartCoroutine(Run());
    }
    void OnDestroy() { Application.logMessageReceived -= OnLog; Application.runInBackground = background; }
    void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, message + "\n" + stack); }
    void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); assertions++; }
    static void Call(object owner, string method) => owner.GetType().GetMethod(method,
        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);

    IEnumerator Run()
    {
        yield return null;
        var flow = HouseholdChoreFlow.Instance;
        var motor = FindObjectOfType<FirstPersonController>();
        var interaction = motor.GetComponent<PlayerInteraction>();
        var body = motor.GetComponent<CharacterController>();
        var bed = FindObjectOfType<BedWakeUpController>();
        Call(bed, "GetOutOfBed");
        yield return new WaitForSeconds(1.1f);
        motor.enabled = false;
        flow.ShowChecklist();
        var rayField = typeof(PlayerInteraction).GetField("raycastOrigin", BindingFlags.Instance | BindingFlags.NonPublic);
        var ray = motor.PlayerCamera;
        rayField.SetValue(interaction, ray);
        float floor = motor.transform.position.y;
        float eye = motor.PlayerCamera.localPosition.y;
        report.AppendLine("Player floor=" + floor + ", eye height=" + eye);
        foreach (var task in new[] { HouseholdChore.WashRice, HouseholdChore.SetTable, HouseholdChore.ReadLetter })
        {
            while (HouseholdChoreFlow.Order[flow.ActiveIndex] != task)
                Check(flow.Complete(HouseholdChoreFlow.Order[flow.ActiveIndex]), "Cannot prepare chore " + task);
            yield return null;
            var targets = FindObjectsOfType<HouseholdChoreInteractable>().Where(t =>
                t.Chore == task && !string.IsNullOrEmpty(t.GetPromptText())).ToArray();
            Check(targets.Length == 1, task + ": expected one active target, got " + targets.Length);
            var target = targets[0];
            var colliders = target.GetComponentsInChildren<Collider>().Where(c => c.enabled).ToArray();
            Check(colliders.Length > 0, task + ": missing colliders");
            Bounds bounds = colliders[0].bounds;
            foreach (var c in colliders.Skip(1)) bounds.Encapsulate(c.bounds);
            report.AppendLine(task + ": target=" + target.name + ", bounds=" + bounds);
            int poses = 0;
            Vector3 selectedFeet = default, selectedAim = default;
            foreach (float clearance in new[] { .65f, 1f, 1.45f })
            for (int angle = 0; angle < 8; angle++)
            {
                Vector3 dir = Quaternion.Euler(0f, angle * 45f, 0f) * Vector3.forward;
                Vector3 feet = bounds.center + new Vector3(dir.x * (bounds.extents.x + clearance),
                    0f, dir.z * (bounds.extents.z + clearance));
                feet.y = floor;
                motor.TeleportTo(feet, Quaternion.identity);
                Physics.SyncTransforms();
                Vector3 center = body.transform.TransformPoint(body.center);
                float half = Mathf.Max(0f, body.height * .5f - body.radius);
                var obstacles = Physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half,
                    body.radius * .95f, ~0, QueryTriggerInteraction.Ignore).Where(c => !c.transform.IsChildOf(motor.transform)).ToArray();
                if (obstacles.Length > 0)
                { report.AppendLine("  blocked feet=" + feet.ToString("F3") + " by " + string.Join(",", obstacles.Select(c => c.name))); continue; }
                Vector3[] aims = { bounds.center, new Vector3(bounds.center.x, bounds.max.y - .01f, bounds.center.z),
                    bounds.center + Vector3.right * bounds.extents.x * .5f,
                    bounds.center + Vector3.forward * bounds.extents.z * .5f };
                bool hit = false;
                foreach (Vector3 aim in aims)
                {
                    ray.position = feet + Vector3.up * eye;
                    ray.rotation = Quaternion.LookRotation(aim - ray.position);
                    Call(interaction, "HandleRaycast");
                    if (!ReferenceEquals(interaction.CurrentTarget, target)) continue;
                    poses++;
                    selectedFeet = feet; selectedAim = aim; hit = true;
                    report.AppendLine("  reachable feet=" + feet.ToString("F3") + " aim=" + aim.ToString("F3"));
                    break;
                }
                if (!hit)
                {
                    var hits = Physics.RaycastAll(ray.position, ray.forward, 3f, ~0, QueryTriggerInteraction.Collide)
                        .OrderBy(h => h.distance).Select(h => h.collider.name + ":" + h.distance.ToString("F2"));
                    report.AppendLine("  missed feet=" + feet.ToString("F3") + " hits=" + string.Join(",", hits));
                    continue;
                }
                ray.rotation = Quaternion.LookRotation(Vector3.up);
                if (task == HouseholdChore.WashRice)
                    report.AppendLine("  looking away: ray=" + ray.forward + ", main=" + Camera.main.transform.forward +
                        ", assist=" + target.TryGetProximityDistance(motor.transform.position, out _));
                Call(interaction, "HandleRaycast");
                Check(!ReferenceEquals(interaction.CurrentTarget, target), task + ": can focus without looking at the prop");
            }
            Check(poses >= 2, task + ": fewer than two clear reachable standing positions");
            motor.TeleportTo(selectedFeet, Quaternion.identity);
            ray.position = selectedFeet + Vector3.up * eye;
            ray.rotation = Quaternion.LookRotation(selectedAim - ray.position);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = Vector3.Lerp(ray.position, selectedAim, .35f);
            wall.transform.rotation = ray.rotation;
            wall.transform.localScale = new Vector3(2f, 2f, .1f);
            Physics.SyncTransforms();
            Call(interaction, "HandleRaycast");
            Check(!ReferenceEquals(interaction.CurrentTarget, target), task + ": can interact through a wall");
            wall.SetActive(false); Destroy(wall);
            ray.position = selectedAim + Vector3.back * 5f;
            motor.TeleportTo(new Vector3(ray.position.x, floor, ray.position.z), Quaternion.identity);
            ray.position = motor.transform.position + Vector3.up * eye;
            ray.rotation = Quaternion.LookRotation(selectedAim - ray.position);
            Call(interaction, "HandleRaycast");
            Check(!ReferenceEquals(interaction.CurrentTarget, target), task + ": can interact from five metres away");
            motor.TeleportTo(selectedFeet, Quaternion.identity);
            ray.position = selectedFeet + Vector3.up * eye;
            ray.rotation = Quaternion.LookRotation(selectedAim - ray.position);
            Call(interaction, "HandleRaycast");
            Check(ReferenceEquals(interaction.CurrentTarget, target), task + ": lost focus after clearing obstruction");
            if (task == HouseholdChore.WashRice)
            {
                ray.rotation *= Quaternion.Euler(0f, 20f, 0f);
                Call(interaction, "HandleRaycast");
                Check(ReferenceEquals(interaction.CurrentTarget, target),
                    "Rice pot aim assistance did not register the initially hidden pot");
                ray.rotation = Quaternion.LookRotation(selectedAim - ray.position);
            }
            interaction.CurrentTarget.Interact();
            Check(GameManager.Instance.InputBlocked, task + ": selected prop did not open its minigame");
            Call(target, "EndInteraction");
            Check(!GameManager.Instance.InputBlocked && flow.IsCurrent(task), task + ": cancelling lost input or granted progress");
            Check(flow.Complete(task), task + ": cannot complete after cancelling");
        }
        Finish(true, "Visible props reachable; no proximity, wall or distance bypass; selected-target open/cancel passed.");
    }
    void Finish(bool passed, string message)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnLog;
        Directory.CreateDirectory("Tools/ProjectAudit");
        File.WriteAllText(Result, (passed ? "PASS" : "FAIL") + " (" + assertions + " assertions)\n" + report + message);
        Debug.Log("[Scene 2 precise targeting] " + (passed ? "PASS" : "FAIL") + ": " + message);
        EditorApplication.isPlaying = false;
    }
}
#endif
