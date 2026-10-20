using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class KuTyMainMenuChecks
{
    private const string Pending = "KuTy.MainMenuChecks.Pending";
    static KuTyMainMenuChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Main menu integration checks").AddComponent<KuTyMainMenuCheckRunner>();
        };
    }

    [MenuItem("KuTy/Tests/Check main menu and checkpoint navigation")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        KuTyCheckpointTestScope.Capture();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    public static void RunBatch()
    {
        KuTyCheckpointTestScope.Restore();
        KuTyMainMenuIntegration.Integrate();
        Run();
    }
}

public sealed class KuTyMainMenuCheckRunner : MonoBehaviour
{
    private int assertions;
    private bool finished;
    private bool previousBackground;
    private float started;
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        Application.logMessageReceived += OnLog;
        SceneManager.sceneLoaded += SuppressWorldRendering;
        StartCoroutine(Run());
    }
    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup - started > 100f) Finish(false, "Timed out");
    }
    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        SceneManager.sceneLoaded -= SuppressWorldRendering;
        Application.runInBackground = previousBackground;
    }
    private void SuppressWorldRendering(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneNames.MainMenu) return;
        // This suite verifies navigation/state, not world graphics. Avoid GPU-heavy scene rendering
        // during repeated resume tests; all changes are transient and disappear when Play Mode ends.
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var regions in root.GetComponentsInChildren<VillageRiceRegions>(true)) regions.enabled = false;
            foreach (var field in root.GetComponentsInChildren<RuralRiceFieldRenderer>(true)) field.enabled = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }
    }
    private void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, message + "\n" + stack);
    }
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        assertions++;
    }
    private IEnumerator Ready(string name)
    {
        float start = Time.realtimeSinceStartup;
        while (SceneManager.GetActiveScene().name != name || (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading))
        {
            if (Time.realtimeSinceStartup - start > 35f) throw new TimeoutException("Waiting for " + name);
            yield return null;
        }
        yield return null;
        yield return null;
    }
    private MainMenuUI Menu()
    {
        var menus = FindObjectsOfType<MainMenuUI>();
        Check(menus.Length == 1, "Expected exactly one active menu controller");
        Check(menus[0].name == "MainMenu_Canvas", "The inactive legacy menu became active");
        return menus[0];
    }
    private Button Button(MainMenuUI menu, string name) => menu.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
    private void Binding(Button button, MainMenuUI menu, string method)
    {
        Check(button.onClick.GetPersistentEventCount() == 1 && button.onClick.GetPersistentTarget(0) == menu &&
              button.onClick.GetPersistentMethodName(0) == method, "Incorrect button binding: " + button.name);
    }
    private IEnumerator Run()
    {
        yield return Ready(SceneNames.MainMenu);
        var menu = Menu();
        int authoredButtons = menu.GetComponentsInChildren<Button>(true).Length;
        Binding(Button(menu, "Btn_NewGame"), menu, nameof(MainMenuUI.OnPlayButtonClicked));
        Binding(Button(menu, "Btn_LoadGame"), menu, nameof(MainMenuUI.OnContinueButtonClicked));
        Binding(Button(menu, "Btn_Quit"), menu, nameof(MainMenuUI.OnQuitButtonClicked));
        foreach (var caption in menu.GetComponentsInChildren<TMP_Text>(true))
            Check(caption.font != null && caption.font.HasCharacters(caption.text, out uint[] missing, true, true),
                "Menu font cannot display caption: " + caption.text);
        Check(!Button(menu, "Btn_LoadGame").interactable, "Continue is enabled without a checkpoint");
        menu.OnContinueButtonClicked();
        yield return null;
        Check(SceneManager.GetActiveScene().name == SceneNames.MainMenu && !Scene2Checkpoint.HasCheckpoint,
            "Continue without a checkpoint started a new game");
        var settings = menu.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Panel_Settings");
        Check(!settings.gameObject.activeSelf, "Settings should start closed");
        Button(menu, "Btn_Settings").onClick.Invoke();
        Check(settings.gameObject.activeSelf, "Settings did not open");
        settings.GetComponentInChildren<Button>().onClick.Invoke();
        Check(!settings.gameObject.activeSelf, "Settings back button did not close the panel");

        Scene2Checkpoint.SaveHome(4);
        GameManager.Instance.ReturnToMainMenu();
        yield return Ready(SceneNames.MainMenu);
        menu = Menu();
        Check(menu.GetComponentsInChildren<Button>(true).Length == authoredButtons, "Checkpoint created a duplicate Continue button");
        Check(Button(menu, "Btn_LoadGame").interactable, "Continue disabled for a home checkpoint");
        Capture(menu);
        Button(menu, "Btn_LoadGame").onClick.Invoke();
        yield return Ready(SceneNames.Act2_Home);
        var chores = HouseholdChoreFlow.Instance;
        Check(chores != null && chores.ActiveIndex == 4 && chores.Pieces == 40, "Continue lost household progress");
        chores.ShowChecklist();
        foreach (var chore in HouseholdChoreFlow.Order.Skip(4)) Check(chores.Complete(chore), "Failed to prepare outdoor checkpoint");
        Check(Scene2Checkpoint.HasOutsideProgress, "Outdoor checkpoint was not saved");
        DialogueUI.Instance?.HideDialogue();
        GameManager.Instance.ReturnToMainMenu();
        yield return Ready(SceneNames.MainMenu);
        menu = Menu();
        Check(Button(menu, "Btn_LoadGame").interactable && Scene2Checkpoint.ResumeScene == SceneNames.Act3_OutSide,
            "Menu did not recognize the outdoor checkpoint");
        Button(menu, "Btn_LoadGame").onClick.Invoke();
        yield return Ready(SceneNames.Act3_OutSide);
        Check(CurrencyManager.Instance.CurrentMoney == 2000 && QuestManager.Instance.CurrentStep == QuestStep.BuyCandyAndMarbles,
            "Continue lost outdoor allowance or quest state");
        GameManager.Instance.ReturnToMainMenu();
        yield return Ready(SceneNames.MainMenu);
        menu = Menu();
        Button(menu, "Btn_NewGame").onClick.Invoke();
        yield return Ready(SceneNames.Act1);
        Check(!Scene2Checkpoint.HasCheckpoint, "New Game retained the previous checkpoint");
        Finish(true, "Menu bindings, settings/back, no-save state, home/outdoor resume and New Game passed");
    }
    private void Capture(MainMenuUI menu)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var canvas = menu.GetComponent<Canvas>();
        var camera = Camera.main;
        if (canvas == null || camera == null) return;
        var target = new RenderTexture(1366, 768, 24);
        var texture = new Texture2D(1366, 768, TextureFormat.RGB24, false);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        var oldMode = canvas.renderMode;
        var oldCamera = canvas.worldCamera;
        float oldDistance = canvas.planeDistance;
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1366, 768), 0, 0);
            texture.Apply();
            Directory.CreateDirectory("Library/MenuIntegration");
            File.WriteAllBytes("Library/MenuIntegration/menu.png", texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.planeDistance = oldDistance;
            target.Release();
            Destroy(target);
            Destroy(texture);
        }
    }
    private void Finish(bool success, string message)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnLog;
        Directory.CreateDirectory("Library/MenuIntegration");
        string result = (success ? "PASS" : "FAIL") + " (" + assertions + " assertions)\n" + message;
        File.WriteAllText("Library/MenuIntegration/results.txt", result);
        if (success) Debug.Log("[MainMenuChecks] " + result);
        else Debug.LogError("[MainMenuChecks] " + result);
        if (Application.isBatchMode)
        {
            KuTyCheckpointTestScope.Restore();
            EditorApplication.Exit(success ? 0 : 1);
        }
        else EditorApplication.ExitPlaymode();
    }
}
