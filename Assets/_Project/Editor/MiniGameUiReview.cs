using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MiniGameUiReview
{
    private const string Pending = "KuTy.MiniGameReview.Pending";
    static MiniGameUiReview()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Mini-game UI review").AddComponent<MiniGameUiReviewRunner>();
        };
    }

    [MenuItem("KuTy/Tests/Review mini game UI before")]
    public static void Before() => Run("Before");
    [MenuItem("KuTy/Tests/Review mini game UI after")]
    public static void After() => Run("After");
    private static void Run(string phase)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save the scene and leave Play Mode first.");
        KuTyCheckpointTestScope.Capture();
        SessionState.SetString("KuTy.MiniGameReview.Phase", phase);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class MiniGameUiReviewRunner : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly StringBuilder report = new StringBuilder();
    private string output;
    private bool finished;
    private bool background;
    private float started;
    private int screenshots;
    private static T Get<T>(object owner, string field) => (T)owner.GetType().GetField(field, Private).GetValue(owner);
    private static void Call(object owner, string method) => owner.GetType().GetMethod(method, Private).Invoke(owner, null);

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        output = "Library/MiniGameUiReview/" + SessionState.GetString("KuTy.MiniGameReview.Phase", "After");
        Directory.CreateDirectory(output);
        File.WriteAllText(output + "/result.txt", "RUNNING");
        background = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        Application.logMessageReceived += OnLog;
        StartCoroutine(Review());
    }

    private void OnDestroy() { Application.logMessageReceived -= OnLog; Application.runInBackground = background; }
    private void Update() { if (!finished && Time.realtimeSinceStartup - started > 240f) Finish(false, "Review timed out."); }
    private void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) Finish(false, message + "\n" + stack); }

    private IEnumerator Review()
    {
        yield return new WaitForSecondsRealtime(3f);
        Call(FindObjectOfType<BedWakeUpController>(), "GetOutOfBed");
        yield return new WaitForSecondsRealtime(1f);
        DialogueUI.Instance?.HideDialogue();
        GameManager.Instance?.AcquireInput(this);
        var ui = HouseholdChoreFlow.Instance.UI;
        var instructions = typeof(HouseholdChoreInteractable).GetMethod("InstructionFor", BindingFlags.Static | BindingFlags.NonPublic);
        foreach (HouseholdChore chore in Enum.GetValues(typeof(HouseholdChore)))
        {
            ui.ShowGame(HouseholdChoreFlow.Titles[(int)chore], (string)instructions.Invoke(null, new object[] { chore }), false, () => ui.HideGame(), () => ui.Actions.Restart());
            ui.Actions.Begin(chore, () => { }, ui.SetProgress, ui.SetHint, () => { });
            ui.Actions.SetFont(GameUiTheme.Current.font);
            yield return null;
            Capture("home-" + chore, 1280, 720);
            Capture("home-" + chore, 1440, 1080);
            Get<Button>(ui, "restartButton").onClick.Invoke();
            Require(Get<bool>(ui.Actions, "active"), "Restart button closed the task: " + chore);
            Get<Button>(ui, "leaveButton").onClick.Invoke();
            Require(!Get<bool>(ui.Actions, "active"), "Leave button left a task active: " + chore);
            ui.HideGame();
        }
        ProgressBarUI.Instance.Show("Chén 2/5 — giữ chuột trái, Esc: tạm nghỉ");
        ProgressBarUI.Instance.SetProgress(.65f);
        yield return null;
        Capture("dish-progress", 1280, 720);
        ProgressBarUI.Instance.Hide();
        GameManager.Instance?.ReleaseInput(this);

        SceneLoader.Instance.LoadScene(SceneNames.Act1, .05f);
        yield return Ready(SceneNames.Act1);
        yield return new WaitForSecondsRealtime(1f);
        FindObjectOfType<OfficeIntroController>().SkipToReadyAtDeskForTests();
        yield return new WaitForSecondsRealtime(.5f);
        DialogueUI.Instance?.HideDialogue();
        GameManager.Instance?.AcquireInput(this);
        var excel = FindObjectsOfType<ExcelDataAuditorGame>(true).First();
        Call(OfficeSceneController.Instance, "StartExcelWork");
        yield return new WaitForSecondsRealtime(.3f);
        Capture("excel-tutorial", 1280, 720);
        Capture("excel-tutorial", 1440, 1080);
        excel.BeginAttempt(ExcelAssistMode.Relaxed);
        yield return new WaitForSecondsRealtime(.3f);
        Capture("excel-workbook", 1280, 720);
        Capture("excel-workbook", 1440, 1080);
        Capture("excel-workbook", 2560, 1080);
        Call(excel, "CheckValidation");
        yield return null;
        Capture("excel-validation", 1280, 720);
        Call(excel, "ShowChatPopup");
        Capture("excel-chat", 1280, 720);
        Call(excel, "CloseChatPopup");
        typeof(ExcelDataAuditorGame).GetMethod("FailGame", Private).Invoke(excel, new object[] { "HẾT THỜI GIAN\nDữ liệu đã nhập được giữ lại." });
        yield return new WaitForSecondsRealtime(.1f);
        Capture("excel-timeout", 1280, 720);
        Get<GameObject>(excel, "mainPanel").transform.Find("MiniGameResult/ResultCard/Retry").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.1f);
        Require(Get<bool>(excel, "attemptRunning") && !Get<bool>(excel, "relaxedMode"), "Timed retry did not restart");
        var input = Get<System.Collections.Generic.Dictionary<Vector2Int, TMP_InputField>>(excel, "visibleInputs").Values.First();
        input.text = "1300";
        typeof(ExcelDataAuditorGame).GetMethod("FailGame", Private).Invoke(excel, new object[] { "Hết thời gian" });
        Get<GameObject>(excel, "mainPanel").transform.Find("MiniGameResult/ResultCard/Relaxed").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.1f);
        Require(Get<bool>(excel, "attemptRunning") && Get<bool>(excel, "relaxedMode") && input.text == "1300", "Relaxed retry lost input or did not restart");
        var inputs = Get<System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<Vector2Int, string>>>(excel, "userInputs");
        inputs[0][new Vector2Int(4, 3)] = "1300";
        inputs[1][new Vector2Int(4, 3)] = "11500";
        Call(excel, "CheckValidation");
        Require(!Get<bool>(excel, "attemptRunning"), "Correct workbook was not accepted");
        yield return null;
        Capture("excel-success", 1280, 720);
        excel.ClosePanel();
        GameManager.Instance?.ReleaseInput(this);

        SceneLoader.Instance.LoadScene(SceneNames.Act3_OutSide, .05f);
        yield return Ready(SceneNames.Act3_OutSide);
        GameManager.Instance?.AcquireInput(this);
        var marble = FindObjectOfType<MarbleShootingGame>();
        var cameraAnchor = Get<Transform>(marble, "gameCameraPosition");
        Camera.main.transform.SetPositionAndRotation(cameraAnchor.position, cameraAnchor.rotation);
        var lookAt = Get<Transform>(marble, "gameCameraLookAt");
        if (lookAt) Camera.main.transform.LookAt(lookAt);
        MarbleAimUI.Instance.Show(2, 5, 1, 3);
        MarbleAimUI.Instance.UpdateForceIndicator(.65f);
        MarbleAimUI.Instance.SetExitAction(marble.CancelGame);
        yield return null;
        Capture("marbles-aim", 1280, 720);
        Capture("marbles-aim", 1440, 1080);
        MarbleAimUI.Instance.SetStatus("Chưa đủ bi ra khỏi vòng. Sắp xếp lại để thử tiếp — không mất hũ bi.");
        Capture("marbles-retry", 1280, 720);
        MarbleAimUI.Instance.Hide();
        GameManager.Instance?.ReleaseInput(this);
        Finish(true, "All active mini-game families rendered. Checkpoint restored on exit.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private IEnumerator Ready(string name)
    {
        float time = Time.realtimeSinceStartup;
        while (SceneManager.GetActiveScene().name != name || SceneLoader.Instance.IsLoading)
        {
            if (Time.realtimeSinceStartup - time > 45) throw new TimeoutException(name);
            yield return null;
        }
        yield return null;
        yield return null;
    }

    private void Capture(string name, int width, int height)
    {
        var camera = Camera.main;
        var canvases = FindObjectsOfType<Canvas>().Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace).ToArray();
        var states = canvases.Select(c => (canvas: c, mode: c.renderMode, camera: c.worldCamera, distance: c.planeDistance)).ToArray();
        var target = new RenderTexture(width, height, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = Mathf.Max(.5f, camera.nearClipPlane + .2f);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var safe in FindObjectsOfType<GameUiSafeArea>()) safe.SendMessage("Apply");
            foreach (var fit in FindObjectsOfType<MiniGamePanelFit>()) fit.Refresh();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(output + "/" + name + "-" + width + "x" + height + ".png", image.EncodeToPNG());
            screenshots++;
            foreach (var label in FindObjectsOfType<TMP_Text>())
            {
                if (!label.isActiveAndEnabled || !label.canvas || label.canvas.renderMode == RenderMode.WorldSpace) continue;
                label.ForceMeshUpdate();
                if (label.isTextOverflowing) report.AppendLine("OVERFLOW " + name + " " + width + "x" + height + " " + label.name + ": " + label.text);
            }
        }
        finally
        {
            camera.targetTexture = previousTarget;
            foreach (var state in states)
            {
                state.canvas.renderMode = state.mode;
                state.canvas.worldCamera = state.camera;
                state.canvas.planeDistance = state.distance;
            }
            RenderTexture.active = previousActive;
            target.Release(); Destroy(target); Destroy(image);
        }
    }

    private void Finish(bool passed, string message)
    {
        if (finished) return;
        finished = true;
        StopAllCoroutines();
        Application.logMessageReceived -= OnLog;
        File.WriteAllText(output + "/result.txt", (passed ? "PASS" : "FAIL") + " · " + screenshots + " screenshots\n" + message + "\n" + report);
        Debug.Log("[Mini-game UI review] " + (passed ? "PASS" : "FAIL") + " " + message);
        EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
    }
}
