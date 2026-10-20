using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class KuTyMusicChecks
{
    private const string Pending = "KuTy.MusicChecks.Pending";
    private const string PreviousStart = "KuTy.MusicChecks.PreviousStart";

    static KuTyMusicChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                new GameObject("Soundtrack checks", typeof(AudioListener)).AddComponent<KuTyMusicCheckRunner>();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending, false))
            {
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(PreviousStart, ""));
                SessionState.EraseBool(Pending);
                SessionState.EraseString(PreviousStart);
            }
        };
    }

    [MenuItem("KuTy/Tests/Check selected soundtrack in isolation")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        KuTyMusicSetup.Validate();
        const string path = "Assets/_Project/Generated/MusicSmoke.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try { EditorSceneManager.SaveScene(scene, path); }
        finally { EditorSceneManager.CloseScene(scene, true); }
        SessionState.SetString(PreviousStart, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Pending, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class KuTyMusicCheckRunner : MonoBehaviour
{
    private bool finished;
    private bool previousBackground;
    private float started;
    private readonly System.Text.StringBuilder report = new System.Text.StringBuilder();
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    private void Start()
    {
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        Application.logMessageReceived += OnLog;
        StartCoroutine(Run());
    }

    private void Update() { if (!finished && Time.realtimeSinceStartup - started > 60f) Finish(false, "Timeout"); }
    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        Application.runInBackground = previousBackground;
    }
    private void OnLog(string message, string stack, LogType type)
    {
        if (!finished && (type == LogType.Error || type == LogType.Exception)) Finish(false, message + "\n" + stack);
    }
    private void Check(bool value, string message)
    {
        if (!value) { Finish(false, message); throw new InvalidOperationException(message); }
        report.AppendLine("PASS " + message);
    }
    private IEnumerator Run()
    {
        yield return null;
        var manager = AudioManager.Instance;
        Check(manager != null, "Bootstrap creates AudioManager in an empty scene");
        var source = (AudioSource)typeof(AudioManager).GetField("bgmSource", Flags).GetValue(manager);
        var library = (SoundLibrary)typeof(AudioManager).GetField("soundLibrary", Flags).GetValue(manager);
        var game = GameManager.Instance;
        game.SetGameState(GameState.MainMenu);
        yield return new WaitForSecondsRealtime(2f);
        Check(source.isPlaying && source.clip == library.bgmMainMenu && source.spatialBlend == 0f, "Menu plays Childhood in 2D");
        game.SetGameState(GameState.Act1_RealWorld);
        yield return new WaitForSecondsRealtime(1.8f);
        Check(!source.isPlaying, "Office stops menu music when no office BGM is assigned");
        game.SetGameState(GameState.Act2_MemoryWorld_Home);
        yield return new WaitForSecondsRealtime(1.8f);
        Check(source.isPlaying && source.clip == library.bgmAct2, "Home plays countryside guitar");

        var canvas = new GameObject("Music test canvas", typeof(RectTransform), typeof(Canvas));
        var board = new HouseholdActionBoard(canvas.transform);
        board.SetFont(AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_Project/Generated/Vietnamese.asset"));
        board.Begin(HouseholdChore.ReadLetter, () => {}, (v,s) => {}, s => {}, () => {});
        yield return new WaitForSecondsRealtime(1.8f);
        Check(source.clip == library.bgmLetter && source.isPlaying, "Opening letter plays Reverie");
        typeof(HouseholdActionBoard).GetMethod("Complete", Flags).Invoke(board, null);
        board.Hide();
        yield return new WaitForSecondsRealtime(1.8f);
        Check(source.clip == library.bgmAct2 && source.isPlaying, "Completing and closing letter restores home music");
        board.Begin(HouseholdChore.ReadLetter, () => {}, (v,s) => {}, s => {}, () => {});
        yield return new WaitForSecondsRealtime(1.8f);
        board.Hide();
        yield return new WaitForSecondsRealtime(1.8f);
        Check(source.clip == library.bgmAct2, "Cancelling letter restores home music");
        Destroy(canvas);

        game.SetGameState(GameState.Act3_MemoryWorld_OutSide);
        yield return new WaitForSecondsRealtime(1.8f);
        Check(source.isPlaying && source.clip == library.bgmVillage, "Village uses countryside track");
        game.SetPaused(true);
        yield return new WaitForSecondsRealtime(.2f);
        float pausedTime = source.time;
        yield return new WaitForSecondsRealtime(.4f);
        Check(AudioListener.pause && Mathf.Abs(source.time - pausedTime) < .1f, "Pause freezes music playback");
        game.SetPaused(false);
        yield return new WaitForSecondsRealtime(.4f);
        Check(!AudioListener.pause && source.time > pausedTime, "Resume continues music");
        source.time = source.clip.length - 1.8f;
        yield return new WaitForSecondsRealtime(3.5f);
        Check(source.isPlaying && source.time < 6f && source.volume > .3f, "Track tail fades and restarts");
        manager.BeginLetterMusic();
        game.SetGameState(GameState.Act_Ending);
        yield return new WaitForSecondsRealtime(1.8f);
        manager.EndLetterMusic();
        yield return new WaitForSecondsRealtime(.2f);
        Check(source.isPlaying && source.clip == library.bgmAct3, "Ending state takes priority over closing letter");

        game.SetGameState(GameState.MainMenu);
        var menu = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(KuTyMainMenuIntegration.PrefabPath));
        yield return null;
        yield return null;
        var buttons = menu.GetComponentsInChildren<Button>(true);
        Check(buttons.All(b => b.GetComponents<MenuButtonSfx>().Length == 1), "Every menu button, including hidden panels, has exactly one SFX binding");
        var settings = buttons.Single(b => b.name == "Btn_Settings");
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        int played = manager.SuccessfulSfxPlayCount;
        UnityEngine.EventSystems.ExecuteEvents.Execute(settings.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
        Check(manager.SuccessfulSfxPlayCount == played + 1 && manager.LastPlayedSfx == SfxId.UIHover, "Pointer entry plays one hover cue");
        UnityEngine.EventSystems.ExecuteEvents.Execute(settings.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
        Check(manager.SuccessfulSfxPlayCount == played + 1, "Rapid repeated hover is rate limited");
        settings.onClick.Invoke();
        Check(manager.LastPlayedSfx == SfxId.UIConfirm, "Settings click plays confirm audio");
        var settingsBack = buttons.First(b => b.name == "Btn_Back" && b.gameObject.activeInHierarchy);
        settingsBack.onClick.Invoke();
        Check(manager.LastPlayedSfx == SfxId.UIBack && !settingsBack.gameObject.activeInHierarchy, "Back click plays audio and preserves panel action");
        bool enabled = settings.interactable;
        settings.interactable = false;
        played = manager.SuccessfulSfxPlayCount;
        yield return new WaitForSecondsRealtime(.1f);
        UnityEngine.EventSystems.ExecuteEvents.Execute(settings.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
        settings.onClick.Invoke();
        Check(manager.SuccessfulSfxPlayCount == played && !settingsBack.gameObject.activeInHierarchy, "Disabled button is silent and does not run its action");
        settings.interactable = enabled;
        MenuButtonSfx.Attach(settings);
        Check(settings.GetComponents<MenuButtonSfx>().Length == 1, "Repeated binding does not duplicate the component");
        yield return new WaitForSecondsRealtime(.1f);
        var credits = menu.GetComponentsInChildren<Button>(true).Single(b => b.name == "MusicCreditsButton");
        credits.onClick.Invoke();
        Check(manager.LastPlayedSfx == SfxId.UIConfirm, "Dynamically created credits button plays click sound");
        var close = menu.GetComponentsInChildren<Button>(true).Single(b => b.name == "CloseMusicCredits");
        Check(close.gameObject.activeInHierarchy, "Music attribution opens from menu");
        close.onClick.Invoke();
        Check(manager.LastPlayedSfx == SfxId.UIBack, "Credits back button plays back sound");
        Check(!close.gameObject.activeInHierarchy, "Music attribution closes without navigating or changing saves");
        Finish(true, "Soundtrack checks passed; no scene navigation or checkpoint writes.");
    }

    private void Finish(bool success, string message)
    {
        if (finished) return;
        finished = true;
        Directory.CreateDirectory("Tools/Music");
        File.WriteAllText("Tools/Music/playmode-checks.txt", (success ? "PASS\n" : "FAIL\n") + report + message);
        EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
    }
}
