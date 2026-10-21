using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KuTyOfficeOpeningFix
{
    private const string Pending = "KuTy.OpeningFix.Test";
    private const string Previous = "KuTy.OpeningFix.PreviousStart";
    static KuTyOfficeOpeningFix()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                new GameObject("Opening regression check").AddComponent<KuTyOpeningCheckRunner>().Begin();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending, false))
            {
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Previous, ""));
                SessionState.EraseBool(Pending);
            }
        };
    }

    [MenuItem("KuTy/Office/Fix opening framing")]
    public static void FixFraming()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty)
            throw new InvalidOperationException("Open a saved Act1 scene in Edit Mode.");
        if (scene.name != SceneNames.Act1)
            scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act1_Office_Blockout.unity");
        var intro = UnityEngine.Object.FindObjectOfType<OfficeIntroController>();
        var boss = GameObject.Find("BossPlaceholder_SLOT");
        var head = intro && intro.bossAnimation ? intro.OpeningLookTarget : boss != null ? boss.transform.Find("Visual_REPLACE/Head") : null;
        if (!intro || !head) throw new InvalidOperationException("Intro or existing boss head missing.");
        Directory.CreateDirectory("Tools/OfficeOpening");
        if (!File.Exists("Tools/OfficeOpening/before-framing.unity"))
            File.Copy(scene.path, "Tools/OfficeOpening/before-framing.unity");
        Undo.RecordObject(intro.spawn, "Frame boss beside monitor");
        Vector3 position = new Vector3(-5.9f, intro.spawn.position.y, 1.6f);
        Vector3 direction = head.position - position; direction.y = 0f;
        intro.spawn.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        var serialized = new SerializedObject(intro);
        serialized.FindProperty("openingLookTarget").objectReferenceValue = head;
        serialized.FindProperty("openingFadeDuration").floatValue = 2.1f;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Office opening] Opening camera now looks at the existing boss from beside the monitor.");
    }

    [MenuItem("KuTy/Tests/Check office opening transition")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        SessionState.SetString(Previous, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Project/Generated/MusicSmoke.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class KuTyOpeningCheckRunner : MonoBehaviour
{
    private readonly System.Text.StringBuilder report = new System.Text.StringBuilder();
    private bool previousBackground, finished;
    private float started;
    public void Begin()
    {
        DontDestroyOnLoad(gameObject);
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        StartCoroutine(Run());
    }
    private void Check(bool condition, string message)
    {
        report.AppendLine((condition ? "PASS " : "FAIL ") + message);
        if (!condition) { Finish(false); throw new InvalidOperationException(message); }
    }
    private void Update() { if (!finished && Time.realtimeSinceStartup-started > 90f) Finish(false); }
    private IEnumerator Run()
    {
        yield return null;
        SceneLoader.Instance.LoadScene(SceneNames.Act1, .7f);
        while (SceneManager.GetActiveScene().name != SceneNames.Act1) yield return null;
        yield return null;
        var intro = FindObjectOfType<OfficeIntroController>();
        Check(intro != null && !intro.IsPaused && !intro.hud.pausePanel.activeSelf, "Scene transition does not activate intro pause");
        Check(intro.hud.OpeningFadeAlpha > .9f, "Office starts behind a black fade");
        while (SceneLoader.Instance.IsLoading) yield return null;
        yield return new WaitForSeconds(.65f);
        Check(!intro.IsPaused && intro.hud.OpeningFadeAlpha > 0f && intro.hud.OpeningFadeAlpha < 1f, "Black overlay fades progressively");
        Check(intro.LineElapsed == 0f && !intro.hud.subtitlePanel.activeSelf, "Dialogue waits until the opening image is visible");
        while (intro.IsFadingIn) yield return null;
        yield return new WaitForSeconds(.3f);
        Check(intro.hud.OpeningFadeAlpha == 0f && intro.hud.subtitlePanel.activeSelf && intro.LineElapsed > 0f, "Dialogue begins after fade without a pause panel");
        var boss = intro.bossAnimation;
        Check(boss != null, "Animated boss is connected to the existing intro owner");
        var bossAnimator = boss.GetComponent<Animator>();
        Check(boss.CurrentState == "Boss_Talk_Stern" && !bossAnimator.applyRootMotion,
            "Boss begins speaking without root drift");
        Check(boss.GetComponentsInChildren<SkinnedMeshRenderer>().Length == 6 &&
            boss.GetComponentsInChildren<MeshRenderer>().Length == 0,
            "Boss contains six character meshes and no imported furniture");
        Check(intro.hud.subtitlePanel.GetComponent<UnityEngine.UI.Image>().sprite == OfficeUiStyle.Rounded &&
            intro.hud.subtitle.color == OfficeUiStyle.Paper,
            "Dialogue retains the dark rounded text box and cream text");
        var head = intro.OpeningLookTarget;
        Check(head != null, "Existing boss visual is present");
        Vector3 screen = intro.view.WorldToViewportPoint(head.position);
        Check(screen.z > 0f && screen.x > .2f && screen.x < .8f && screen.y > .2f && screen.y < .8f, "Boss head is framed in front of the player");
        bool blocked = Physics.Linecast(intro.view.transform.position, head.position, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore);
        Check(!blocked || hit.transform == head || hit.transform.IsChildOf(head.parent), "Boss head is not hidden behind a solid desk or monitor");
        Directory.CreateDirectory("Tools/OfficeOpening");
        ScreenCapture.CaptureScreenshot("Tools/OfficeOpening/after-fade.png");
        yield return new WaitForEndOfFrame();
        intro.SetPaused(true);
        Check(intro.IsPaused && intro.hud.pausePanel.activeSelf, "Explicit pause still opens the pause panel");
        yield return null;
        float pausedAnimationTime = bossAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(bossAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime - pausedAnimationTime) < .01f,
            "Esc pause freezes the boss animation together with the voice");
        Canvas.ForceUpdateCanvases();
        Check(!intro.hud.subtitlePanel.activeSelf && !HUDController.Instance.CrosshairVisible,
            "Pause hides subtitles and crosshair behind the modal");
        var pauseCard = intro.hud.pausePanel.transform.Find("OfficePauseCard");
        Check(pauseCard != null, "Designed pause card is present");
        Check(pauseCard.GetComponent<UnityEngine.UI.Image>().sprite == GameUiTheme.Current.PanelFrame,
            "Pause uses the sliced Main Menu parchment artwork");
        Check(intro.hud.resumeButton.GetComponent<UnityEngine.UI.Image>().sprite == GameUiTheme.Current.menuFrame,
            "Resume uses the exact Main Menu button sprite");
        foreach (var label in pauseCard.GetComponentsInChildren<TMPro.TMP_Text>())
        {
            label.ForceMeshUpdate();
            Check(!label.isTextOverflowing, "Pause label fits: " + label.name);
        }
        var corners = new Vector3[4];
        ((RectTransform)pauseCard).GetWorldCorners(corners);
        Check(corners[0].x >= 0 && corners[0].y >= 0 && corners[2].x <= Screen.width && corners[2].y <= Screen.height,
            "Pause card stays within the game viewport");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot("Tools/OfficeOpening/pause-ui.png");
        yield return new WaitForEndOfFrame();
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        Check(eventSystem != null, "Pause has an active EventSystem");
        var pointer = new UnityEngine.EventSystems.PointerEventData(eventSystem)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, intro.hud.resumeButton.transform.position)
        };
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        eventSystem.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && hits[0].gameObject == intro.hud.resumeButton.gameObject,
            "Resume button receives pointer hits above the dimmer");
        intro.hud.resumeButton.onClick.Invoke();
        Check(!intro.IsPaused && !intro.hud.pausePanel.activeSelf, "Resume closes the pause panel");
        Check(intro.hud.subtitlePanel.activeSelf, "Resume restores the current subtitle");
        intro.hud.SetPrompt("[E] Mở cửa");
        Check(intro.hud.prompt.gameObject.activeInHierarchy, "Interaction plate becomes visible with a prompt");
        intro.hud.SetPrompt("");
        if (intro.Sequence != null && intro.Sequence.CollectValidationIssues(true).Count == 0)
        {
            int spoken = 0, lastLine = -1;
            bool checkedImpact = false;
            while (intro.State == OfficeIntroController.IntroState.OpeningDialogue)
            {
                int index = intro.DialogueIndex;
                var line = intro.Sequence.GetLine(index);
                if (line.slam && intro.LineElapsed > .6f && !checkedImpact)
                {
                    checkedImpact = true;
                    Check(boss.CurrentState == "Boss_DeskSlam" && boss.ImpactCount == 1,
                        "Desk slam plays once and emits one contact sound event");
                    Check(Mathf.Abs(boss.LastImpactLineTime - 14f / 30f) < .1f,
                        "Desk sound follows actual animation contact at 14/30 seconds");
                    ScreenCapture.CaptureScreenshot("Tools/BossAnimation/desk-slam.png");
                }
                if (index != lastLine)
                {
                    lastLine = index;
                    if (!line.silentBeat)
                    {
                        Check(intro.VoicePlayer.Source.clip == line.voiceClip && intro.VoicePlayer.IsPlaying,
                            "Voice playback matches " + line.id);
                        Check(Mathf.Abs(line.ResolveDuration() - line.voiceClip.length - line.postDelay) < .01f,
                            "Subtitle duration follows clip length for " + line.id);
                        if (line.id == "act1_intro_ty_001")
                            Check(line.text == "Dạ dạ sếp, em kiểm tra lại ngay ạ" && line.spokenText == line.text,
                                "Ty subtitle and spoken script match the supplied recording");
                        spoken++;
                    }
                }
                yield return null;
            }
            Check(spoken == 4 && intro.DialogueIndex == intro.Sequence.Count,
                "All four voice clips and silent beats finish before returning control");
            Check(checkedImpact && boss.ImpactCount == 1 && boss.CurrentState == "Boss_Idle",
                "Boss finishes the slam, command and return clips and settles into idle");
        }
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot("Tools/OfficeOpening/objective-ui.png");
        yield return new WaitForEndOfFrame();
        var objective = GameObject.Find("OfficeCurrentObjective");
        Check(objective != null, "Current objective card is present after dialogue");
        foreach (var label in objective.GetComponentsInChildren<TMPro.TMP_Text>())
        {
            label.ForceMeshUpdate();
            Check(!label.isTextOverflowing, "Objective text fits its card");
        }
        intro.SkipToReadyAtDeskForTests();
        yield return null;
        var gameplayPause = FindObjectOfType<PauseUI>();
        Check(gameplayPause != null && gameplayPause.enabled, "Gameplay pause owner becomes active after intro handoff");
        gameplayPause.Pause();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot("Tools/OfficeOpening/gameplay-pause-ui.png");
        yield return new WaitForEndOfFrame();
        Check(GameManager.Instance.IsPaused && !HUDController.Instance.CrosshairVisible,
            "Gameplay pause uses the same modal and hides the crosshair");
        gameplayPause.Resume();
        Check(!GameManager.Instance.IsPaused, "Gameplay resume restores input");
        var office = FindObjectOfType<OfficeSceneController>();
        office.MakePrintingAvailableForTests();
        while (office.Phase == OfficePhase.PrintingReport) yield return null;
        CheckGameplayVoice(office, "act1_ty_print_ready_001");
        office.HandleMicroInteraction(OfficeMicroInteractionKind.PrinterPaper);
        CheckGameplayVoice(office, "act1_ty_print_pickup_001");
        yield return new WaitForSeconds(.15f);
        gameplayPause.Pause();
        float pickupPauseTime = office.GameplayVoicePlayer.Source.time;
        yield return new WaitForSecondsRealtime(.2f);
        Check(office.GameplayVoicePlayer.IsPaused && Mathf.Abs(office.GameplayVoicePlayer.Source.time-pickupPauseTime) < .05f,
            "Esc freezes Ty's pickup recording at its current position");
        gameplayPause.Resume();
        Check(office.GameplayVoicePlayer.IsPlaying && !office.GameplayVoicePlayer.IsPaused,
            "Resume continues Ty's pickup recording");
        office.MakeReportDeliveryAvailableForTests();
        office.HandleInteraction(OfficeInteractionKind.Boss);
        Check(office.ReportDelivered && office.Phase == OfficePhase.WaterBreak,
            "Handing over the printed report keeps the existing quest progression");
        float reportSpeechSeconds = office.CurrentSubtitleSeconds;
        Check(boss.CurrentState == "Boss_Talk_Command" && Mathf.Abs(boss.SpeechSecondsRemaining-reportSpeechSeconds) < .05f,
            "Boss animation follows the actual report reply duration");
        var gameplaySequence = Resources.Load<DialogueSequence>("OfficeGameplayVoices");
        if (gameplaySequence.Lines.Any(l => l.id == "act1_intro_boss_004"))
            CheckGameplayVoice(office, "act1_intro_boss_004");
        yield return new WaitForSeconds(.5f);
        gameplayPause.Pause();
        float remainingSpeech = boss.SpeechSecondsRemaining;
        float pausedVoiceTime = office.GameplayVoicePlayer.Source.time;
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(boss.SpeechSecondsRemaining - remainingSpeech) < .02f,
            "Pausing report delivery freezes the remaining boss speech time");
        if (office.GameplayVoicePlayer.Source.clip != null)
            Check(office.GameplayVoicePlayer.IsPaused && Mathf.Abs(office.GameplayVoicePlayer.Source.time-pausedVoiceTime) < .05f,
                "Esc pauses the gameplay voice without losing its playback position");
        gameplayPause.Resume();
        yield return new WaitForSeconds(Mathf.Max(.1f, reportSpeechSeconds-.3f));
        Check(boss.CurrentState == "Boss_ReturnIdle", "Boss lowers his hand when the report reply finishes");
        yield return new WaitForSeconds(1.3f);
        Check(boss.CurrentState == "Boss_Idle" && boss.ImpactCount == 1,
            "Report reply returns to idle without another desk slam");
        office.HandleMicroInteraction(OfficeMicroInteractionKind.WaterCooler);
        CheckGameplayVoice(office, "act1_ty_water_after_report_001");
        Check(office.WaterBreakCompleted && office.Phase == OfficePhase.Rest,
            "Water recording keeps the required water/rest progression");
        office.HandleInteraction(OfficeInteractionKind.Chair);
        CheckGameplayVoice(office, "act1_ty_rest_001");
        Check(office.Phase == OfficePhase.Leaving, "Rest recording plays during the actual collapse transition");
        yield return new WaitForSeconds(office.CurrentSubtitleSeconds + .1f);
        Check(!office.GameplayVoicePlayer.IsPlaying, "Ty's rest line finishes before the scene transition");
        office.ShowCinematicSubtitle("TÝ", "Cà phê lạnh ngắt rồi... mình quên uống từ lúc nào nhỉ?", 3.5f);
        Check(office.GameplayVoicePlayer.Source.clip == null && office.CurrentSubtitleSeconds == 3.5f,
            "Lines without supplied recordings stop the previous voice and preserve text-only timing");
        SceneLoader.Instance.LoadScene(SceneNames.MainMenu);
        while (SceneManager.GetActiveScene().name != SceneNames.MainMenu || SceneLoader.Instance.IsLoading) yield return null;
        yield return null;
        yield return null;
        var mainMenu = FindObjectOfType<MainMenuUI>();
        Check(mainMenu != null, "Return to Main Menu succeeds");
        foreach (var button in mainMenu.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            Check(button.GetComponent<UnityEngine.UI.Image>().sprite == GameUiTheme.Current.menuFrame,
                "Menu uses shared button artwork: " + button.name);
            foreach (var label in button.GetComponentsInChildren<TMPro.TMP_Text>(true))
                Check(label.font == GameUiTheme.Current.font && label.color == GameUiTheme.Current.text,
                    "Menu uses shared typography: " + button.name);
        }
        ScreenCapture.CaptureScreenshot("Tools/OfficeOpening/main-menu-shared-ui.png");
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Finish(true);
    }
    private void CheckGameplayVoice(OfficeSceneController office, string id)
    {
        var sequence = Resources.Load<DialogueSequence>("OfficeGameplayVoices");
        var line = sequence.Lines.First(l => l.id == id);
        Check(office.GameplayVoicePlayer.IsPlaying && office.GameplayVoicePlayer.Source.clip == line.voiceClip,
            "Gameplay recording matches interaction: " + id);
        Check(Mathf.Abs(office.CurrentSubtitleSeconds-line.ResolveDuration()) < .01f,
            "Gameplay subtitle follows voice length: " + id);
    }
    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        Directory.CreateDirectory("Tools/OfficeOpening");
        File.WriteAllText("Tools/OfficeOpening/checks.txt", (success ? "PASS\n" : "FAIL\n") + report);
        Application.runInBackground = previousBackground;
        EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
    }
}
