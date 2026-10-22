using System;
using System.Collections;
using System.Collections.Generic;
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

/// <summary>Exercise actual screens and capture overlays at small, standard and narrow aspect ratios.</summary>
[InitializeOnLoad]
public static class KuTyUiPresentationReview
{
    const string Pending = "KuTy.UI.Review.Pending";
    static KuTyUiPresentationReview()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            new GameObject("UI presentation review").AddComponent<KuTyUiPresentationReviewRunner>();
        };
    }

    [MenuItem("KuTy/Tests/Review all game UI and screen ratios")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        KuTyCheckpointTestScope.Capture();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class KuTyUiPresentationReviewRunner : MonoBehaviour
{
    const string Output = "Library/UiPresentationReview";
    readonly StringBuilder report = new StringBuilder();
    int checks;
    bool finished;
    bool oldBackground;
    float started;
    static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Get<T>(object owner, string name) => (T)owner.GetType().GetField(name, Fields).GetValue(owner);
    static void Call(object owner, string name) => owner.GetType().GetMethod(name, Fields).Invoke(owner, null);

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/results.txt", "RUNNING");
        oldBackground = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        Application.logMessageReceived += OnLog;
        StartCoroutine(Run());
    }
    void OnDestroy() { Application.logMessageReceived -= OnLog; Application.runInBackground = oldBackground; }
    void Update() { if (!finished && Time.realtimeSinceStartup - started > 210) Finish(false, "Timed out."); }
    void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, message + "\n" + stack); }
    void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); checks++; }

    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(3);
        Call(FindObjectOfType<BedWakeUpController>(), "GetOutOfBed");
        yield return new WaitForSecondsRealtime(1.2f);
        DialogueUI.Instance?.HideDialogue();
        HouseholdChoreFlow.Instance.ShowChecklist();
        var motor = FindObjectOfType<FirstPersonController>();
        motor.TeleportTo(new Vector3(6.4f, .08f, 3f), Quaternion.Euler(0, 73f, 0));
        motor.SetPitch(2);
        motor.IsMovementLocked = true;
        yield return null;
        Capture("home-hud", 1280, 720);
        Capture("home-hud", 1920, 1080);
        Capture("home-hud", 1440, 1080);
        Capture("home-hud", 2560, 1080);

        var quest = FindObjectOfType<Scene2QuestHUD>();
        var board = Get<GameObject>(quest, "board");
        board.SetActive(true);
        Capture("mission-board", 1280, 720);
        Capture("mission-board", 1440, 1080);
        board.SetActive(false);

        var pause = FindObjectOfType<PauseUI>();
        pause.Pause();
        yield return null;
        Check(GameManager.Instance.IsPaused, "Pause must still freeze gameplay.");
        Capture("pause", 1280, 720);
        pause.Resume();
        Check(!GameManager.Instance.IsPaused, "Resume must still release pause.");

        DialogueUI.Instance.ShowDialogue("MẸ", "Sau này đi đâu làm gì, con cũng nhớ phải ăn cơm đúng bữa nhé. Nhà mình luôn có một chỗ chờ con.", 0);
        yield return new WaitForSecondsRealtime(4);
        Capture("dialogue", 1280, 720);
        Capture("dialogue", 1440, 1080);
        DialogueUI.Instance.HideDialogue();
        Check(!DialogueUI.Instance.IsShowing, "Dialogue must close normally.");

        JournalUI.Instance.Open();
        Capture("journal", 1280, 720);
        Capture("journal", 1440, 1080);
        JournalUI.Instance.Close();
        Check(!JournalUI.Instance.IsOpen, "Journal must close normally.");

        ShopUI.Instance.OpenShop();
        yield return null;
        Capture("shop", 1280, 720);
        Capture("shop", 1440, 1080);
        Check(FindObjectsOfType<ShopItemSlotUI>().Length == 2, "Both shop items must remain available.");
        ShopUI.Instance.CloseShop();
        Check(!ShopUI.Instance.IsOpen, "Shop must close normally.");

        var task = FindObjectsOfType<HouseholdChoreInteractable>().First(t => t.isActiveAndEnabled && t.Chore == HouseholdChore.ArrangeKeepsakes);
        task.Interact();
        yield return null;
        Check(GameManager.Instance.InputBlocked, "Chore review must open the actual interaction panel.");
        Capture("household", 1280, 720);
        Capture("household", 1440, 1080);
        Call(task, "EndInteraction");
        Check(HouseholdChoreFlow.Instance.ActiveIndex == 0, "Closing a chore must not award progress.");

        SceneLoader.Instance.LoadScene(SceneNames.Act3_OutSide, .05f);
        yield return Ready(SceneNames.Act3_OutSide);
        Capture("village-hud", 1280, 720);
        var marbles = MarbleAimUI.Instance;
        marbles.Show(1, 5, 1, 3);
        yield return null;
        Capture("marbles", 1280, 720);
        marbles.Hide();

        GameManager.Instance.ReturnToMainMenu();
        yield return Ready(SceneNames.MainMenu);
        Capture("main-menu", 1280, 720);
        Capture("main-menu", 1440, 1080);
        var menu = FindObjectOfType<MainMenuUI>();
        var settings = menu.GetComponentsInChildren<Button>(true).First(b => b.name == "Btn_Settings");
        settings.onClick.Invoke();
        Capture("settings", 1280, 720);
        Check(menu.GetComponentsInChildren<Slider>(true).Length >= 2, "Settings sliders exist.");
        var settingsPanel = menu.GetComponentInChildren<GameSettingsUI>(true);
        var settingsSliders = settingsPanel.GetComponentsInChildren<Slider>(true);
        float originalVolume = GameUiPreferences.MasterVolume;
        float originalSensitivity = GameUiPreferences.LookSensitivity;
        bool hadVolume = PlayerPrefs.HasKey("KuTy.Settings.MasterVolume");
        bool hadSensitivity = PlayerPrefs.HasKey("KuTy.Settings.LookSensitivity");
        try
        {
            settingsSliders[0].value = .35f;
            Check(Mathf.Approximately(AudioListener.volume, .35f), "Volume slider changes real game audio.");
            settingsSliders[1].value = 1.25f;
            Check(Mathf.Approximately(GameUiPreferences.LookSensitivity, 1.25f), "Sensitivity slider changes the shared look setting.");
        }
        finally
        {
            GameUiPreferences.SetMasterVolume(originalVolume);
            GameUiPreferences.SetLookSensitivity(originalSensitivity);
            if (!hadVolume) PlayerPrefs.DeleteKey("KuTy.Settings.MasterVolume");
            if (!hadSensitivity) PlayerPrefs.DeleteKey("KuTy.Settings.LookSensitivity");
        }
        menu.GetComponentsInChildren<Button>(true).First(b => b.name == "Btn_Back").onClick.Invoke();

        SceneLoader.Instance.LoadScene(SceneNames.Act1, .05f);
        yield return Ready(SceneNames.Act1);
        yield return new WaitForSecondsRealtime(2);
        Capture("office-dialogue", 1280, 720);
        var intro = FindObjectOfType<OfficeIntroController>();
        intro.SkipToReadyAtDeskForTests();
        yield return new WaitForSecondsRealtime(.3f);
        Capture("office-hud", 1280, 720);

        SceneLoader.Instance.LoadScene(SceneNames.Act_Ending, .05f);
        yield return Ready(SceneNames.Act_Ending);
        yield return new WaitForSecondsRealtime(3.2f);
        Capture("ending", 1280, 720);
        Capture("ending", 1440, 1080);
        Finish(true, "All screens rendered; original checkpoint restored when leaving Play Mode.");
    }

    IEnumerator Ready(string scene)
    {
        float start = Time.realtimeSinceStartup;
        while (SceneManager.GetActiveScene().name != scene || SceneLoader.Instance.IsLoading)
        {
            if (Time.realtimeSinceStartup - start > 45) throw new TimeoutException(scene);
            yield return null;
        }
        yield return null;
        yield return null;
    }

    void Capture(string name, int width, int height)
    {
        var camera = Camera.main;
        Check(camera != null, name + ": main camera exists.");
        var canvases = FindObjectsOfType<Canvas>().Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace).ToArray();
        var states = new List<(Canvas canvas, Camera camera, float distance, RenderMode mode)>();
        var target = new RenderTexture(width, height, 24);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            foreach (var canvas in canvases)
            {
                states.Add((canvas, canvas.worldCamera, canvas.planeDistance, canvas.renderMode));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = Mathf.Max(.5f, camera.nearClipPlane + .2f);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var safe in FindObjectsOfType<GameUiSafeArea>()) safe.SendMessage("Apply");
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Output + "/" + name + "-" + width + "x" + height + ".png", image.EncodeToPNG());
            foreach (var text in FindObjectsOfType<TMP_Text>())
            {
                if (!text.isActiveAndEnabled || !text.canvas || text.canvas.renderMode == RenderMode.WorldSpace) continue;
                if (text.GetComponentInParent<CanvasGroup>() is CanvasGroup group && group.alpha < .1f) continue;
                if (text.name == "MoneyText" || text.name == "ObjectiveText" || text.name == "MissionList" ||
                    text.name == "MemoryHeading" || text.name == "MemoryName" || text.name == "HintText" ||
                    (DialogueUI.Instance && text == Get<TMP_Text>(DialogueUI.Instance, "bodyText")) ||
                    (JournalUI.Instance && text == Get<TMP_Text>(JournalUI.Instance, "bodyText")))
                {
                    text.ForceMeshUpdate();
                    Check(!text.isTextOverflowing, name + ": overflowing text " + text.name + " at " + width + "x" + height);
                }
            }
            report.AppendLine(name + " " + width + "x" + height + " rendered");
        }
        finally
        {
            camera.targetTexture = oldTarget;
            foreach (var state in states)
            {
                state.canvas.renderMode = state.mode;
                state.canvas.worldCamera = state.camera;
                state.canvas.planeDistance = state.distance;
            }
            Canvas.ForceUpdateCanvases();
            RenderTexture.active = oldActive;
            target.Release(); Destroy(target); Destroy(image);
        }
    }

    void Finish(bool passed, string message)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnLog;
        File.WriteAllText(Output + "/results.txt", (passed ? "PASS" : "FAIL") + " (" + checks + " checks)\n" + message + "\n" + report);
        Debug.Log("[UI review] " + (passed ? "PASS" : "FAIL") + " " + message);
        EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
    }
}
