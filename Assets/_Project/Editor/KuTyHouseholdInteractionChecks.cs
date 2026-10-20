using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Checks modal cancellation, all chore surfaces, and duplicate completion in Play Mode.</summary>
[InitializeOnLoad]
public static class KuTyHouseholdInteractionChecks
{
    private const string Pending = "KuTy.HandsOnChecks.Pending";
    private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static int assertions;
    private static bool previewOpen;

    [MenuItem("KuTy/Tests/Preview household chest in Play Mode")]
    public static void PreviewChest()
    {
        if (!Application.isPlaying || previewOpen) return;
        previewOpen = true;
        var host = new GameObject("HouseholdChestPreview");
        var ui = new HouseholdChoreUI(host.transform);
        ui.SetChecklistVisible(false);
        host.GetComponentInChildren<Canvas>().sortingOrder = 200;
        GameManager.Instance?.AcquireInput(host);
        ui.ShowGame("Xếp đồ vào rương", "Kéo vật vào ngăn cùng hình, sau đó kéo nắp rương xuống.", false);
        ui.Actions.Begin(HouseholdChore.ArrangeKeepsakes, () =>
        {
            ui.HideGame();
            GameManager.Instance?.ReleaseInput(host);
            UnityEngine.Object.Destroy(host);
            previewOpen = false;
        }, ui.SetProgress, ui.SetHint, () => { });
        EditorApplication.QueuePlayerLoopUpdate();
        // Preview owns a separate modal and never calls the story flow or awards memories.
        EditorApplication.update += TickPreview;
        void TickPreview()
        {
            if (host == null || !Application.isPlaying)
            {
                EditorApplication.update -= TickPreview;
                previewOpen = false;
                return;
            }
            ui.Tick();
            EditorApplication.QueuePlayerLoopUpdate();
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            ui.HideGame();
            GameManager.Instance?.ReleaseInput(host);
            UnityEngine.Object.Destroy(host);
        }
    }

    static KuTyHouseholdInteractionChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                EditorApplication.delayCall += Execute;
            }
        };
    }

    [MenuItem("KuTy/Tests/Check household interactions")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    public static void RunBatch()
    {
        // Only the unattended batch invocation opens a blank scene; interactive checks keep the user's scene.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Run();
    }

    private static T Get<T>(object instance, string name) => (T)instance.GetType().GetField(name, Private).GetValue(instance);
    private static void Set(object instance, string name, object value) => instance.GetType().GetField(name, Private).SetValue(instance, value);
    private static void Call(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, Private).Invoke(instance, args);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        assertions++;
    }

    private static void Execute()
    {
        GameObject host = null;
        int exitCode = 0;
        try
        {
            assertions = 0;
            host = new GameObject("HouseholdChecks", typeof(Canvas));
            var board = new HouseholdActionBoard(host.transform);
            int finished = 0;
            Action finish = () => finished++;
            foreach (var chore in HouseholdChoreFlow.Order)
            {
                board.Begin(chore, finish, (value, message) => Check(value >= 0f && value <= 1f, "Progress outside 0..1"), _ => { }, () => { });
                Check(Get<bool>(board, "active"), chore + " did not open");
                Check(Get<RectTransform>(board, "tutorialHand") != null, chore + " is missing hand guide");
                board.Hide();
                Call(board, "Complete");
                Check(finished == 0, chore + " completed after cancellation");
            }
            board.Begin(HouseholdChore.ArrangeKeepsakes, finish, null, null, null);
            Call(board, "Place", 2);
            Call(board, "Place", 0);
            Call(board, "Place", 0);
            Check(Get<int>(board, "successes") == 2, "Duplicate placement was counted");
            Call(board, "Place", 1);
            Check(finished == 0 && Get<int>(board, "stage") == 1, "Chest completed before closing its lid");
            Check(Get<RectTransform>(board, "actionArea").name == "FinalGesture", "Chest lid missing");
            Call(board, "Complete");
            Call(board, "Complete");
            Check(finished == 1, "Completion callback must fire once");
            board.Hide();

            board.Begin(HouseholdChore.MeasureRice, finish, null, null, null);
            Set(board, "stage", 2);
            Set(board, "amount", .9f);
            Call(board, "TickMeasure");
            Check(Mathf.Approximately(Get<float>(board, "amount"), .9f), "Overfilled rice was reset instead of remaining correctable");
            Set(board, "amount", .66f);
            Call(board, "TickMeasure");
            Check(Get<int>(board, "stage") == 3, "Enough rice should allow putting the scoop away");
            board.Hide();

            board.Begin(HouseholdChore.WashDishes, finish, null, null, null);
            Array.Copy(new[] { 0f, 1f, 1f }, Get<float[]>(board, "stainRemaining"), 3);
            Call(board, "TickScrub");
            Check(Get<int>(board, "stage") == 0, "Cleaning one stain cleaned the whole dish");
            Array.Clear(Get<float[]>(board, "stainRemaining"), 0, 3);
            Call(board, "TickScrub");
            Check(Get<int>(board, "stage") == 1, "Clean dish did not request a rinse");
            Check(Get<RectTransform>(board, "rinseZone").gameObject.activeSelf, "Rinse zone missing");
            Check(!Get<RectTransform>(board, "rack").gameObject.activeSelf, "Rack available before rinsing");
            board.Hide();

            board.Begin(HouseholdChore.TuneRadio, finish, null, null, null);
            Call(board, "UpdateRadioAudio", .8f);
            Check(Get<AudioSource>(board, "radioNoise") != null, "Radio noise source missing");
            Check(Get<AudioSource>(board, "radioTune").volume > Get<AudioSource>(board, "radioNoise").volume, "Radio clarity did not mix audio");
            board.Hide();
            Check(Get<AudioSource>(board, "radioNoise") == null && Get<AudioClip>(board, "tuneClip") == null, "Radio resources survived cancellation");
            Check(finished == 1, "Other gestures incorrectly completed a chore");
            Debug.Log("[HouseholdChecks] PASS " + assertions + " assertions; all 14 surfaces, cancellation, chest, rice, dish stages and radio cleanup.");
        }
        catch (Exception error)
        {
            exitCode = 1;
            Debug.LogException(error);
        }
        finally
        {
            if (host != null) UnityEngine.Object.Destroy(host);
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            else EditorApplication.ExitPlaymode();
        }
    }
}
