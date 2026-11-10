#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public sealed class Scene2MealDeliveryChecks : MonoBehaviour
{
    private const string Pending = "KuTy.MealDeliveryChecks.Pending";
    private bool background;

    static Scene2MealDeliveryChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.EraseBool(Pending);
            var runner = new GameObject("Meal delivery checks").AddComponent<Scene2MealDeliveryChecks>();
            runner.background = Application.runInBackground;
            Application.runInBackground = true;
            EditorApplication.update += EditorApplication.QueuePlayerLoopUpdate;
        };
    }

    [MenuItem("KuTy/Tests/Check Scene 2 Meal Delivery")]
    private static void Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != SceneNames.Act2_Home)
            throw new InvalidOperationException("Open Scene 2 in Edit Mode first.");
        Type.GetType("KuTyCheckpointTestScope, Assembly-CSharp-Editor").GetMethod("Capture").Invoke(null, null);
        Scene2Checkpoint.SaveHome(2);
        SessionState.SetBool(Pending, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.EnterPlaymode();
    }

    private IEnumerator Start()
    {
        yield return null;
        var checks = CheckDelivery();
        while (true)
        {
            object step;
            try
            {
                if (!checks.MoveNext())
                {
                    Debug.Log("[MealDeliveryChecks] PASS");
                    break;
                }
                step = checks.Current;
            }
            catch (Exception error)
            {
                Debug.LogError($"[MealDeliveryChecks] FAIL: {error}");
                break;
            }
            yield return step;
        }
        EditorApplication.ExitPlaymode();
    }

    private void OnDestroy()
    {
        EditorApplication.update -= EditorApplication.QueuePlayerLoopUpdate;
        Application.runInBackground = background;
    }

    private static IEnumerator CheckDelivery()
    {
        var flow = HouseholdChoreFlow.Instance;
        var player = FindObjectOfType<FirstPersonController>();
        var carrier = player.GetComponent<Scene2FoodCarry>();
        var view = player.PlayerCamera;
        var table = GameObject.Find("bandai_khach").GetComponent<Collider>();
        var cover = GameObject.Find("LongBan").GetComponent<Scene2FoodInteractable>();
        var potBounds = GameObject.Find("noicom").GetComponent<Collider>().bounds;
        Require(potBounds.size.y < potBounds.size.z, "The rice pot is upright.");
        flow.ShowChecklist();
        Require(flow.IsCurrent(HouseholdChore.SetTable), "The meal chore starts from its checkpoint.");
        Require(!flow.GetComponent<Scene2HouseholdSimulator>().Handles(HouseholdChore.SetTable), "The old table minigame is unavailable.");
        player.enabled = false;
        view.position = cover.GetComponent<Collider>().bounds.center + Vector3.up * .5f;
        Require(cover.GetPromptText() == "[E] Lấy lồng bàn", "The cover has its own Vietnamese prompt.");

        int placements = 0;
        carrier.FoodPlaced += (_, _) => placements++;
        var names = new[] { "Canh", "ThitKho", "RauXao", "noicom" };
        Bounds bounds = table.bounds;
        Vector3 along = bounds.size.x > bounds.size.z ? Vector3.right * bounds.size.x : Vector3.forward * bounds.size.z;
        for (int i = 0; i < names.Length; i++)
        {
            var food = GameObject.Find(names[i]).GetComponent<Scene2FoodInteractable>();
            view.position = food.GetComponent<Collider>().bounds.center + Vector3.up * 4f;
            Require(string.IsNullOrEmpty(food.GetPromptText()), $"{names[i]} is out of reach.");
            view.position -= Vector3.up * 3.5f;
            Require(food.GetPromptText().StartsWith("[E] Cầm"), $"{names[i]} can be carried nearby.");
            food.Interact();
            yield return new WaitForSecondsRealtime(.35f);
            Require(carrier.HeldItem == food, $"{names[i]} is held.");
            if (i == 0)
            {
                carrier.PlaceHeldItem(food.transform.position, Quaternion.identity, null);
                yield return new WaitForSecondsRealtime(.35f);
                Require(flow.IsCurrent(HouseholdChore.SetTable), "Placement elsewhere does not complete the chore.");
                view.position = food.GetComponent<Collider>().bounds.center + Vector3.up * .5f;
                food.Interact();
                yield return new WaitForSecondsRealtime(.35f);
            }
            view.SetPositionAndRotation(bounds.center + along * (-.33f + .22f * i) +
                Vector3.up * (bounds.extents.y + .48f), Quaternion.LookRotation(Vector3.down, Vector3.forward));
            Physics.SyncTransforms();
            Require(carrier.UpdatePlacementRay(new Ray(view.position, view.forward), 3f) &&
                carrier.GetPromptText().Contains("bàn dài phòng khách"), $"{names[i]} targets the living-room table.");
            carrier.Interact();
            yield return new WaitForSecondsRealtime(.35f);
            Require(!carrier.IsHolding && placements == i + 2, $"{names[i]} was placed.");
            Require(flow.IsCompleted(HouseholdChore.SetTable) == (i == names.Length - 1),
                "The reward requires all four dishes on the right table.");
        }
        Require(flow.CurrentChore == HouseholdChore.WashDishes && flow.Pieces == 30, "The next chore and one reward are recorded.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log("[MealDeliveryChecks] " + message);
    }
}
#endif
