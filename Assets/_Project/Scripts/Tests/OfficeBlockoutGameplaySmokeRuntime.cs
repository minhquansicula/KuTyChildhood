#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class OfficeBlockoutGameplaySmokeBootstrap
{
    private const string PendingKey = "KuTy.OfficeBlockoutGameplaySmoke.Pending";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPendingRun()
    {
        if (!SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        new GameObject("Office Blockout Gameplay Smoke")
            .AddComponent<OfficeBlockoutGameplaySmokeRunner>();
    }
}

public sealed class OfficeBlockoutGameplaySmokeRunner : MonoBehaviour
{
    private const string ResultPath = "Assets/Screenshots/OfficeBlockoutGameplay_Integration.txt";
    public bool StopPlayOnCompletion { get; set; } = true;
    public event Action<bool, string> Completed;
    private int assertions;
    private bool finished;
    private bool previousRunInBackground;

    private void Start()
    {
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += OnLog;
        StartCoroutine(Run());
    }

    private void OnDestroy()
    {
        Application.runInBackground = previousRunInBackground;
        Application.logMessageReceived -= OnLog;
    }

    private IEnumerator Run()
    {
        yield return null;
        OfficeIntroController intro = FindObjectOfType<OfficeIntroController>();
        OfficeBlockoutGameplayBridge bridge = FindObjectOfType<OfficeBlockoutGameplayBridge>();
        PlayerInteraction playerInteraction = FindObjectOfType<PlayerInteraction>();
        OfficeSceneController office = FindObjectOfType<OfficeSceneController>();
        OfficeWorkstationInteractable workstation = FindObjectOfType<OfficeWorkstationInteractable>();
        OfficeBlockoutChairInteractable chair = FindObjectOfType<OfficeBlockoutChairInteractable>();
        OfficeBlockoutDoorInteractable doorAdapter = FindObjectOfType<OfficeBlockoutDoorInteractable>();
        ExcelDataAuditorGame excel = FindObjectOfType<ExcelDataAuditorGame>(true);
        OfficeGuidanceController guidance = FindObjectOfType<OfficeGuidanceController>(true);
        OfficeAtmosphere atmosphere = FindObjectOfType<OfficeAtmosphere>(true);
        AudioManager audio = AudioManager.Instance;
        OfficeMicroInteractable printer = null;
        OfficeMicroInteractable water = null;
        foreach (OfficeMicroInteractable micro in FindObjectsOfType<OfficeMicroInteractable>())
        {
            OfficeMicroInteractionKind microKind = Get<OfficeMicroInteractionKind>(micro, "kind");
            if (microKind == OfficeMicroInteractionKind.PrinterPaper) printer = micro;
            if (microKind == OfficeMicroInteractionKind.WaterCooler) water = micro;
        }
        OfficeInteractable boss = null;
        foreach (OfficeInteractable interaction in FindObjectsOfType<OfficeInteractable>())
        {
            if (Get<OfficeInteractionKind>(interaction, "kind") == OfficeInteractionKind.Boss)
            {
                boss = interaction;
                break;
            }
        }

        Check(intro != null && bridge != null && office != null && playerInteraction != null,
            "Core office controllers are missing");
        Check(workstation != null && chair != null && doorAdapter != null && excel != null && guidance != null,
            "Excel task interactions are missing");
        Check(printer != null && water != null && boss != null,
            "Print, delivery, or water task interactions are missing");
        OfficeReportMiniGame legacyReport = FindObjectOfType<OfficeReportMiniGame>();
        Check(legacyReport == null || !legacyReport.enabled,
            "Legacy email/document/report mini-game is still enabled");
        Check(GameManager.Instance != null && SceneLoader.Instance != null,
            "Direct-play runtime services are missing");
        Check(audio != null && audio.Catalog != null && audio.Catalog.CollectValidationIssues(true).Count == 0,
            "Typed SFX catalog or AudioManager wiring is missing");
        Check(atmosphere != null && Get<AudioSource>(atmosphere, "propSource") != null,
            "Office scene-local SFX source is missing");
        Check(audio.PlaySFX(SfxId.UIConfirm) && !audio.PlaySFX(SfxId.UIConfirm),
            "SFX playback or per-cue cooldown did not reject an immediate duplicate");
        Check(audio.PlaySFX(SfxId.DishClean) && audio.ActiveGameplaySfxVoices > 0,
            "Pooled gameplay SFX did not start on an active transient voice");
        audio.StopTransientSfx();
        Check(audio.ActiveGameplaySfxVoices == 0,
            "Stopping transient SFX left a pooled gameplay voice active");

        var invalidCatalog = ScriptableObject.CreateInstance<SfxCatalog>();
        invalidCatalog.ReplaceCuesForEditor(new[]
        {
            new SfxCue { id = SfxId.TaskProgress },
            new SfxCue { id = SfxId.TaskProgress }
        });
        Check(invalidCatalog.CollectValidationIssues(false).Exists(issue => issue.Contains("duplicates ID")),
            "SFX validation did not reject duplicate typed IDs");
        Destroy(invalidCatalog);
        Check(intro.State == OfficeIntroController.IntroState.OpeningDialogue && office.Phase == OfficePhase.Opening,
            "Office did not begin in the boss-room opening");
        Check(intro.Sequence != null && intro.Sequence.Count == 6,
            "Act1 TTS dialogue sequence is missing or has an unexpected beat count");
        Check(intro.VoicePlayer != null && intro.VoicePlayer.Source != null &&
              !intro.VoicePlayer.Source.playOnAwake && !intro.VoicePlayer.Source.loop &&
              Mathf.Approximately(intro.VoicePlayer.Source.spatialBlend, 0f),
            "Act1 voice player is not configured as a non-looping 2D source");
        Check(intro.Sequence.CollectValidationIssues(false).Count == 0,
            "Act1 dialogue sequence has invalid IDs, beats, or fallback timing");

        var validationSequence = ScriptableObject.CreateInstance<DialogueSequence>();
        validationSequence.ReplaceLinesForEditor(new[]
        {
            new DialogueSequence.DialogueLine { id = "duplicate", text = "A", fallbackDuration = .3f, postDelay = .2f },
            new DialogueSequence.DialogueLine { id = "duplicate", text = "B", fallbackDuration = .3f }
        });
        Check(validationSequence.CollectValidationIssues(false).Exists(issue => issue.Contains("duplicates ID")),
            "Dialogue validation did not reject duplicate stable IDs");
        Check(Mathf.Abs(validationSequence.GetLine(0).ResolveDuration() - .5f) < .01f,
            "Missing voice clip did not resolve to fallback duration plus post delay");
        Destroy(validationSequence);
        yield return RunVoicePlayerLifecycle();

        yield return WaitUntilOrFail(() => !intro.IsFadingIn, 5f,
            "Opening fade did not finish before the dialogue pause test");
        float introElapsedBeforePause = intro.LineElapsed;
        int introLineBeforePause = Get<int>(intro, "lineIndex");
        intro.SetPaused(true);
        yield return new WaitForSecondsRealtime(.15f);
        Check(intro.IsPaused && Mathf.Abs(intro.LineElapsed - introElapsedBeforePause) < .01f,
            "Intro subtitle timing advanced while paused");
        intro.SetPaused(false);
        yield return new WaitForSecondsRealtime(.12f);
        Check(!intro.IsPaused && (intro.LineElapsed > introElapsedBeforePause ||
              Get<int>(intro, "lineIndex") > introLineBeforePause),
            "Intro subtitle timing did not resume after pause");
        yield return WaitUntilOrFail(
            () => intro.State == OfficeIntroController.IntroState.ReturnToDesk,
            30f,
            "Missing TTS clips caused the opening intro to hang");
        Check(intro.DialogueIndex == intro.Sequence.Count,
            "Opening intro did not consume each authored dialogue beat exactly once");

        intro.SkipToReadyAtDeskForTests();
        yield return null;
        Check(bridge.HandoffComplete && intro.GameplayHandoffComplete,
            "Intro-to-gameplay handoff did not complete");
        Check(office.Phase == OfficePhase.ReturnToDesk && office.IsSeated,
            "Handoff did not adopt the seated work state");
        Check(!GameManager.Instance.InputBlocked, "Handoff left gameplay input blocked");
        Check(EventSystem.current != null && EventSystem.current.gameObject.activeInHierarchy,
            "Intro handoff disabled the EventSystem needed by gameplay buttons");
        Check(chair.TryGetProximityDistance(chair.transform.position + Vector3.right * 1.2f, out _) &&
              !chair.TryGetProximityDistance(chair.transform.position + Vector3.right * 2f, out _),
            "Chair nearby interaction range is missing or too broad");
        Check(doorAdapter.TryGetProximityDistance(doorAdapter.transform.position + Vector3.right * 1.5f, out _) &&
              !doorAdapter.TryGetProximityDistance(doorAdapter.transform.position + Vector3.right * 2.5f, out _),
            "Door nearby interaction range is missing or too broad");
        Check(doorAdapter.GetPromptText() == "[E] Mở cửa", "Door cannot be opened after the intro handoff");
        doorAdapter.Interact();
        Check(intro.door.State == OfficeDoorInteractable.DoorState.Opening, "Door did not begin opening");
        intro.door.Tick(2f, intro.body);
        Check(intro.door.State == OfficeDoorInteractable.DoorState.Open &&
              doorAdapter.GetPromptText() == "[E] Đóng cửa", "Open door has no close interaction");
        GameObject doorBlocker = new GameObject("SmokeDoorSweepBlocker");
        CharacterController blockerBody = doorBlocker.AddComponent<CharacterController>();
        doorBlocker.transform.position = intro.door.leaf.bounds.center - Vector3.up * blockerBody.center.y;
        Check(!intro.door.TryClose(blockerBody) && intro.door.State == OfficeDoorInteractable.DoorState.Open,
            "Door closed through a character standing in its sweep");
        Destroy(doorBlocker);
        doorAdapter.Interact();
        Check(intro.door.State == OfficeDoorInteractable.DoorState.Closing, "Door did not begin closing");
        intro.door.Tick(2f, intro.body);
        Check(intro.door.State == OfficeDoorInteractable.DoorState.Closed,
            "Door failed to return to its closed state");

        // Edge case: help appears in two stages and resets on genuine progress.
        guidance.ConfigureThresholdsForTests(.01f, .02f);
        guidance.AdvanceForTests(.011f);
        Check(guidance.HintShown && !guidance.MarkerVisible,
            "Guidance text did not appear before the stronger target marker");
        guidance.AdvanceForTests(.011f);
        Check(guidance.MarkerVisible && guidance.CurrentTarget == workstation.transform,
            "Guidance marker did not resolve the seated workstation target");
        GameManager.Instance.SetPaused(true);
        yield return null;
        Check(!guidance.MarkerVisible && AudioListener.pause,
            "Guidance or global audio did not enter the paused state");
        Check(audio.PlaySFX(SfxId.UIBack) && !audio.PlaySFX(SfxId.DishClean),
            "Paused gameplay SFX was accepted or pause-menu UI SFX was rejected");
        GameManager.Instance.SetPaused(false);
        yield return null;
        Check(guidance.MarkerVisible && !AudioListener.pause,
            "Guidance or global audio did not return after resuming");

        // Edge case: stale callbacks must not skip the Excel task or acquire input.
        office.CancelWork();
        office.CompleteExcelTask();
        Check(office.Phase == OfficePhase.ReturnToDesk && !office.IsWorking &&
              !GameManager.Instance.InputBlocked,
            "A stale cancel/complete callback skipped or locked the Excel task");

        office.StandUp(true);
        Check(!office.IsSeated, "Safe stand position was incorrectly rejected");
        Check(chair.TryGetProximityDistance(playerInteraction.transform.position, out _),
            "Standing beside the desk left the chair outside interaction range");
        Transform originalRayOrigin = Get<Transform>(playerInteraction, "raycastOrigin");
        GameObject upwardRayOrigin = new GameObject("SmokeChairRayOrigin");
        upwardRayOrigin.transform.SetPositionAndRotation(originalRayOrigin.position,
            Quaternion.LookRotation(Vector3.up, Vector3.forward));
        Set(playerInteraction, "raycastOrigin", upwardRayOrigin.transform);
        Call(playerInteraction, "HandleRaycast");
        Check(playerInteraction.CurrentTarget == chair,
            "Chair could not be focused nearby when the camera ray missed its collider");
        Set(playerInteraction, "raycastOrigin", originalRayOrigin);
        Destroy(upwardRayOrigin);
        CheckPropTargeting(playerInteraction, intro.motor, water,
            new[] { .85f, 1.4f, 2.1f }, new[] { .9f, 1.3f, 1.75f });
        CheckPropTargeting(playerInteraction, intro.motor, printer,
            new[] { 1.15f, 1.7f, 2.2f }, new[] { 1.2f, 1.65f });
        var clock = Array.Find(FindObjectsOfType<OfficeMicroInteractable>(),
            item => item.Kind == OfficeMicroInteractionKind.Clock);
        CheckPropTargeting(playerInteraction, intro.motor, clock,
            new[] { 2f, 2.6f }, new[] { 2.07f });
        var coffee = Array.Find(FindObjectsOfType<OfficeMicroInteractable>(),
            item => item.Kind == OfficeMicroInteractionKind.ColdCoffee);
        CheckPropTargeting(playerInteraction, intro.motor, coffee,
            new[] { .85f, 1.2f }, new[] { .85f });
        if (SessionState.GetBool("KuTy.OfficeSpatialOnly", false))
        {
            SessionState.SetBool("KuTy.OfficeSpatialOnly", false);
            chair.Interact();
            yield return new WaitForSeconds(.7f);
            originalRayOrigin.rotation = Quaternion.LookRotation(workstation.transform.position - originalRayOrigin.position);
            Physics.SyncTransforms();
            Call(playerInteraction, "HandleRaycast");
            Check(office.IsSeated && playerInteraction.CurrentTarget == workstation,
                "Seated player cannot select the visible computer monitor");
            office.StandUp(true);
            office.MakeReportDeliveryAvailableForTests();
            doorAdapter.Interact();
            Check(office.Phase == OfficePhase.DeliverReport, "Door incorrectly accepted the printed report");
            intro.motor.TeleportTo(new Vector3(boss.transform.position.x, .08f,
                boss.transform.position.z - 2f), Quaternion.identity);
            originalRayOrigin.rotation = Quaternion.LookRotation(boss.transform.position + Vector3.up - originalRayOrigin.position);
            Physics.SyncTransforms();
            Call(playerInteraction, "HandleRaycast");
            Check(playerInteraction.CurrentTarget == boss, "Boss desk cannot be selected from the visitor side");
            playerInteraction.CurrentTarget.Interact();
            Check(office.Phase == OfficePhase.WaterBreak && office.ReportDelivered,
                "Handing the report to the selected boss desk did not start the water break");
            intro.motor.TeleportTo(new Vector3(water.transform.position.x, .08f,
                water.transform.position.z - 1.1f), Quaternion.identity);
            originalRayOrigin.rotation = Quaternion.LookRotation(water.transform.position + Vector3.up * 1.1f - originalRayOrigin.position);
            Physics.SyncTransforms();
            Call(playerInteraction, "HandleRaycast");
            Check(playerInteraction.CurrentTarget == water, "Actual water dispenser did not receive focus");
            playerInteraction.CurrentTarget.Interact();
            Check(office.Phase == OfficePhase.Rest && office.WaterBreakCompleted,
                "Using the selected water dispenser did not complete the drink step");
            CaptureUI("OfficeInteractions_Water", 1366, 768);
            Complete(true, "Spatial targeting, clear standing positions, distance/occlusion, boss desk delivery and drinking passed");
            yield break;
        }
        AudioSource officePropSource = Get<AudioSource>(atmosphere, "propSource");
        Check(officePropSource.isPlaying, "Standing up did not play the chair SFX");
        yield return new WaitForSecondsRealtime(.5f);
        printer.Interact();
        Check(office.Phase == OfficePhase.ReturnToDesk && !officePropSource.isPlaying,
            "Checking the printer early advanced the task or played a false-success SFX");
        chair.Interact();
        yield return null;
        Check(office.IsSeated && officePropSource.isPlaying,
            "Chair did not return the player to the seated state with feedback");
        yield return new WaitForSeconds(.6f);
        CaptureUI("OfficeShift_HUD", 1366, 768);

        workstation.Interact();
        yield return null;
        Check(office.Phase == OfficePhase.ExcelAuditing && office.IsWorking && excel.gameObject.activeInHierarchy,
            "Excel task did not open directly from the workstation");
        Check(!guidance.HintShown && !guidance.MarkerVisible,
            "Guidance was not cleared after task progress");
        Check(GameManager.Instance.InputBlocked, "Work UI did not acquire input");
        yield return new WaitForSeconds(.3f);
        Check(excel.IsTutorialActive && !excel.IsAttemptRunning,
            "First Excel open did not pause on the tutorial");
        Check(TutorialButtonReceivesPointer(excel, "TimedMode") &&
              TutorialButtonReceivesPointer(excel, "RelaxedMode"),
            "First-run Excel choices are visible but do not receive pointer raycasts");
        float tutorialTime = Get<float>(excel, "timeRemaining");
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(Get<float>(excel, "timeRemaining") - tutorialTime) < .01f,
            "Excel timer advanced while the first-run tutorial was open");
        CaptureUI("OfficeExcel_Tutorial", 1366, 768);

        excel.BeginAttempt(ExcelAssistMode.Relaxed);
        float relaxedTime = Get<float>(excel, "timeRemaining");
        yield return new WaitForSecondsRealtime(.2f);
        Check(excel.IsRelaxedMode && excel.IsAttemptRunning &&
              Mathf.Abs(Get<float>(excel, "timeRemaining") - relaxedTime) < .01f,
            "Relaxed mode started or consumed a deadline");
        excel.BeginAttempt(ExcelAssistMode.Timed);
        Check(!excel.IsRelaxedMode && excel.IsAttemptRunning,
            "Timed mode could not start after the tutorial choice");
        CaptureUI("OfficeExcel_UI", 1920, 1080);
        CaptureUI("OfficeExcel_UI", 1366, 768);

        // Edge case: repeated interaction/open events are idempotent and do not reset the timer.
        float beforeRepeatedOpen = Get<float>(excel, "timeRemaining");
        workstation.Interact();
        Check(excel.Open(office), "Repeated Excel open should be accepted as an idempotent no-op");
        yield return null;
        Check(office.IsWorking && Get<bool>(excel, "isGameActive") &&
              Get<float>(excel, "timeRemaining") <= beforeRepeatedOpen,
            "Repeated Excel interaction restarted or closed the active task");

        GameManager.Instance.SetPaused(true);
        Check(Time.timeScale == 0f, "Pause did not freeze scaled gameplay time");
        float pausedTimer = Get<float>(excel, "timeRemaining");
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(Get<float>(excel, "timeRemaining") - pausedTimer) < .01f,
            "Excel timer advanced during pause");
        GameManager.Instance.SetPaused(false);

        // Edge case: the boss popup pauses the task timer and safely resumes it when closed.
        GameObject chatPopup = Get<GameObject>(excel, "chatPopupPanel");
        Call(excel, "ShowChatPopup");
        Check(chatPopup != null && chatPopup.activeSelf, "Boss popup could not be shown");
        Check(Get<Image>(excel, "chatBackdrop").gameObject.activeInHierarchy,
            "Boss popup has no modal input shield");
        int originalSheet = Get<int>(excel, "currentSheetIndex");
        Call(excel, "SwitchSheet", 1);
        Call(excel, "CheckValidation");
        Check(Get<int>(excel, "currentSheetIndex") == originalSheet && office.IsWorking,
            "The spreadsheet accepted input behind the boss message");
        CaptureUI("OfficeExcel_Chat", 1366, 768);
        float frozenTime = Get<float>(excel, "timeRemaining");
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(Get<float>(excel, "timeRemaining") - frozenTime) < .05f,
            "Excel timer continued while the boss popup was open");
        Call(excel, "CloseChatPopup");
        float resumedTime = Get<float>(excel, "timeRemaining");
        yield return new WaitForSecondsRealtime(.2f);
        Check(Get<float>(excel, "timeRemaining") < resumedTime,
            "Excel timer did not resume after the boss popup closed");

        Set<GameObject>(excel, "chatPopupPanel", null);
        IEnumerator missingPopupRoutine = (IEnumerator)Call(excel, "BossDistractionRoutine");
        Check(missingPopupRoutine.MoveNext() && missingPopupRoutine.MoveNext(),
            "Missing boss popup reference caused the distraction routine to stop unexpectedly");
        Set(excel, "chatPopupPanel", chatPopup);

        var inputs = Get<Dictionary<int, Dictionary<Vector2Int, string>>>(excel, "userInputs");
        Vector2Int answerCell = new Vector2Int(4, 3);
        inputs[0][answerCell] = "-1";
        inputs[1][answerCell] = "11500";
        Call(excel, "CheckValidation");
        var errors = Get<Dictionary<int, HashSet<Vector2Int>>>(excel, "validationErrors");
        Check(errors.Count == 1 && errors.TryGetValue(0, out var firstSheetErrors) &&
              firstSheetErrors.Contains(answerCell) && office.Phase == OfficePhase.ExcelAuditing && office.IsWorking,
            "Invalid Excel input was accepted or did not focus the incorrect sheet");
        Check(audio.LastPlayedSfx == SfxId.UIInvalid,
            "Invalid Excel submission did not play the invalid SFX cue");

        // Edge case: timeout retries in place, closes the popup, and preserves typed data.
        Call(excel, "ShowChatPopup");
        Call(excel, "FailGame", "Kiểm tra hết giờ");
        Check(!Get<bool>(excel, "isGameActive") && !chatPopup.activeSelf &&
              !Get<Button>(excel, "submitButton").interactable,
            "Timeout did not enter a safe retry state");
        yield return new WaitForSecondsRealtime(2.15f);
        Check(Get<bool>(excel, "isGameActive") &&
              inputs[0].TryGetValue(answerCell, out string retainedValue) && retainedValue == "-1" &&
              Get<float>(excel, "timeRemaining") > 0f,
            "Timeout retry lost user data or failed to restart the task");

        float retainedTime = Get<float>(excel, "timeRemaining");
        office.CancelWork();
        Check(office.Phase == OfficePhase.ExcelAuditing && !office.IsWorking && !excel.gameObject.activeInHierarchy,
            "Excel cancellation did not return control cleanly");
        office.CancelWork();
        Check(office.Phase == OfficePhase.ExcelAuditing && !GameManager.Instance.InputBlocked,
            "Repeated Excel cancellation was not idempotent");

        workstation.Interact();
        yield return null;
        Check(Get<float>(excel, "timeRemaining") <= retainedTime && Get<float>(excel, "timeRemaining") > retainedTime - 1f,
            "Closing and reopening reset the task deadline");
        office.CancelWork();

        // Start a fresh attempt with an invalid Inspector limit: use a playable default.
        float configuredTimeLimit = Get<float>(excel, "timeLimit");
        Set(excel, "timeLimit", 0f);
        Set(excel, "attemptStarted", false);
        workstation.Interact();
        yield return null;
        Check(office.IsWorking && Get<float>(excel, "timeRemaining") >= 1f,
            "A non-positive Excel time limit caused an immediate failure loop");
        office.CancelWork();
        Set(excel, "timeLimit", configuredTimeLimit);

        workstation.Interact();
        yield return null;
        Check(office.IsWorking && excel.gameObject.activeInHierarchy && inputs[0][answerCell] == "-1",
            "Excel task did not reopen or preserve data after cancellation");

        // Whitespace around otherwise correct numeric input should be accepted.
        Call(excel, "SwitchSheet", 0);
        Get<Dictionary<Vector2Int, TMP_InputField>>(excel, "visibleInputs")[answerCell].text = "1300";
        Call(excel, "SwitchSheet", 1);
        yield return null;
        Get<Dictionary<Vector2Int, TMP_InputField>>(excel, "visibleInputs")[answerCell].text = "11500";
        Check(inputs[0][answerCell] == "1300" && inputs[1][answerCell] == "11500" && office.WorkProgress == 1f,
            "Typing into the actual TMP fields failed to update task data and progress");
        inputs[0][answerCell] = " 1300 ";
        Set(office, "printerDurationSeconds", .05f);
        Call(excel, "CheckValidation");
        Check(!Get<bool>(excel, "isGameActive") && !Get<Button>(excel, "submitButton").interactable,
            "Correct Excel data did not enter the completion state");
        Check(audio.LastPlayedSfx == SfxId.TaskComplete,
            "Correct Excel submission did not play the completion SFX cue");
        yield return new WaitForSecondsRealtime(1.65f);
        Check(office.Phase == OfficePhase.CollectPrint && !office.IsWorking && officePropSource.isPlaying,
            "Excel completion did not finish the printer beat with printer SFX");
        printer.Interact();
        Check(office.Phase == OfficePhase.DeliverReport && office.HasPrintedReport && officePropSource.isPlaying,
            "Taking the printout did not start report delivery with paper SFX");
        Check(boss != null && guidance.CurrentTarget == boss.transform,
            "Report delivery guidance did not point to the boss's desk");
        Check(!boss.TryGetProximityDistance(doorAdapter.transform.position, out _),
            "Report delivery can still be reached from the doorway");
        Check(boss.TryGetProximityDistance(boss.transform.position + Vector3.back * 2f, out _),
            "Report delivery is not reachable from the visitor side of the desk");
        Check(!doorAdapter.GetPromptText().Contains("nộp") &&
            !doorAdapter.GetPromptText().Contains("báo cáo"),
            "Door still advertises report delivery");
        doorAdapter.Interact();
        Check(office.Phase == OfficePhase.DeliverReport && !office.ReportDelivered,
            "Using the door incorrectly delivered the report");
        Check(!Get<bool>(HUDController.Instance, "interactionPromptsVisible"),
            "Scene 1 still shows text over the crosshair");
        boss.Interact();
        Check(office.Phase == OfficePhase.WaterBreak && office.ReportDelivered,
            "Delivering the report at the boss's desk did not unlock the water break");
        office.StandUp(true);
        Vector3 waterStand = new Vector3(water.transform.position.x, .08f,
            water.transform.position.z - 1.1f);
        intro.motor.TeleportTo(waterStand, Quaternion.identity);
        originalRayOrigin.rotation = Quaternion.LookRotation(
            water.transform.position + Vector3.up * 1.1f - originalRayOrigin.position);
        Physics.SyncTransforms();
        Call(playerInteraction, "HandleRaycast");
        Check(playerInteraction.CurrentTarget == water,
            "Looking at the actual water dispenser did not select the drinking interaction");
        playerInteraction.CurrentTarget.Interact();
        Check(office.Phase == OfficePhase.Rest && office.WaterBreakCompleted && officePropSource.isPlaying,
            "Drinking water did not unlock the final rest objective with water SFX");
        yield return null;
        CaptureUI("OfficeShift_Complete", 1366, 768);

        workstation.Interact();
        Check(office.Phase == OfficePhase.Leaving && GameManager.Instance.InputBlocked,
            "Rest interaction did not start the exhaustion sequence");

        yield return WaitUntilOrFail(
            () => SceneManager.GetActiveScene().name == SceneNames.Act2_Home,
            45f,
            "Exhaustion did not load Act 2");
        Check(SceneManager.GetActiveScene().name == SceneNames.Act2_Home,
            "Exhaustion did not load Act 2");
        Complete(true, "Act1 TTS/SFX, Excel, print, delivery, water, and exhaustion smoke passed");
    }

    private IEnumerator RunVoicePlayerLifecycle()
    {
        GameObject testObject = new GameObject("Dialogue Voice Player Smoke");
        AudioSource source = testObject.AddComponent<AudioSource>();
        DialogueVoicePlayer player = testObject.AddComponent<DialogueVoicePlayer>();
        player.ConfigureForEditor(source, .5f);
        AudioClip clip = AudioClip.Create("DialogueVoicePlayerSmoke", 44100, 1, 44100, false);

        player.SetMuted(true);
        int firstToken = player.Play(clip);
        yield return new WaitForSecondsRealtime(.08f);
        int playingSample = source.timeSamples;
        Check(source.isPlaying && playingSample > 0,
            "Muted dialogue did not keep playback timing alive");

        player.Pause();
        int pausedSample = source.timeSamples;
        yield return new WaitForSecondsRealtime(.12f);
        Check(player.IsPaused && Mathf.Abs(source.timeSamples - pausedSample) < 256,
            "Dialogue audio advanced while paused");

        player.Resume();
        yield return new WaitForSecondsRealtime(.08f);
        Check(!player.IsPaused && source.timeSamples > pausedSample,
            "Dialogue audio did not resume from its paused position");

        int replacementToken = player.Play(clip);
        Check(replacementToken > firstToken && source.timeSamples < playingSample,
            "Replacing dialogue did not invalidate the previous playback token and restart safely");
        player.Stop();
        Check(!source.isPlaying && source.clip == null && player.PlaybackToken > replacementToken,
            "Stopping dialogue did not clear audio or invalidate stale playback");

        Destroy(testObject);
        Destroy(clip);
        yield return null;
    }

    private IEnumerator WaitUntilOrFail(Func<bool> predicate, float timeout, string message)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!predicate() && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(predicate(), message + $" (timeScale={Time.timeScale}, paused={GameManager.Instance?.IsPaused}, loading={SceneLoader.Instance?.IsLoading})");
    }

    private static void CaptureUI(string name, int width, int height)
    {
        Camera camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("Missing player camera for UI verification");
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        var modes = new RenderMode[canvases.Length];
        var cameras = new Camera[canvases.Length];
        var distances = new float[canvases.Length];
        var target = RenderTexture.GetTemporary(width, height, 24);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        float oldAspect = camera.aspect;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                cameras[i] = canvases[i].worldCamera;
                distances[i] = canvases[i].planeDistance;
                if (!canvases[i].isRootCanvas || modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = .5f;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            Directory.CreateDirectory("Assets/Screenshots");
            File.WriteAllBytes($"Assets/Screenshots/{name}_{width}.png", texture.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = modes[i];
                canvases[i].worldCamera = cameras[i];
                canvases[i].planeDistance = distances[i];
            }
            camera.targetTexture = oldTarget;
            camera.aspect = oldAspect;
            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(target);
            Destroy(texture);
            Canvas.ForceUpdateCanvases();
        }
    }

    private static bool TutorialButtonReceivesPointer(ExcelDataAuditorGame excel, string buttonName)
    {
        EventSystem system = EventSystem.current;
        if (system == null || !system.isActiveAndEnabled) return false;
        Button target = null;
        foreach (Button button in excel.GetComponentsInChildren<Button>(true))
            if (button.name == buttonName) { target = button; break; }
        if (target == null || !target.IsActive() || !target.IsInteractable()) return false;
        RectTransform rect = target.transform as RectTransform;
        if (rect == null) return false;

        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var pointer = new PointerEventData(system) { position = position };
        var hits = new List<RaycastResult>();
        system.RaycastAll(pointer, hits);
        return hits.Count > 0 && (hits[0].gameObject == target.gameObject ||
            hits[0].gameObject.transform.IsChildOf(target.transform));
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        assertions++;
    }

    private void CheckPropTargeting(PlayerInteraction interaction, FirstPersonController motor,
        OfficeMicroInteractable prop, float[] distances, float[] aimHeights)
    {
        Check(prop != null, "Missing prop for spatial interaction checks");
        Transform origin = Get<Transform>(interaction, "raycastOrigin");
        Vector3 savedPosition = motor.transform.position;
        Quaternion savedRotation = motor.transform.rotation;
        Quaternion savedViewRotation = origin.rotation;
        var probe = new GameObject("OfficeInteractionSpatialProbe");
        Set(interaction, "raycastOrigin", probe.transform);
        try
        {
            foreach (float distance in distances)
            foreach (float side in new[] { -.25f, 0f, .25f })
            {
                Vector3 feet = prop.transform.position + new Vector3(side, 0f, -distance);
                feet.y = .08f;
                motor.TeleportTo(feet, Quaternion.identity);
                Physics.SyncTransforms();
                var body = motor.GetComponent<CharacterController>();
                Vector3 center = body.transform.TransformPoint(body.center);
                float half = body.height * .5f - body.radius;
                var overlaps = Physics.OverlapCapsule(center + Vector3.up * half,
                    center - Vector3.up * half, body.radius * .95f, ~0, QueryTriggerInteraction.Ignore);
                Check(!Array.Exists(overlaps, c => !c.transform.IsChildOf(motor.transform)),
                    $"{prop.Kind}: standing position is obstructed at {feet}");
                foreach (float height in aimHeights)
                {
                    probe.transform.position = origin.position;
                    probe.transform.rotation = Quaternion.LookRotation(new Vector3(
                        prop.transform.position.x, height, prop.transform.position.z) - probe.transform.position);
                    Call(interaction, "HandleRaycast");
                    Check(interaction.CurrentTarget == prop,
                        $"{prop.Kind}: cannot target from {feet}, aiming at height {height}");
                }
            }
            probe.transform.position = prop.transform.position + new Vector3(0f, 1.65f, -4.5f);
            probe.transform.rotation = Quaternion.LookRotation(prop.transform.position + Vector3.up - probe.transform.position);
            Call(interaction, "HandleRaycast");
            Check(interaction.CurrentTarget != prop, $"{prop.Kind}: can interact from too far away");
            // A wall must block use even when the prop's trigger is on the ray.
            probe.transform.position = prop.transform.position + new Vector3(0f, 1.65f, -1.8f);
            Vector3 point = prop.transform.position + Vector3.up * aimHeights[0];
            probe.transform.rotation = Quaternion.LookRotation(point - probe.transform.position);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "InteractionOcclusionTest";
            wall.transform.position = Vector3.Lerp(probe.transform.position, point, .5f);
            wall.transform.localScale = new Vector3(1f, 3f, .1f);
            Physics.SyncTransforms();
            Call(interaction, "HandleRaycast");
            Check(interaction.CurrentTarget != prop, $"{prop.Kind}: interaction passes through a wall");
            wall.SetActive(false);
            Destroy(wall);
        }
        finally
        {
            motor.TeleportTo(savedPosition, savedRotation);
            origin.rotation = savedViewRotation;
            Set(interaction, "raycastOrigin", origin);
            Destroy(probe);
            Physics.SyncTransforms();
        }
    }

    private static T Get<T>(object instance, string field)
    {
        FieldInfo info = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null) throw new MissingFieldException(instance.GetType().Name, field);
        return (T)info.GetValue(instance);
    }

    private static void Set<T>(object instance, string field, T value)
    {
        FieldInfo info = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null) throw new MissingFieldException(instance.GetType().Name, field);
        info.SetValue(instance, value);
    }

    private static object Call(object instance, string method, params object[] arguments)
    {
        MethodInfo info = instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null) throw new MissingMethodException(instance.GetType().Name, method);
        return info.Invoke(instance, arguments);
    }

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (finished || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        Complete(false, condition + "\n" + stackTrace);
    }

    private void Complete(bool success, string message)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnLog;
        string result = (success ? "PASS" : "FAIL") + " (" + assertions + " assertions)\n" + message;
        Directory.CreateDirectory("Assets/Screenshots");
        File.WriteAllText(ResultPath, result);
        if (success) Debug.Log("[KuTy] " + result);
        else Debug.LogError("[KuTy] " + result);
        Completed?.Invoke(success, message);
        if (StopPlayOnCompletion) EditorApplication.isPlaying = false;
    }
}
#endif
